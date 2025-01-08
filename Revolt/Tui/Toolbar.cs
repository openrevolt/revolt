namespace Revolt.Tui;

public sealed class Toolbar(Frame parentFrame) : Element(parentFrame) {

    public struct ToolbarItem {
        public string text;
        public string key;
        public Action action;
    }

    public ToolbarItem[] items;
    public int index = 0;

    public Action drawStatus;

    public override void Draw(bool push) {
        (int left, _, int width, int height) = GetBounding();
        int offset = 0;

        for (int i = 0; i < items.Length; i++) {
            if (offset + items[i].key.Length + items[i].text.Length + 4 > width) {
                break;
            }

            offset += DrawItem(i, left + offset, height, isFocused && i == index);
            if (left + offset >= width) break;
            Ansi.Push(); //fixes tearing
        }

        Ansi.SetCursorPosition(offset + 1, height);
        Ansi.SetBgColor(Data.TOOLBAR_COLOR);

        int gap = width - offset;
        if (gap > 0) {
            Ansi.Write(new String(' ', gap));
        }

        if (drawStatus is not null) {
            drawStatus();
        }

        Ansi.Push();
    }

    private int DrawItem(int i, int offset, int height, bool isFocused) {
        Ansi.SetCursorPosition(offset, height);

        Ansi.SetBgColor(Data.TOOLBAR_COLOR);
        Ansi.Write(' ');

        Ansi.SetFgColor(this.isFocused && i == index ? [16, 16, 16] : Data.LIGHT_COLOR);
        Ansi.SetBgColor(this.isFocused && i == index ? Data.SELECT_COLOR : Data.TOOLBAR_COLOR);

        Ansi.SetBold(true);
        Ansi.Write($" {items[i].key}:");
        Ansi.SetBold(false);

        Ansi.Write($"{items[i].text} ");

        return items[i].key.Length + items[i].text.Length + 4;
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