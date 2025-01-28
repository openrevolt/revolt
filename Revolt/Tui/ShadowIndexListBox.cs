using System.Collections.Concurrent;

namespace Revolt.Tui;

public sealed class ShadowIndexListBox<TKey, TValue>(Frame parentFrame) : Element(parentFrame)
    where TValue : class {

    public int index              = -1;
    public int scrollOffset       = 0;
    public byte[] backgroundColor = Glyphs.DARK_COLOR;

    public delegate void DrawItemDelegate(int i, int x, int y, int width);
    public DrawItemDelegate drawItemHandler;

    public IndexedDictionary<TKey, TValue> shadow;

    public void BindDictionary(IndexedDictionary<TKey, TValue> shadow) {
        this.shadow = shadow;
        this.Draw(true);
    }

    public int Count => shadow?.Count ?? 0;

    public TValue this[int index] {
        get {
            return shadow?[index] ?? null;
        }
    }

    public override void Draw(bool push) {
        (int left, int top, int width, int height) = GetBounding();
        int visibleItems = height;

        CalculateScrollOffset(height);

        for (int i = scrollOffset; i < Math.Min(scrollOffset + visibleItems, shadow?.Count ?? 0); i++) {
            drawItemHandler(i, left, top, width);
        }

        if ((shadow?.Count ?? 0) < height - 1) {
            string blank = new String(' ', width);
            Ansi.SetBgColor(this.backgroundColor);
            for (int i = shadow?.Count ?? 0; i < height; i++) {
                Ansi.SetCursorPosition(left, top + i);
                Ansi.Write(blank);
            }
        }

        if (push) {
            Ansi.Push();
        }
    }

    private void CalculateScrollOffset(int height) {
        int visibleItems = height;
        int calculatedIndex = Math.Max(index, 0);

        if (shadow?.Count <= visibleItems) {
            scrollOffset = 0;
        }
        else if (calculatedIndex < scrollOffset) {
            scrollOffset = calculatedIndex;
        }
        else if (calculatedIndex >= scrollOffset + visibleItems) {
            scrollOffset = calculatedIndex - visibleItems + 1;
        }

        int maxScrollOffset = Math.Max(0, (shadow?.Count ?? 0) - visibleItems);
        scrollOffset = Math.Min(scrollOffset, maxScrollOffset);
    }

    public override void HandleKey(ConsoleKeyInfo key) {
        if (shadow is null || shadow.Count == 0) return;

        (int left, int top, int width, int height) = GetBounding();
        int lastIndex = index;
        int lastScrollOffset = scrollOffset;

        switch (key.Key) {
        case ConsoleKey.UpArrow:
            index = Math.Max(0, index - 1);
            break;

        case ConsoleKey.DownArrow:
            index = Math.Min(shadow.Count - 1, index + 1);
            break;

        case ConsoleKey.PageUp:
            index = Math.Clamp(index - height + 1, 0, shadow.Count - 1);
            Draw(true);
            return;

        case ConsoleKey.PageDown:
            index = Math.Clamp(index + height - 1, 0, shadow.Count - 1);
            Draw(true);
            return;

        case ConsoleKey.Home:
            if (shadow.Count > 0) {
                index = 0;
            }
            if (index != lastIndex) {
                Draw(true);
            }
            return;

        case ConsoleKey.End:
            if (shadow.Count > 0) {
                index = shadow.Count - 1;
            }
            if (index != lastIndex) {
                Draw(true);
            }
            return;
        
        default:
            return;
        }

        if (index == lastIndex) {
            return;
        }

        CalculateScrollOffset(height);

        if (scrollOffset == lastScrollOffset) {
            drawItemHandler(lastIndex, left, top, width);
            drawItemHandler(index, left, top, width);
            Ansi.Push();
        }
        else if (scrollOffset != lastScrollOffset) {
            int delta = scrollOffset - lastScrollOffset;

            Ansi.SetScrollRegion(top, top + height - 1);

            if (delta < 0) {
                Ansi.ScrollDown(1);
            }
            else if (delta > 0) {
                Ansi.ScrollUp(1);
            }

            drawItemHandler(lastIndex, left, top, width);
            drawItemHandler(index, left, top, width);
            Ansi.Push();
        }
    }

    public void Clear() {
        shadow?.Clear();
        index = -1;
        Draw(true);
    }

    public override void Focus(bool draw = true) {
        base.Focus(draw);

        if (index == -1 && shadow?.Count > 0) {
            index = 0;
        }

        if (index > -1 && draw) {
            (int left, int top, int width, _) = GetBounding();
            drawItemHandler(index, left, top, width);
        }
    }

    public override void Blur(bool draw = true) {
        base.Blur(draw);

        if (index > -1 && draw) {
            (int left, int top, int width, _) = GetBounding();
            drawItemHandler(index, left, top, width);
        }
    }
}