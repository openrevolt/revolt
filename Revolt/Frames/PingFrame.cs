using Revolt.Protocols;

namespace Revolt.Frames;

public sealed class PingFrame : Ui.Frame {
    const int HISTORY_LEN = 160;
 
    public struct PingItem {
        public string  host;
        public short   status;
        public short[] history;
    }

    public static readonly PingFrame singleton;

    public Ui.Toolbar toolbar;
    public Ui.ListBox<PingItem> list;

    private CancellationTokenSource cancellationTokenSource;
    private CancellationToken cancellationToken;

    private bool status = true;
    private int rotatingIndex = 0;
    private readonly List<string> history = [];

    static PingFrame() {
        singleton = new PingFrame();
    }

    public PingFrame() {
        toolbar = new Ui.Toolbar(this) {
            left  = 1,
            right = 1,
            items = [
                new Ui.Toolbar.ToolbarItem() { text="Add",     action=AddDialog},
                new Ui.Toolbar.ToolbarItem() { text="Pause",   action=ToggleStatus },
                new Ui.Toolbar.ToolbarItem() { text="Clear",   action=Clear },
                new Ui.Toolbar.ToolbarItem() { text="Options", action=Options },
            ]
        };

        list = new Ui.ListBox<PingItem>(this) {
            left   = 1,
            right  = 1,
            top    = 3,
            bottom = 1,
            drawItemHandler = DrawPingItem
        };

        elements.Add(toolbar);
        elements.Add(list);

        defaultElement = toolbar;
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
            MainMenu.singleton.Show();
            break;

        default:
            focusedElement.HandleKey(key);
            break;
        }

        return true;
    }

    private void DrawPingItem(int i, int x, int y, int width) {
        if (list.items is null || list.items.Count == 0) return;
        if (i >= list.items.Count) return;

        int yPos = y + i * 2;
        int pingCellPosX = x + 25;
        int usableWidth = Math.Min(width - 38, HISTORY_LEN);
        int historyOffset = (rotatingIndex + HISTORY_LEN - usableWidth + 1) % HISTORY_LEN;

        PingItem item = list.items[i];

        Ansi.SetCursorPosition(x, yPos);
        if (i == list.index) {
            Ansi.SetFgColor(list.isFocused ? [16, 16, 16] : Data.FG_COLOR);
            Ansi.SetBgColor(list.isFocused ? Data.SELECT_COLOR : Data.INPUT_COLOR);
        }
        else {
            Ansi.SetFgColor(Data.FG_COLOR);
            Ansi.SetBgColor(Data.BG_COLOR);
        }

        Ansi.Write(item.host.Length > 24 ? item.host[..23] + Data.ELLIPSIS : item.host.PadRight(24));

        Ansi.SetBgColor(Data.BG_COLOR);
        Ansi.SetCursorPosition(pingCellPosX, yPos);

        for (int t = 0; t < usableWidth; t++) {
            Ansi.SetFgColor(RttColor(item.history[(historyOffset + t) % HISTORY_LEN]));
            Ansi.Write(Data.PING_CELL);
        }

        Ansi.Write(' ');

        Ansi.SetFgColor(RttColor(item.status));
        Ansi.Write(RttText(item.status));

        Ansi.Push();
    }

    private void UpdatePingItem(int i, int x, int y, int width) {
        int yPos = y + i * 2;
        int pingCellPosX = x + 25;
        int usableWidth = Math.Min(width - 38, HISTORY_LEN);
        int historyOffset = (rotatingIndex + HISTORY_LEN - usableWidth + 1) % HISTORY_LEN;

        PingItem item = list.items[i];

        Ansi.SetBgColor(Data.BG_COLOR);
        Ansi.SetCursorPosition(pingCellPosX, yPos);

        for (int t = 0; t < usableWidth; t++) {
            Ansi.SetFgColor(RttColor(item.history[(historyOffset + t) % HISTORY_LEN]));
            Ansi.Write(Data.PING_CELL);
        }

        Ansi.Write(' ');

        Ansi.SetFgColor(RttColor(item.status));
        Ansi.Write(RttText(item.status));

        Ansi.Push();
    }

    private static byte[] RttColor(short rtt) => rtt switch {
        Icmp.TIMEDOUT        => [240, 32, 32],
        Icmp.UNREACHABLE     => [240, 128, 0],
        Icmp.INVALID_ADDREDD => [192, 0, 0],
        Icmp.GENERAL_FAILURE => [192, 0, 0],
        Icmp.ERROR           => [192, 0, 0],
        Icmp.UNKNOWN         => [192, 0, 0],
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

    private static string RttText(short rtt) => rtt switch {
        Icmp.TIMEDOUT        => "timed out   ",
        Icmp.UNREACHABLE     => "unreachable ",
        Icmp.INVALID_ADDREDD => "invalid     ",
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
            (int left, int top, int width, _) = list.GetBounding();
            string[] hosts = list.items.Select(o => o.host).ToArray();

            short[] result = await Icmp.PingArrayAsync(hosts, 1000);
            rotatingIndex++;

            for (int i = 0; i < list.items.Count && i < result.Length; i++) {
                short status = result[i];

                PingItem item = list.items[i];
                item.status = status;
                item.history[rotatingIndex % HISTORY_LEN] = status;
                list.items[i] = item;

                if (Renderer.ActiveFrame == this && Renderer.ActiveDialog == null) {
                    UpdatePingItem(i, left, top, width);
                }
            }

            Thread.Sleep(1000);
            rotatingIndex %= HISTORY_LEN;
        }

        Tokens.dictionary.TryRemove(cancellationTokenSource, out _);
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
                    AddItem(string.Join('.', bytes.Select(b => b.ToString())));
                }
            }
            else {
                AddItem(query);
            }
        }
        else if (query.Contains('/')) {
            string[] parts = query.Split('/');
            if (parts.Length != 2 || !int.TryParse(parts[1].Trim(), out int cidr)) return;

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

            ParseQuery(string.Join(".", net) + " - " + string.Join(".", broadcast));
        }
        else {
            AddItem(query);
        }
    }

    private void AddItem(string host) {
        if (String.IsNullOrEmpty(host)) return;

        list.Add(new PingItem {
            host    = host,
            status  = Icmp.UNDEFINED,
            history = Enumerable.Repeat(Icmp.UNDEFINED, HISTORY_LEN).ToArray()
        });

        (int left, int top, int width, _) = list.GetBounding();
        list.drawItemHandler(list.items.Count - 1, left, top, width);
    }

    private void Start() =>
        new Thread(async () => await PingLoop()).Start();

    private void Stop() =>
        cancellationTokenSource.Cancel();

    private void AddDialog() {
        Ui.InputDialog dialog = new Ui.InputDialog() {
            text = "Enter IP, domain or hostname:",
        };

        dialog.valueTextbox.enableHistory = true;
        dialog.valueTextbox.history = history;

        dialog.okButton.action = () => {
            history.Add(dialog.valueTextbox.Value.Trim());
            ParseQuery(dialog.valueTextbox.Value.Trim());
            dialog.Close();
        };

        Renderer.ActiveDialog = dialog;
        dialog.Draw();
    }

    private void Clear() {
        list.items.Clear();
        list.Draw(true);
    }

    private void ToggleStatus() {
        status = !status;

        if (status) {
            Start();
            toolbar.items[1].text = "Pause";
        }
        else {
            Stop();
            toolbar.items[1].text = "Start";
        }

        toolbar.Draw(true);
    }

    private void Options() { }

}