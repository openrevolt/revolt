namespace Revolt.Ui;

public sealed class Textbox(Frame parentFrame) : Element(parentFrame) {
    string value = "test"; //String.Empty;

    public override void Draw() {
        (int left, int top, int width, _) = GetBounding();

        Ansi.SetFgColor(Data.FG_COLOR);
        Ansi.SetBgColor(Data.INPUT_COLOR);
        Ansi.SetCursorPosition(left, top);

        int calculatedWidth = width - 2;

        Console.Write(value);
        if (value.Length + 1 < calculatedWidth) {
            Console.Write(new string(' ', calculatedWidth - value.Length));
        }

        Ansi.SetFgColor(isFocused ? Data.SELECT_COLOR : Data.INPUT_COLOR);
        Ansi.SetBgColor(Data.PANE_COLOR);
        Ansi.SetCursorPosition(left, top + 1);

        for (int i = 0; i < calculatedWidth; i++) {
            Console.Write(Data.UPPER_1_8TH_BLOCK);
        }

        Ansi.SetFgColor(Data.FG_COLOR);
        Ansi.SetBgColor(Data.INPUT_COLOR);
        Ansi.SetCursorPosition(left + value.Length, top);
    }

    public override void HandleKey(ConsoleKeyInfo key) {
        switch (key.Key) {
        case ConsoleKey.LeftArrow:
            break;

        case ConsoleKey.RightArrow:
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
