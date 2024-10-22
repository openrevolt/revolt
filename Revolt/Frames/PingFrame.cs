using System.Runtime.CompilerServices;

namespace Revolt.Frames;

public sealed class PingFrame : Ui.Frame {
    public struct PingItem {
        public string  host;
        public short   status;
        public short[] history;
    }

    private bool status = true;
    //private int rotatingIndex = 0;

    public Ui.Toolbar toolbar;
    public Ui.ListBox<PingItem> list;
    public Ui.Textbox input;

    public static readonly PingFrame singleton;
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
            Renderer.ActiveFrame = MainMenu.singleton;
            Renderer.Redraw();
            break;

        default:
            focusedElement.HandleKey(key);
            break;
        }

        return true;
    }

    private void DrawPingItem(int idx, int x, int y, int width) {
        if (list.items is null || list.items.Count == 0) return;
        if (idx >= list.items.Count) return;

        PingItem item = list.items[idx];

        Ansi.SetCursorPosition(x, y + idx*2);
        if (idx == list.index) {
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

        Ansi.SetCursorPosition(x + 25, y + idx*2);
        for (int i = 0; i < Math.Min(width - 36, item.history.Length); i++) {
            Ansi.SetFgColor([32, 224, 32]);
            Console.Write(Data.PING_CELL);
        }

        Ansi.SetFgColor(Data.FG_COLOR);
        Ansi.SetBgColor(Data.BG_COLOR);

        string status = item.status + "ms";
        Ansi.SetCursorPosition(x + width - 10, y + idx*2);
        Console.Write(status);
        Console.Write(new String(' ', 11 - status.ToString().Length));
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
            string[] split = query.Split("-");
            string[] start = split[0].Trim().Split(".");
            string[] end  = split[1].Trim().Split(".");

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
                    AddItem(string.Join(".", bytes.Select(b => b.ToString())));
                }
            }
            else {
                AddItem(query);
            }
        }
        else if (query.Contains('/')) {
            string[] parts = query.Split('/');
            if (parts.Length != 2 || !int.TryParse(parts[1].Trim(), out int cidr)) {
                return;
            }

            string ip = parts[0].Trim();
            string[] ipBytes = ip.Split('.');
            if (ipBytes.Length != 4 || !ipBytes.All(o => int.TryParse(o, out _))) {
                return;
            }

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

            string networkRange = string.Join(".", net) + " - " + string.Join(".", broadcast);
            ParseQuery(networkRange);
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
            history = new short[160]
        });

        list.Draw();
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
            toolbar.items[0].text = "Pause";
        }
        else {
            toolbar.items[0].text = "Start";
        }

        toolbar.Draw();
    }

    private void Options() { }

}