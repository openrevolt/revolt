namespace Revolt.Ui;

public sealed class Toolbar(Frame parentFrame) : Element(parentFrame) {

    public struct ToolbarItem {
        public string text;
        public Action action;
    }

    public ToolbarItem[] items;
    public int index = 0;

    public override void Draw() {
        (int left, _, int width, _) = GetBounding();

        int count = 0;
        Ansi.SetCursorPosition(left, 1);
        Ansi.SetBgColor(Data.BG_COLOR);
        for (int i = 0; i < items.Length; i++) {
            int length = items[i].text.Length + 3;
            if (count + length > width) break;

            Ansi.SetFgColor(Data.TOOLBAR_COLOR);
            Console.Write(Data.LOWER_3_8TH_BLOCK);

            Ansi.SetFgColor(isFocused && i == index ? Data.SELECT_COLOR : Data.CONTROL_COLOR);
            Console.Write(new string(Data.LOWER_3_8TH_BLOCK, items[i].text.Length + 2));

            count += length;
        }

        int r = width - count;
        Ansi.SetFgColor(Data.TOOLBAR_COLOR);
        Console.Write(new string(Data.LOWER_3_8TH_BLOCK, r));


        count = 0;
        Ansi.SetCursorPosition(left, 2);
        for (int i = 0; i < items.Length; i++) {
            int length = items[i].text.Length + 3;
            if (count + length > width) break;

            Ansi.SetFgColor(isFocused && i == index ? [16, 16, 16] : Data.FG_COLOR);

            Ansi.SetBgColor(Data.TOOLBAR_COLOR);
            Console.Write(' ');

            Ansi.SetBgColor(isFocused && i == index ? Data.SELECT_COLOR : Data.CONTROL_COLOR);
            Console.Write($" {items[i].text} ");

            count += length;
        }

        Ansi.SetBgColor(Data.TOOLBAR_COLOR);
        Console.Write(new string(' ', r));


        count = 0;
        Ansi.SetCursorPosition(left, 3);
        Ansi.SetBgColor(Data.BG_COLOR);
        for (int i = 0; i < items.Length; i++) {
            int length = items[i].text.Length + 3;
            if (count + length > width) break;

            Ansi.SetFgColor(Data.TOOLBAR_COLOR);
            Console.Write(Data.UPPER_1_8TH_BLOCK);

            Ansi.SetFgColor(isFocused && i == index ? Data.SELECT_COLOR : Data.CONTROL_COLOR);
            Console.Write(new string(Data.UPPER_1_8TH_BLOCK, items[i].text.Length + 2));

            count += length;
        }

        Ansi.SetFgColor(Data.TOOLBAR_COLOR);
        Console.Write(new string(Data.UPPER_1_8TH_BLOCK, r));
    }

    public override void HandleKey(ConsoleKeyInfo key) {
        if (items is null || items.Length == 0) return;

        switch (key.Key) {
        case ConsoleKey.LeftArrow:
            index = Math.Max(0, index - 1);
            Draw();
            break;

        case ConsoleKey.RightArrow:
            index = Math.Min(items.Length - 1, index + 1);
            Draw();
            break;

        case ConsoleKey.Enter:
            items[index].action();
            break;
        }
    }
}