namespace Revolt.Frames;

public sealed class PingFrame : Ui.Frame {
    const int HISTORY_LEN = 160;
    
    const short TIMEDOUT        = -1;
    const short UNREACHABLE     = -2;
    const short INVALID_ADDREDD = -3;
    const short GENERAL_FAILURE = -4;
    const short ERROR           = -5;
    const short UNDEFINED       = -9;

    public struct PingItem {
        public string  host;
        public short   status;
        public short[] history;
    }

    public static readonly PingFrame singleton;

    public Ui.Toolbar toolbar;
    public Ui.ListBox<PingItem> list;
    public Ui.Textbox input;

    private CancellationTokenSource cancellationTokenSource;
    private CancellationToken cancellationToken;

    private bool status = true;
    private int rotatingIndex = 0;

    static PingFrame() {
        singleton = new PingFrame();
    }

    public PingFrame() {
        toolbar = new Ui.Toolbar(this) {
            left  = 1,
            right = 1,
            items = [
                new Ui.Toolbar.ToolbarItem() { text="Pause",   action=ToggleStatus },
                new Ui.Toolbar.ToolbarItem() { text="Clear",   action=Clear },
                new Ui.Toolbar.ToolbarItem() { text="Options", action=Options },
            ]
        };

        list = new Ui.ListBox<PingItem>(this) {
            left   = 1,
            right  = 1,
            top    = 3,
            bottom = 3,
            drawItemHandler = DrawPingItem
        };

        input = new Ui.Textbox(this) {
            left          = 2,
            right         = 2,
            bottom        = 0,
            enableHistory = true,
            placeholder   = "IP, domain or hostname",
            action = () => {
                ParseQuery(input.Value.Trim());
                input.Value = String.Empty;
            }
        };

        elements.Add(toolbar);
        elements.Add(list);
        elements.Add(input);

        defaultElement = input;
        FocusNext();

        if (status) {
            Start();
        }
    }

    public override void Draw(int width, int height) {
        input.top = height - 2;
        base.Draw(width, height);
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

        PingItem item = list.items[i];

        Ansi.SetCursorPosition(x, y + i*2);
        if (i == list.index) {
            Ansi.SetFgColor(list.isFocused ? [16, 16, 16] : Data.FG_COLOR);
            Ansi.SetBgColor(list.isFocused ? Data.SELECT_COLOR : Data.INPUT_COLOR);
        }
        else {
            Ansi.SetFgColor(Data.FG_COLOR);
            Ansi.SetBgColor(Data.BG_COLOR);
        }

        if (item.host.Length > 24) {
            Console.Write(item.host[..23]);
            Console.Write(Data.ELLIPSIS);
        }
        else {
            Console.Write(item.host);
            Console.Write(new String(' ', 24 - item.host.Length));
        }

        Ansi.SetBgColor(Data.BG_COLOR);

        Ansi.SetCursorPosition(x + 25, y + i*2);
        for (int t = 0; t < Math.Min(width - 36, HISTORY_LEN); t++) {
            Ansi.SetFgColor(RttColor(item.history[t]));
            Console.Write(Data.PING_CELL);
        }

        Ansi.SetFgColor(Data.FG_COLOR);
        Ansi.SetBgColor(Data.BG_COLOR);

        string status = $"{item.status}ms".PadLeft(6, ' ');
        Ansi.SetCursorPosition(x + width - 10, y + i*2);
        Console.Write(status);
        Console.Write(new String(' ', 11 - status.ToString().Length));
    }

    private void UpdatePingItem(int i, int x, int y, int width, short stauts) {

    }

    private static byte[] RttColor(short rtt) => rtt switch {
        TIMEDOUT => [240, 80, 24],
        UNREACHABLE => [232, 118, 0],
        INVALID_ADDREDD => [255, 0, 0],
        GENERAL_FAILURE => [255, 0, 0],
        ERROR => [255, 0, 0],
        UNDEFINED => Data.CONTROL_COLOR,

        < 10 => [128, 224, 48],
        < 100 => [48, 224, 228],
        < 200 => [48, 140, 224],
        < 400 => [128, 64, 232],
        _ => [240, 48, 160]
    };

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
            host = host,
            status = 0,
            history = Enumerable.Repeat(UNDEFINED, HISTORY_LEN).ToArray()
        });

        (int left, int top, int width, _) = list.GetBounding();
        list.drawItemHandler(list.items.Count - 1, left, top, width);
    }

    private void Start() =>
        new Thread(PingLoop).Start();

    private void Stop() =>
        cancellationTokenSource.Cancel();

    private void PingLoop() {
        cancellationTokenSource = new CancellationTokenSource();
        cancellationToken = cancellationTokenSource.Token;

        while (!cancellationToken.IsCancellationRequested) {
            /*
            (int left, int top, int width, _) = list.GetBounding();

            for (int j = 0; j < HISTORY_LEN; j++) {

                for (int i = 0; i < list.items.Count; i++) {
                    int t = (rotatingIndex + j) % HISTORY_LEN;
                    list.items[i].history[t] = 0;

                    if (Renderer.ActiveFrame == this) {
                        DrawPingItem(i, left, top, width);
                    }

                }
            }*/

            Thread.Sleep(1000);
            rotatingIndex++;
        }
    }

    /*private void Add() {
        Ui.InputDialog dialog = new Ui.InputDialog() {
            text = "Enter IP, domain or hostname:",
        };

        dialog.okButton.action = () => {
            AddItem(dialog.valueTextbox.Value);
            dialog.Close();
        };

        Renderer.Dialog = dialog;
    dialog.Draw();
    }*/

    private void Clear() {
        list.items.Clear();
        list.Draw();
    }

    private void ToggleStatus() {
        status = !status;

        if (status) {
            Start();
            toolbar.items[0].text = "Pause";
        }
        else {
            Stop();
            toolbar.items[0].text = "Start";
        }

        toolbar.Draw();
    }

    private void Options() { }

}