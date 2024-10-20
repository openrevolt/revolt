namespace Revolt.Ui;

public sealed class Textbox(Frame parentFrame) : Element(parentFrame) {
    string value = String.Empty;
    int index = 0;

    public override void Draw() {
        (int left, int top, int width, _) = GetBounding();
        int calculatedWidth = width - 2;

        Ansi.SetFgColor(Data.FG_COLOR);
        Ansi.SetBgColor(Data.INPUT_COLOR);
        Ansi.SetCursorPosition(left, top);

        Console.Write(new string(' ', calculatedWidth));

        Ansi.SetFgColor(isFocused ? Data.SELECT_COLOR : Data.INPUT_COLOR);
        Ansi.SetBgColor(Data.PANE_COLOR);
        Ansi.SetCursorPosition(left, top + 1);

        Console.Write(new string(Data.UPPER_1_8TH_BLOCK, calculatedWidth));

        DrawValue();
    }

    private void DrawValue() {
        (int left, int top, int width, _) = GetBounding();

        Ansi.SetFgColor(Data.FG_COLOR);
        Ansi.SetBgColor(Data.INPUT_COLOR);
        Ansi.SetCursorPosition(left + index, top);

    }

    public override void HandleKey(ConsoleKeyInfo key) {
        switch (key.Key) {
        case ConsoleKey.LeftArrow:
            if (key.Modifiers == ConsoleModifiers.Control) {

            }
            else {

            }
            break;

        case ConsoleKey.RightArrow:
            if (key.Modifiers == ConsoleModifiers.Control) {

            }
            else {

            }
            break;

        case ConsoleKey.Home:
            break;

        case ConsoleKey.End:
            break;

        case ConsoleKey.Backspace:
            if (key.Modifiers == ConsoleModifiers.Control) {
                
            }
            else {

            }

            if (index > 0) {
                index--;
            }

            break;

        case ConsoleKey.Delete:
            if (key.Modifiers == ConsoleModifiers.Control) {

            }
            else {

            }
            break;

        default:
            byte asci = (byte)key.KeyChar;

            if (asci > 31 && asci < 127) {
                value += key.KeyChar;
                index++;
                DrawValue();
            }
            break;
        }
    }

    public override void Focus(bool draw = true) {
        base.Focus(draw);
        Ansi.ShowCursor();
    }
    public override void Blur(bool draw = true) {
        base.Blur(draw);
        Ansi.HideCursor();
    }
}