namespace Revolt.Frames;

public sealed class PingFrame : Ui.Frame {
    public struct PingItem {
        public string  host;
        public short   status;
        public short[] history;
    }

    private bool status = true;

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
                new Ui.Toolbar.ToolbarItem() { text="Add",     action=Add },
                new Ui.Toolbar.ToolbarItem() { text="Clear",   action=Clear },
                new Ui.Toolbar.ToolbarItem() { text="Pause",   action=ToggleStatus },
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
            left   = 1,
            right  = 1,
            top    = -1,
            bottom = 0
        };

        elements.Add(toolbar);
        elements.Add(list);
        elements.Add(input);

        defaultElement = toolbar;
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
            Console.Write(item.host[..24]);
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

    private void Add() {
        Ui.InputDialog dialog = new Ui.InputDialog() {
            text = "Enter IP, domain or hostname:",
        };

        dialog.okButton.action = () => {
            AddItem(dialog.valueTextbox.Value);
            dialog.Close();
        };

        Renderer.Dialog = dialog;
        dialog.Draw();
    }

    private void AddItem(string host) {
        if (String.IsNullOrEmpty(host)) return;

        list.Add(new PingItem {
            host = host,
            status = 0,
            history = new short[160]
        });
    }

    private void Clear() {
        list.items.Clear();
        list.Draw();
    }

    private void ToggleStatus() {
        status = !status;

        if (status) {
            toolbar.items[2].text = "Pause";
        }
        else {
            toolbar.items[2].text = "Start";
        }

        toolbar.Draw();
    }

    private void Options() { }

}