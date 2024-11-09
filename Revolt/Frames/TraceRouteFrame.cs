namespace Revolt.Frames;

public sealed class TraceRouteFrame : Ui.Frame {
    public struct TrItem {
        public string hop;
        public int rrt;
    }

    public static TraceRouteFrame Instance { get; } = new TraceRouteFrame();

    public Ui.Toolbar toolbar;
    public Ui.Textbox textbox;
    public Ui.ListBox<TrItem> list;

    public TraceRouteFrame() {
        toolbar = new Ui.Toolbar(this) {
            left = 1,
            right = 1,
            items = [
                new Ui.Toolbar.ToolbarItem() { text="Clear",   action=Clear },
            ]
        };

        textbox = new Ui.Textbox(this) {
            top = 4,
            left = 16,
            right = 16
        };

        list = new Ui.ListBox<TrItem>(this) {
            left = 1,
            right = 1,
            top = 9,
            bottom = 1,
            drawItemHandler = DrawTrItem
        };

        elements.Add(toolbar);
        elements.Add(textbox);
        elements.Add(list);

        defaultElement = textbox;
        FocusNext();
    }

    public override void Show(bool draw = true) {
        Ansi.HideCursor();
        base.Show(draw);
    }

    public override void Draw(int width, int height) {
        string blank = new string(' ', width);

        int top = 0;
        Ansi.SetBgColor(Data.BG_COLOR);
        for (int y = top; y <= height; y++) {
            if (y > Console.WindowHeight) break;

            Ansi.SetCursorPosition(0, y);
            Ansi.Write(blank);
        }

        int padding = width < 64 ? 1 : 16;
        textbox.left = padding;
        textbox.right = padding;

        Ansi.SetFgColor(Data.FG_COLOR);
        Ansi.SetBgColor(Data.BG_COLOR);
        WriteLabel("Target:", padding, 4, width - padding);

        Ansi.SetCursorPosition(0, 7);
        Ansi.SetBgColor(Data.BG_COLOR);
        Ansi.Write(new String(' ', (width - 30) / 2));

        Ansi.SetBgColor(Data.SELECT_COLOR_LIGHT);
        Ansi.Write(new String(' ', 30));

        Ansi.SetBgColor(Data.BG_COLOR);
        Ansi.Write(new String(' ', (width - 30) / 2));

        for (int i = 0; i < elements.Count; i++) {
            elements[i].Draw(false);
        }

        Ansi.Push();
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

        case ConsoleKey.Enter:
            if (focusedElement == textbox) {
                Trace(textbox.Value);
                textbox.Value = String.Empty;
                list.Clear();
            }
            break;

        default:
            focusedElement?.HandleKey(key);
            break;
        }

        return true;
    }

    private void DrawTrItem(int index, int x, int y, int width) {
        if (list.items is null || list.items.Count == 0) return;
        if (index >= list.items.Count) return;

        int adjustedY = y + index - list.scrollOffset * list.itemHeight;
        if (adjustedY < y || adjustedY > Renderer.LastHeight) return;

        Ansi.SetCursorPosition(2, adjustedY);
        Ansi.Write("Malakia");

        Ansi.Push();
    }

    private void Clear() {
        list.Clear();
    }

    private void Trace(string host) {

    }
}
