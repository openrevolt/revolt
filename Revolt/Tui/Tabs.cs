using System;

namespace Revolt.Tui;

public sealed class Tabs(Frame parentFrame) : Element(parentFrame) {

    public struct TabItem {
        public string text;
        public string key;
        public string label;
    }

    public TabItem[] items;
    public int index = 0;

    public override void Draw(bool push) {
        (int left, int top, int width, int _) = GetBounding();
        int offset = 0;

        for (int i = 0; i < items.Length; i++) {
            if (offset + items[i].key.Length + items[i].text.Length + 4 > width) break;

            offset += DrawItem(i, left + offset, top, isFocused && i == index);
            if (left + offset >= width) break;
            Ansi.Push(); //fixes tearing
        }

        Ansi.SetCursorPosition(left, top + 2);
        Ansi.SetBgColor(Glyphs.PANE_COLOR);
        Ansi.Write(new String(' ', width));

        if (push) {
            Ansi.Push();
        }
    }

    private int DrawItem(int i, int offset, int top, bool isFocused) {
        int keyIndex = items[i].text.IndexOf(items[i].key, StringComparison.OrdinalIgnoreCase);

        byte[] backgroundColor = i == index ? Glyphs.PANE_COLOR : [24, 24, 24];

        int length = items[i].text.Length + 2 + (String.IsNullOrEmpty(items[i].label) ? 0 : items[i].label.Length + 1);

        Ansi.SetCursorPosition(offset, top);

        Ansi.SetBgColor(Glyphs.DARK_COLOR);
        Ansi.Write(' ');

        Ansi.SetFgColor(this.isFocused && i == index ? Glyphs.FOCUS_COLOR : backgroundColor);
        Ansi.Write(new String(Glyphs.LOWER_1_8TH_BLOCK, length));


        Ansi.SetCursorPosition(offset, top + 1);

        Ansi.SetBgColor(Glyphs.DARK_COLOR);
        Ansi.Write(' ');

        Ansi.SetFgColor(i == index ? [255, 255, 255] : Glyphs.LIGHT_COLOR);
        Ansi.SetBgColor(backgroundColor);

        if (keyIndex == -1) {
            Ansi.Write($" {items[i].text} ");
        }
        else {
            Ansi.Write($" {items[i].text[..keyIndex]}");
            Ansi.SetUnderline(true);
            Ansi.Write(items[i].text[keyIndex]);
            Ansi.SetUnderline(false);
            Ansi.Write($"{items[i].text[(keyIndex+1)..]} ");
        }

        if (!String.IsNullOrEmpty(items[i].label)) {
            Ansi.SetFgColor(Glyphs.RED_COLOR);
            Ansi.Write($"{items[i].label} ");
        }

        return length + 1;
    }

    public override void HandleKey(ConsoleKeyInfo key) {
        if (items is null || items.Length == 0) return;

        int lastIndex = index;

        switch (key.Key) {
        case ConsoleKey.LeftArrow:
            index = Math.Max(0, index - 1);
            if (lastIndex == index) break;
            Draw(true);
            break;

        case ConsoleKey.RightArrow:
            index = Math.Min(items.Length - 1, index + 1);
            if (lastIndex == index) break;
            Draw(true);
            break;
        }
    }
}
