namespace Revolt;

public sealed class UiList<T>(UiFrame parentFrame) : UiElement(parentFrame) {
    public List<T> items = [];
    public int index = -1;

    public delegate void DrawItemDelegate(int i, int x, int y, int width);
    public DrawItemDelegate drawItemHandler;

    public override void Draw() {
        (int left, int top, int width, int height) = GetBounding();

        int x = left;
        int y = top;

        Ansi.SetBgColor(isFocused ? Data.SELECT_COLOR : Data.CONTROL_COLOR);
        Ansi.SetCursorPosition(x, y);
        Console.Write('.');

        Ansi.SetCursorPosition(x+width, y);
        Console.Write('.');

        Ansi.SetCursorPosition(x, y + height);
        Console.Write('.');

        Ansi.SetCursorPosition(x+width, y + height);
        Console.Write('.');

        for (int i = 0; i < height; i++) {
            drawItemHandler(i, x, y, width);
        }
    }

    public override void HandleKey(ConsoleKeyInfo key) {
        if (items is null || items.Count == 0) return;

        switch (key.Key) {
        case ConsoleKey.UpArrow:
            index = Math.Max(0, index - 1);
            //Draw();
            break;

        case ConsoleKey.DownArrow:
            index = Math.Min(items.Count - 1, index + 1);
            //Draw();
            break;

        case ConsoleKey.LeftArrow:
            break;

        case ConsoleKey.RightWindows:
            break;

        case ConsoleKey.Enter:
            break;
        }
    }
}