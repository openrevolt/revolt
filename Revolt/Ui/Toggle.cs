namespace Revolt.Ui;

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

        byte[] backColor = isFocused ? Data.SELECT_COLOR : Data.PANE_COLOR;

        DrawValue(false);

        Ansi.SetCursorPosition(left + 2, top);
        Ansi.SetFgColor(Data.BG_COLOR);
        Ansi.SetBgColor(backColor);
        Ansi.Write(text);

        if (push) {
            Ansi.Push();
        }
    }

    private void DrawValue(bool push) {
        (int left, int top, _, _) = GetBounding();

        Ansi.SetCursorPosition(left, top);

        Ansi.SetFgColor(Data.CONTROL_COLOR);
        Ansi.SetBgColor(Data.PANE_COLOR);

        Ansi.Write(_value ? Data.TOGGLE_ON : Data.TOGGLE_OFF);

        if (push) {
            Ansi.Push();
        }
    }

    public override void HandleKey(ConsoleKeyInfo key) {
        if (key.Key == ConsoleKey.LeftArrow || key.Key == ConsoleKey.UpArrow) {
            parentFrame.FocusPrevious();
        }
        else if (key.Key == ConsoleKey.RightArrow || key.Key == ConsoleKey.DownArrow) {
            parentFrame.FocusNext();
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
