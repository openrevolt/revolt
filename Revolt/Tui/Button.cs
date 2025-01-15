namespace Revolt.Tui;

public sealed class Button(Frame parentFrame, string text) : Element(parentFrame) {
    public string text = text;
    public Action action;

    public override void Draw(bool push) {
        (int left, int top, _, _) = GetBounding();

        byte[] foreColor = isFocused ? [16, 16, 16] : Data.LIGHT_COLOR;
        byte[] backColor = isFocused ? Data.SELECT_COLOR : Data.CONTROL_COLOR;

        Ansi.SetFgColor(foreColor);
        Ansi.SetBgColor(backColor);
        Ansi.SetCursorPosition(left, top);

        Ansi.Write(text);

        Ansi.SetFgColor(backColor);
        Ansi.SetBgColor(Data.DIALOG_COLOR);
        Ansi.SetCursorPosition(left, top + 1);

        Ansi.Write(new String(Data.UPPER_1_8TH_BLOCK, text.Length));

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
        else if ((key.Key == ConsoleKey.Enter || key.Key == ConsoleKey.Spacebar) && action is not null) {
            action();
        }
    }
}
