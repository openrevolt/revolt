namespace Revolt.Tui;

public sealed class Toolbar(Frame parentFrame) : Element(parentFrame) {

    public struct ToolbarItem {
        public string     text;
        public string     key;
        public Ansi.Color color;
        public bool       disabled;
        public Action     action;
    }

    public ToolbarItem[] items;
    public int index = 0;

    public Action drawStatus;

    public override void Draw(bool push) {
        (int left, _, int width, int height) = GetBounding();
        int offset = 0;

        for (int i = 0; i < items.Length; i++) {
            if (offset + items[i].key.Length + items[i].text.Length + 4 > width) break;

            offset += DrawItem(i, left + offset, height, isFocused && i == index);
            if (left + offset >= width) break;
            Ansi.Push(); //fixes tearing
        }

        Ansi.SetCursorPosition(offset + 1, height);
        Ansi.SetBgColor(Glyphs.TOOLBAR_COLOR);

        int gap = width - offset;
        if (gap > 0) {
            Ansi.Write(new String(' ', gap));
        }

        if (drawStatus is not null) {
            drawStatus();
        }

        if (push) {
            Ansi.Push();
        }
    }

    private int DrawItem(int i, int offset, int height, bool isFocused) {
        Ansi.SetCursorPosition(offset, height);

        Ansi.SetBgColor(Glyphs.TOOLBAR_COLOR);
        Ansi.Write(' ');

        if (items[i].disabled) {
            Ansi.SetFgColor(Glyphs.INPUT_COLOR);
        }
        if (items[i].color == default) {
            Ansi.SetFgColor(this.isFocused && i == index ? Glyphs.DARKGRAY_COLOR : Glyphs.LIGHT_COLOR);
        }
        else {
            Ansi.SetFgColor(items[i].color);
        }

        Ansi.SetBgColor(this.isFocused && i == index ? Glyphs.FOCUS_COLOR : Glyphs.TOOLBAR_COLOR);

        Ansi.Write(' ');
        Ansi.SetUnderline(true);
        Ansi.Write(items[i].key);
        Ansi.SetUnderline(false);

        Ansi.Write($":{items[i].text} ");

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
            if (items[index].disabled) break;
            items[index].action();
            break;
        }
    }
}