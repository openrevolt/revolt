namespace Revolt.Tui;

public sealed class Toggle(Frame parentFrame, string text) : Element(parentFrame) {
    public string text = text;
    public Action action;

    private bool _value = default;
    public bool Value {
        get {
            return _value;
        }
        set {
            _value = value;
            DrawValue(true);
        }
    }

    public override void Draw(bool push) {
        (int left, int top, _, _) = GetBounding();

        Ansi.Color backColor = isFocused ? Glyphs.FOCUS_COLOR : Glyphs.DIALOG_COLOR;

        DrawValue(false);

        Ansi.SetBgColor(Glyphs.DIALOG_COLOR);
        Ansi.Write(' ');

        Ansi.SetFgColor(Glyphs.DARK_COLOR);
        Ansi.SetBgColor(backColor);
        Ansi.Write(text);

        if (push) {
            Ansi.Push();
        }
    }

    private void DrawValue(bool push) {
        (int left, int top, _, _) = GetBounding();

        Ansi.SetCursorPosition(left, top);

        if (_value) {
            Ansi.SetBgColor(Glyphs.FOCUS_COLOR);
            Ansi.SetFgColor(Glyphs.DARK_COLOR);
            Ansi.Write(' ');
            Ansi.Write(' ');
            Ansi.Write(Glyphs.TOGGLE_BOX);
        }
        else {
            Ansi.SetBgColor(Glyphs.GRAY_COLOR);
            Ansi.SetFgColor(Glyphs.CONTROL_COLOR);
            Ansi.Write(Glyphs.TOGGLE_BOX);
            Ansi.Write(' ');
            Ansi.Write(' ');
        }

        if (push) {
            Ansi.Push();
        }
    }

    public override void HandleKey(ConsoleKeyInfo key) {
        if (key.Key == ConsoleKey.UpArrow) {
            parentFrame.FocusPrevious();
        }
        else if (key.Key == ConsoleKey.DownArrow) {
            parentFrame.FocusNext();
        }
        else if (key.Key == ConsoleKey.LeftArrow) {
            _value = false;
            DrawValue(true);
            if (action is not null) {
                action();
            }
        }
        else if (key.Key == ConsoleKey.RightArrow) {
            _value = true;
            DrawValue(true);
            if (action is not null) {
                action();
            }
        }
        else if (key.Key == ConsoleKey.Enter || key.Key == ConsoleKey.Spacebar) {
            _value = !_value;
            DrawValue(true);
            if (action is not null) {
                action();
            }
        }
    }
}
