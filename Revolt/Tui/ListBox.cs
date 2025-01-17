namespace Revolt.Tui;

public sealed class ListBox<T>(Frame parentFrame) : Element(parentFrame) {
    public SynchronizedList<T> items = [];
    public int itemHeight   = 1;
    public int index        = -1;
    public int scrollOffset = 0;
    public byte[] backgroundColor = Glyphs.DARK_COLOR;

    public delegate void DrawItemDelegate(int i, int x, int y, int width);
    public DrawItemDelegate drawItemHandler;

    public override void Draw(bool push) {
        (int left, int top, int width, int height) = GetBounding();
        int visibleItems = height / itemHeight;

        CalculateScrollOffset(height);

        for (int i = scrollOffset; i < Math.Min(scrollOffset + visibleItems, items.Count); i++) {
            drawItemHandler(i, left, top, width);
        }

        if (items.Count * itemHeight < height - 1) {
            string blank = new String(' ', width);
            Ansi.SetBgColor(this.backgroundColor);
            for (int i = items.Count * itemHeight; i < height - 1; i++) {
                Ansi.SetCursorPosition(left, top + i);
                Ansi.Write(blank);
            }
        }

        if (push) {
            Ansi.Push();
        }
    }

    private void CalculateScrollOffset(int height) {
        int visibleItems = height / itemHeight;

        if (items.Count <= visibleItems) {
            scrollOffset = 0;
        }
        else if (index < scrollOffset) {
            scrollOffset = index;
        }
        else if (index >= scrollOffset + visibleItems) {
            scrollOffset = index - visibleItems + 1;
        }

        int maxScrollOffset = Math.Max(0, items.Count - visibleItems);
        scrollOffset = Math.Min(scrollOffset, maxScrollOffset);
    }

    public override void HandleKey(ConsoleKeyInfo key) {
        if (items is null || items.Count == 0) return;

        (int left, int top, int width, int height) = GetBounding();
        int lastIndex = index;
        int lastScrollOffset = scrollOffset;

        switch (key.Key) {
        case ConsoleKey.UpArrow:
            index = Math.Max(0, index - 1);
            break;

        case ConsoleKey.DownArrow:
            index = Math.Min(items.Count - 1, index + 1);
            break;

        case ConsoleKey.PageUp:
            index = Math.Clamp(index - (height / itemHeight) + 1, 0, items.Count - 1);
            Draw(true);
            return;

        case ConsoleKey.PageDown:
            index = Math.Clamp(index + (height / itemHeight) - 1, 0, items.Count - 1);
            Draw(true);
            return;

        case ConsoleKey.Home:
            if (items.Count > 0) {
                index = 0;
            }
            if (index != lastIndex) {
                Draw(true);
            }
            return;

        case ConsoleKey.End:
            if (items.Count > 0) {
                index = items.Count - 1;
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
                Ansi.ScrollDown(itemHeight);
            }
            else if (delta > 0) {
                Ansi.ScrollUp(itemHeight);
            }

            drawItemHandler(lastIndex, left, top, width);
            drawItemHandler(index, left, top, width);
            Ansi.Push();
        }
    }

    public void Add(T item) {
        items.Add(item);
        //index = items.Count - 1;
    }

    public void Remove(T item) {
        int removedIndex = items.IndexOf(item);
        if (removedIndex < 0) return;
        
        items.RemoveAt(removedIndex);
        index = Math.Clamp(index, 0, items.Count - 1);
    }

    public T RemoveSelected() {
        if (items.Count == 0)     return default;
        if (index < 0)            return default;
        if (index >= items.Count) return default;
       
        T item = items[index];
        items.RemoveAt(index);

        index = items.Count == 0 ? -1 : Math.Clamp(index, 0, items.Count - 1);

        Draw(true);

        return item;
    }

    public void Clear() {
        items.Clear();
        index = -1;
        Draw(true);
    }

    public override void Focus(bool draw = true) {
        base.Focus(draw);

        if (index == -1 && items.Count > 0) {
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