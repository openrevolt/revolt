namespace Revolt.Ui;

public sealed class Toolbar(Frame parentFrame) : Element(parentFrame) {

    public struct ToolbarItem {
        public string text;
        public Action action;
    }

    public ToolbarItem[] items;
    public int index = 0;

    public override void Draw(bool push) {
        (int left, _, int width, _) = GetBounding();

        int offset = 0;

        for (int i = 0; i < items.Length; i++) {
            offset += DrawItem(i, left + offset, isFocused && i == index);
            if (left + offset >= width) break;
            Ansi.Push(); //fixes tearing
        }

        Ansi.SetCursorPosition(offset + 2, 1);
        Ansi.SetFgColor(Data.TOOLBAR_COLOR);
        Ansi.Write(new String(Data.LOWER_3_8TH_BLOCK, width - offset));

        Ansi.SetCursorPosition(offset + 2, 2);
        Ansi.SetBgColor(Data.TOOLBAR_COLOR);
        Ansi.Write(new String(' ', width - offset));

        Ansi.SetCursorPosition(offset + 2, 3);
        Ansi.SetFgColor(Data.TOOLBAR_COLOR);
        Ansi.SetBgColor(Data.DARK_COLOR);
        Ansi.Write(new String(Data.UPPER_1_8TH_BLOCK, width - offset));

        Ansi.Push();
    }

    private int DrawItem(int i, int offset, bool isFocused) {
        Ansi.SetCursorPosition(offset, 1);

        Ansi.SetFgColor(Data.TOOLBAR_COLOR);
        Ansi.SetBgColor(Data.DARK_COLOR);
        Ansi.Write(Data.LOWER_3_8TH_BLOCK);

        Ansi.SetFgColor(isFocused && i == index ? Data.SELECT_COLOR : Data.CONTROL_COLOR);
        Ansi.Write(new String(Data.LOWER_3_8TH_BLOCK, items[i].text.Length + 2));


        Ansi.SetCursorPosition(offset, 2);

        Ansi.SetBgColor(Data.TOOLBAR_COLOR);
        Ansi.Write(' ');

        Ansi.SetFgColor(this.isFocused && i == index ? [16, 16, 16] : Data.LIGHT_COLOR);
        Ansi.SetBgColor(this.isFocused && i == index ? Data.SELECT_COLOR : Data.CONTROL_COLOR);
        Ansi.SetBold(true);
        Ansi.Write($" {items[i].text} ");
        Ansi.SetBold(false);


        Ansi.SetCursorPosition(offset, 3);

        Ansi.SetFgColor(Data.TOOLBAR_COLOR);
        Ansi.SetBgColor(Data.DARK_COLOR);
        Ansi.Write(Data.UPPER_1_8TH_BLOCK);

        Ansi.SetFgColor(isFocused && i == index ? Data.SELECT_COLOR : Data.CONTROL_COLOR);
        Ansi.Write(new String(Data.UPPER_1_8TH_BLOCK, items[i].text.Length + 2));

        return items[i].text.Length + 3;
    }

    public override void HandleKey(ConsoleKeyInfo key) {
        if (items is null || items.Length == 0) return;

        switch (key.Key) {
        case ConsoleKey.LeftArrow:
            index = Math.Max(0, index - 1);
            Draw(true);
            break;

        case ConsoleKey.RightArrow:
            index = Math.Min(items.Length - 1, index + 1);
            Draw(true);
            break;

        case ConsoleKey.Enter:
        case ConsoleKey.Spacebar:
            items[index].action();
            break;
        }
    }
}