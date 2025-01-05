namespace Revolt.Tui;

public sealed class SelectBox(Frame parentFrame) : Element(parentFrame) {
    public string[] options;
    public int index = 0;
    public Action afterChange;
    
    public override void Draw(bool push) {
        (int left, int top, int width, _) = GetBounding();
        int usableWidth = Math.Max(width, 6);

        byte[] foreColor = isFocused ? [16, 16, 16] : Data.LIGHT_COLOR;
        byte[] backColor = isFocused ? Data.SELECT_COLOR : Data.INPUT_COLOR;
        string selected = options[index];

        Ansi.SetFgColor(foreColor);
        Ansi.SetBgColor(backColor);
        Ansi.SetCursorPosition(left, top);

        if (index == 0) {
            Ansi.SetFgColor([128, 128, 128]);
        }
        Ansi.Write(' ');
        Ansi.Write(Data.LEFT_TRIANGLE);
        Ansi.Write(' ');

        Ansi.SetFgColor(foreColor);
        Ansi.Write(selected);
        Ansi.Write(new string(' ', Math.Max(usableWidth - selected.Length - 6, 0)));

        if (index == options.Length - 1) {
            Ansi.SetFgColor([128, 128, 128]);
        }
        Ansi.Write(' ');
        Ansi.Write(Data.RIGHT_TRIANGLE);
        Ansi.Write(' ');

        if (push) {
            Ansi.Push();
        }
    }

    public override void HandleKey(ConsoleKeyInfo key) {
        int lastIndex = index;

        switch (key.Key) {
        case ConsoleKey.LeftArrow:
            index = Math.Max(index - 1, 0);
            Draw(true);
            break;

        case ConsoleKey.RightArrow:
            index = Math.Min(index + 1, options.Length - 1);
            Draw(true);
            break;
        }

        if (index != lastIndex && afterChange is not null) {
            afterChange();
        }
    }
}