namespace Revolt.Ui;

public sealed class ListBox<T>(Frame parentFrame) : Element(parentFrame) {
    public List<T> items = [];
    public int index = -1;

    public delegate void DrawItemDelegate(int i, int x, int y, int width);
    public DrawItemDelegate drawItemHandler;

    public override void Draw(bool push) {
        (int left, int top, int width, int height) = GetBounding();

        for (int i = 0; i < height; i++) {
            drawItemHandler(i, left, top, width);
        }

        if (push) {
            Ansi.Push();
        }
    }

    public override void HandleKey(ConsoleKeyInfo key) {
        if (items is null || items.Count == 0) return;

        switch (key.Key) {
        case ConsoleKey.UpArrow:
            index = Math.Max(0, index - 1);
            Draw(true);
            break;

        case ConsoleKey.DownArrow:
            index = Math.Min(items.Count - 1, index + 1);
            Draw(true);
            break;

        case ConsoleKey.LeftArrow:
            break;

        case ConsoleKey.RightArrow:
            break;

        case ConsoleKey.Enter:
            break;
        }
    }

    public void Add(T item) =>
        items.Add(item);

    public void Remove(T item) =>
        items.Remove(item);

    public void Clear() {
        items.Clear();
        Renderer.Redraw(true);
    }

    public override void Focus(bool draw = true) {
        base.Focus(draw);

        if (index == -1 && items.Count > 0) {
            index = 0;
        }

        if (index > -1) {
            (int left, int top, int width, _) = GetBounding();
            drawItemHandler(index, left, top, width);
        }
    }

    public override void Blur(bool draw = true) {
        base.Blur(draw);

        if (index > -1) {
            (int left, int top, int width, _) = GetBounding();
            drawItemHandler(index, left, top, width);
        }
    }
}