using System.Diagnostics;
using Revolt.Protocols;

namespace Revolt.Frames;

public sealed class PingFrame : Tui.Frame {
    const int HISTORY_LEN = 160;
 
    enum MoveOption : int {
        Never         = 0,
        OnRise        = 1,
        OnFall        = 2,
        OnRiseAndFall = 3
    }

    public static PingFrame Instance { get; } = new PingFrame();

    public Tui.ListBox<PingItem> list;
    public Tui.Toolbar toolbar;

    private CancellationTokenSource cancellationTokenSource;
    private CancellationToken cancellationToken;

    private readonly List<string> queryHistory = [];

    private int        ringIndex = 0;
    private bool       status    = true;
    private int        timeout   = 1000;
    private int        interval  = 1000;
    private MoveOption move      = MoveOption.Never;

    public PingFrame() {
        list = new Tui.ListBox<PingItem>(this) {
            left              = 1,
            right             = 1,
            top               = 1,
            bottom            = 3,
            itemHeight        = 2,
            drawItemHandler   = DrawPingItem
        };

        toolbar = new Tui.Toolbar(this) {
            left  = 0,
            right = 0,
            items = [
                new Tui.Toolbar.ToolbarItem() { text="Add",     key="INS", action=AddDialog},
                new Tui.Toolbar.ToolbarItem() { text="Remove",  key="DEL", action=RemoveSelected},
                new Tui.Toolbar.ToolbarItem() { text="Pause",   key="F2",  action=ToggleStatus },
                new Tui.Toolbar.ToolbarItem() { text="Clear",   key="F3",  action=Clear },
                new Tui.Toolbar.ToolbarItem() { text="Options", key="F4",  action=OptionsDialog },
            ]
        };

        elements.Add(list);
        elements.Add(toolbar);

        defaultElement = list;
        FocusNext();

        if (status) {
            Start();
        }
    }

    public override bool HandleKey(ConsoleKeyInfo key) {
        switch (key.Key) {
        case ConsoleKey.Tab:
            if (key.Modifiers == ConsoleModifiers.Shift) {
                FocusPrevious();
            }
            else {
                FocusNext();
            }
            break;

        case ConsoleKey.Escape:
            MainMenu.Instance.Show();
            break;

        case ConsoleKey.Insert:
            AddDialog();
            break;

        case ConsoleKey.Delete:
            RemoveSelected();
            break;

        case ConsoleKey.F2:
            ToggleStatus();
            break;

        case ConsoleKey.F3:
            Clear();
            break;

        case ConsoleKey.F4:
            OptionsDialog();
            break;

        default:
            focusedElement.HandleKey(key);
            break;
        }

        return true;
    }

    private void DrawPingItem(int index, int x, int y, int width) {
        if (list.items is null || list.items.Count == 0) return;
        if (index >= list.items.Count) return;

        int adjustedY = y + index * 2 - list.scrollOffset * list.itemHeight;
        if (adjustedY < y || adjustedY > Renderer.LastHeight) return;

        PingItem item = list.items[index];

        DrawItemLabel(item, index, adjustedY);
        UpdateHistoryAndStatus(item, index, x, adjustedY, width);
    }

    private void DrawItemLabel(PingItem item, int index, int y) {
        if (index == list.index) {
            Ansi.SetFgColor(list.isFocused ? [16, 16, 16] : Data.LIGHT_COLOR);
            Ansi.SetBgColor(list.isFocused ? Data.SELECT_COLOR : Data.SELECT_COLOR_LIGHT);
        }
        else {
            Ansi.SetFgColor(Data.LIGHT_COLOR);
            Ansi.SetBgColor(Data.DARK_COLOR);
        }

        Ansi.SetCursorPosition(2, y);
        Ansi.Write(item.host.Length > 23 ? item.host[..22] + Data.ELLIPSIS : item.host.PadRight(23));

        Ansi.Write(' ');
    }

    private void UpdatePingItem(int index, int x, int y, int width) {
        PingItem item = list.items[index];
        UpdateHistoryAndStatus(item, index, x, y, width);
    }

    private void UpdateHistoryAndStatus(PingItem item, int index, int x, int y, int width) {
        int usableWidth = Math.Min(width - 38, HISTORY_LEN);
        int historyOffset = (ringIndex + HISTORY_LEN - usableWidth + 1) % HISTORY_LEN;

        Ansi.SetBgColor(index == list.index ? Data.SELECT_COLOR_LIGHT : Data.DARK_COLOR);

        Ansi.SetCursorPosition(x + 24, y);
        Ansi.Write(' ');

        byte[] lastColor = new byte[3];
        for (int t = 0; t < usableWidth; t++) {
            byte[] color = DetermineRttColor(item.history[(historyOffset + t) % HISTORY_LEN]);
            if (!color.SequenceEqual(lastColor)) {
                Ansi.SetFgColor(color);
                Array.Copy(color, lastColor, 3);
            }
            Ansi.Write(Data.PING_CELL);
        }

        Ansi.Write(' ');
        Ansi.SetFgColor(DetermineRttColor(item.status));
        Ansi.Write(DetermineRttText(item.status));

        Ansi.SetBgColor(Data.DARK_COLOR);
    }

    private void DrawStatus() {
        int total = list.items.Count;
        int unreachable = list.items.Count(o=> o.status < 0);

        string reachableString = $" {total - unreachable} ";
        string unreachableString = unreachable > 0 ? $" {unreachable} " : String.Empty;
        string totalString = $" {total} ";

        int w = reachableString.Length + unreachableString.Length + totalString.Length - 1;
        Ansi.SetCursorPosition(Renderer.LastWidth - w, Math.Max(Renderer.LastHeight - 1, 0));

        Ansi.SetFgColor([16, 16, 16]);
        Ansi.SetBgColor([128, 224, 48]);
        Ansi.Write(reachableString);

        if (unreachable > 0) {
            Ansi.SetFgColor([16, 16, 16]);
            Ansi.SetBgColor([240, 32, 32]);
            Ansi.Write(unreachableString);
        }

        Ansi.SetFgColor([16, 16, 16]);
        Ansi.SetBgColor(Data.LIGHT_COLOR);
        Ansi.Write(totalString);
    }

    private static byte[] DetermineRttColor(short rtt) => rtt switch {
        Icmp.TIMEDOUT        => [240, 32, 32],
        Icmp.UNREACHABLE     => [240, 128, 0],
        Icmp.INVALID_ADDRESS => [176, 0, 0],
        Icmp.GENERAL_FAILURE => [176, 0, 0],
        Icmp.ERROR           => [176, 0, 0],
        Icmp.UNKNOWN         => [176, 0, 0],
        Icmp.UNDEFINED       => Data.CONTROL_COLOR,
        < 5   => [128, 224, 48],
        < 10  => [48, 224, 128],
        < 20  => [48, 224, 160],
        < 50  => [48, 224, 224],
        < 100 => [64, 128, 224],
        < 200 => [128, 96, 232],
        < 400 => [160, 64, 232],
        _     => [224, 52, 192]
    };

    private static string DetermineRttText(short rtt) => rtt switch {
        Icmp.TIMEDOUT        => "timed out   ",
        Icmp.UNREACHABLE     => "unreachable ",
        Icmp.INVALID_ADDRESS => "invalid     ",
        Icmp.GENERAL_FAILURE => "failure     ",
        Icmp.ERROR           => "error       ",
        Icmp.UNKNOWN         => "unknown     ",
        Icmp.UNDEFINED       => "undefine    ",
        _ => $"{rtt}ms".PadRight(12, ' ')
    };

    private async Task PingLoop() {
        cancellationTokenSource = new CancellationTokenSource();
        cancellationToken = cancellationTokenSource.Token;

        Tokens.dictionary.TryAdd(cancellationTokenSource, cancellationToken);

        while (!cancellationToken.IsCancellationRequested) {
            long startTime = Stopwatch.GetTimestamp();
            short[] result = await Icmp.PingArrayAsync(list.items, timeout);

            if (list.items.Count != result.Length) continue;

            ringIndex++;

            int moveCounter = 0;
            (int left, int top, int width, int height) = list.GetBounding();

            for (int i = 0; i < list.items.Count && i < result.Length; i++) {
                short status = result[i];
                short lastStatus = list.items[i].status;

                list.items[i].status = status;
                list.items[i].history[ringIndex % HISTORY_LEN] = status;

                bool shouldMoveToTop = lastStatus != Icmp.UNDEFINED && move switch {
                    MoveOption.OnRiseAndFall => lastStatus < 0 != status < 0,
                    MoveOption.OnRise        => lastStatus < 0 && status >= 0,
                    MoveOption.OnFall        => lastStatus >= 0 && status < 0,
                    _ => false
                };

                if (shouldMoveToTop) {
                    PingItem item = list.items[i];
                    list.items.RemoveAt(i);
                    list.items.Insert(moveCounter, item);
                }

                if (Renderer.ActiveDialog is not null) continue;
                if (Renderer.ActiveFrame != this) continue;

                if (shouldMoveToTop && moveCounter >= list.scrollOffset) { //redraw if in viewport
                    int movedAdjustedY = top + moveCounter * 2 - list.scrollOffset * list.itemHeight;

                    if (movedAdjustedY >= top && movedAdjustedY <= top + height - 1) {
                        DrawItemLabel(list.items[moveCounter], moveCounter, movedAdjustedY);
                        UpdatePingItem(moveCounter, left, movedAdjustedY, width);
                    }
                }

                if (shouldMoveToTop) {
                    moveCounter++;
                }

                int adjustedY = top + i * 2 - list.scrollOffset * list.itemHeight;
                if (adjustedY < top) continue;
                if (adjustedY > top + height - 1) continue;

                if (ringIndex > 0) {
                    DrawItemLabel(list.items[i], i, adjustedY);
                }
                UpdatePingItem(i, left, adjustedY, width);
            }

            if (Renderer.ActiveFrame == this && Renderer.ActiveDialog is null) {
                Ansi.Push();
            }

            ringIndex %= HISTORY_LEN;

            int elapsed = Stopwatch.GetElapsedTime(startTime).Milliseconds;
            if (elapsed < interval) {
                try {
                    await Task.Delay(interval - elapsed, cancellationToken);
                }
                catch (TaskCanceledException) { }
            }
        }

        Tokens.dictionary.TryRemove(cancellationTokenSource, out _);
        cancellationTokenSource.Dispose();
    }

    private void ParseQuery(string query) {
        if (query.Contains(';')) {
            foreach (string host in query.Split(';').Select(o => o.Trim())){
                AddItem(host);
            }
        }
        else if (query.Contains(',')) {
            foreach (string host in query.Split(',').Select(o => o.Trim())) {
                AddItem(host);
            }
        }
        else if (query.Contains('-')) {
            string[] split = query.Split('-');
            string[] start = split[0].Trim().Split('.');
            string[] end   = split[1].Trim().Split('.');

            if (start.Length == 4 && end.Length == 4 && start.All(o => int.TryParse(o, out _)) && end.All(o => int.TryParse(o, out _))) {
                int iStart = (int.Parse(start[0]) << 24) + (int.Parse(start[1]) << 16) + (int.Parse(start[2]) << 8) + int.Parse(start[3]);
                int iEnd = (int.Parse(end[0]) << 24) + (int.Parse(end[1]) << 16) + (int.Parse(end[2]) << 8) + int.Parse(end[3]);

                if (iStart > iEnd) iEnd = iStart;
                if (iEnd - iStart > 1024) iEnd = iStart + 1024;

                for (int i = iStart; i <= iEnd; i++) {
                    int value = i;
                    byte[] bytes = new byte[4];
                    for (int j = 3; j >= 0; j--) {
                        bytes[j] = (byte)(value & 255);
                        value >>= 8;
                    }
                    AddItem(String.Join('.', bytes.Select(b => b.ToString())));
                }
            }
            else {
                AddItem(query);
            }
        }
        else if (query.Contains('/')) {
            string[] parts = query.Split('/');
            if (parts.Length != 2 || !int.TryParse(parts[1].Trim(), out int cidr)) return;
            cidr = Math.Clamp(cidr, 16, 31);

            string ip = parts[0].Trim();
            string[] ipBytes = ip.Split('.');
            if (ipBytes.Length != 4 || !ipBytes.All(o => int.TryParse(o, out _))) return;

            int[] ipIntBytes = ipBytes.Select(o => int.Parse(o)).ToArray();

            string bits = new string('1', cidr).PadRight(32, '0');
            int[] mask = [
                Convert.ToInt32(bits[..8], 2),
                Convert.ToInt32(bits[8..16], 2),
                Convert.ToInt32(bits[16..24], 2),
                Convert.ToInt32(bits[24..], 2),
            ];

            int[] net = new int[4];
            int[] broadcast = new int[4];

            for (int i = 0; i < 4; i++) {
                net[i] = ipIntBytes[i] & mask[i];
                broadcast[i] = ipIntBytes[i] | (255 - mask[i]);
            }

            ParseQuery(String.Join(".", net) + " - " + String.Join(".", broadcast));
        }
        else {
            AddItem(query);
        }
    }

    private void AddItem(string host) {
        if (String.IsNullOrEmpty(host)) return;

        PingItem item = list.items.Find(o => o.host.Equals(host, StringComparison.OrdinalIgnoreCase));

        if (item is null) {
            list.Add(new PingItem {
                host    = host,
                status  = Icmp.UNDEFINED,
                history = Enumerable.Repeat(Icmp.UNDEFINED, HISTORY_LEN).ToArray(),
                ping    = new()
            });
        }
        else {
            list.items.Remove(item);
            list.Add(item);
        }

        //(int left, int top, int width, _) = list.GetBounding();
        //list.drawItemHandler(list.items.Count - 1, left, top, width);
    }

    private void Start() => Task.Run(PingLoop);

    private void Stop() =>
        cancellationTokenSource?.Cancel();

    private void AddDialog() {
        Tui.InputDialog dialog = new Tui.InputDialog() {
            text = "Enter IP, domain or hostname:",
        };

        dialog.valueTextbox.enableHistory = true;
        dialog.valueTextbox.history = queryHistory;

        dialog.okButton.action = () => {
            if (!String.IsNullOrWhiteSpace(dialog.valueTextbox.Value)) {
                queryHistory.Add(dialog.valueTextbox.Value.Trim());
            }
            ParseQuery(dialog.valueTextbox.Value.Trim());
            dialog.Close();
        };

        dialog.Show(true);
    }

    private void RemoveSelected() {
        list.RemoveSelected()?.Dispose();
    }

    private void Clear() {
        ClearDialog dialog = new ClearDialog();

        dialog.okButton.action = () => {
            switch (dialog.clearSelectBox.index) {
            case 0: //clear all
                foreach (PingItem item in list.items) {
                    item?.Dispose();
                }
                list.Clear();
                break;

            case 1: //remove reachable
                list.items.RemoveAll(r => r.history.Any(o => o > -1));
                list.Draw(true);
                break;

            case 2://remove unreachable
                list.items.RemoveAll(r => r.history.All(o => o < 0));
                list.Draw(true);
                break;
            }

            dialog.Close();
        };

        dialog.Show(true);
    }

    private void ToggleStatus() {
        status = !status;

        if (status) {
            Start();
            toolbar.items[2].text = "Pause";
        }
        else {
            Stop();
            toolbar.items[2].text = "Start";
        }

        toolbar.Draw(true);
    }

    private void OptionsDialog() {
        OptionsDialog dialog = new OptionsDialog();

        dialog.okButton.action = () => {
            _ = int.TryParse(dialog.timeoutTextbox.Value, out timeout);
            _ = int.TryParse(dialog.intervalTextbox.Value, out interval);

            timeout  = Math.Clamp(timeout, 50, 10_000);
            interval = Math.Clamp(interval, 100, 30_000);
            move     = (MoveOption)dialog.moveSelectBox.index;

            if (timeout > interval) {
                timeout = interval;
            }

            dialog.Close();
        };

        dialog.moveSelectBox.index = (int)move;

        dialog.Show(true);

        dialog.intervalTextbox.Value = interval.ToString();
        dialog.timeoutTextbox.Value = timeout.ToString();
    }
}

file sealed class ClearDialog : Tui.DialogBox {
    public Tui.SelectBox clearSelectBox;

    public ClearDialog() {
        clearSelectBox = new Tui.SelectBox(this) {
            options = ["Clear all", "Remove reachable", "Remove unreachable"],
        };

        elements.Add(clearSelectBox);

        defaultElement = clearSelectBox;
    }

    public override void Draw(int width, int height) {
        int left = (Renderer.LastWidth - width) / 2 + 1;
        int top = 1;

        string blank = new String(' ', width);

        Ansi.SetFgColor([16, 16, 16]);
        Ansi.SetBgColor(Data.PANE_COLOR);

        Ansi.SetCursorPosition(left, top);
        Ansi.Write(blank);

        Ansi.SetCursorPosition(left, top);
        Ansi.Write(blank);

        clearSelectBox.left = left;
        clearSelectBox.right = Renderer.LastWidth - width - left + 2;
        clearSelectBox.top = top++;

        Ansi.SetCursorPosition(left, top++);
        Ansi.Write(blank);

        for (int i = 0; i < 3; i++) {
            Ansi.SetCursorPosition(left, top + i);
            Ansi.Write(blank);
        }

        okButton.left = left + (width - 20) / 2;
        okButton.top = top;

        cancelButton.left = left + (width - 20) / 2 + 10;
        cancelButton.top = top;

        if (elements is null) return;
        for (int i = 0; i < elements?.Count; i++) {
            elements[i].Draw(false);
        }

        if (focusedElement == clearSelectBox) {
            clearSelectBox.Focus();
        }

        Ansi.Push();
    }

    public override void Draw() {
        int width = Math.Min(Renderer.LastWidth, 48);
        Draw(width, 0);
    }

    public override bool HandleKey(ConsoleKeyInfo key) {
        switch (key.Key) {
        case ConsoleKey.Escape:
            Close();
            return true;

        case ConsoleKey.Enter:
            if (focusedElement == clearSelectBox) {
                okButton.action();
                return true;
            }
            else {
                return base.HandleKey(key);
            }

        default:
            return base.HandleKey(key);
        }
    }

    public override void Close() {
        Ansi.HideCursor();
        base.Close();
    }
}

file sealed class OptionsDialog : Tui.DialogBox {
    public Tui.IntegerBox timeoutTextbox;
    public Tui.IntegerBox intervalTextbox;
    public Tui.SelectBox moveSelectBox;

    public OptionsDialog() {
        timeoutTextbox = new Tui.IntegerBox(this) {
            backColor = Data.PANE_COLOR,
            min = 50,
            max = 5_000
        };

        intervalTextbox = new Tui.IntegerBox(this) {
            backColor = Data.PANE_COLOR,
            min = 100,
            max = 10_000
        };

        moveSelectBox = new Tui.SelectBox(this) {
            options = ["Never", "On rise", "On fall", "On rise and fall"],
        };

        elements.Add(timeoutTextbox);
        elements.Add(intervalTextbox);
        elements.Add(moveSelectBox);

        defaultElement = timeoutTextbox;
    }

    public override void Draw(int width, int height) {
        int left = (Renderer.LastWidth - width) / 2 + 1;
        int top = 1;

        string blank = new String(' ', width);

        Ansi.SetFgColor([16, 16, 16]);
        Ansi.SetBgColor(Data.PANE_COLOR);

        Ansi.SetCursorPosition(left, top);
        Ansi.Write(blank);

        WriteLabel("Timed out (ms):", left, ++top, width);
        timeoutTextbox.left = left + 16;
        timeoutTextbox.right = Renderer.LastWidth - width - left + 2;
        timeoutTextbox.top = top++ - 1;

        Ansi.SetCursorPosition(left, top);
        Ansi.Write(blank);

        WriteLabel("Interval (ms):", left, ++top, width);
        intervalTextbox.left = left + 16;
        intervalTextbox.right = Renderer.LastWidth - width - left + 2;
        intervalTextbox.top = top++ - 1;

        Ansi.SetCursorPosition(left, top);
        Ansi.Write(blank);

        WriteLabel("Move on top:", left, ++top, width);
        moveSelectBox.left = left + 16;
        moveSelectBox.right = Renderer.LastWidth - width - left + 2;
        moveSelectBox.top = top++ - 1;

        for (int i = 0; i < 3; i++) {
            Ansi.SetCursorPosition(left, top + i);
            Ansi.Write(blank);
        }

        okButton.left = left + (width - 20) / 2;
        okButton.top = top;

        cancelButton.left = left + (width - 20) / 2 + 10;
        cancelButton.top = top;

        if (elements is null) return;

        for (int i = 0; i < elements?.Count; i++) {
            elements[i].Draw(false);
        }

        if (focusedElement == timeoutTextbox) {
            timeoutTextbox.Focus();
        }
        else if (focusedElement == intervalTextbox) {
            intervalTextbox.Focus();
        }

        Ansi.Push();
    }

    public override void Draw() {
        int width = Math.Min(Renderer.LastWidth, 48);
        Draw(width, 0);
    }

    public override bool HandleKey(ConsoleKeyInfo key) {
        switch (key.Key) {
        case ConsoleKey.Escape:
            Close();
            return true;

        case ConsoleKey.Enter:
            if (focusedElement == timeoutTextbox || focusedElement == intervalTextbox || focusedElement == moveSelectBox) {
                okButton.action();
                return true;
            }
            else {
                return base.HandleKey(key);
            }

        default:
            return base.HandleKey(key);
        }
    }

    public override void Close() {
        Ansi.HideCursor();
        base.Close();
    }
}