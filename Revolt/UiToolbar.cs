namespace Revolt;

public sealed class UiToolbar : UiElement {
    public struct ToolbarItem {
        public string text;
        public Action action;
    }

    private bool focused = false;

    public ToolbarItem[] items;
    public int index = 0;

    public override void Draw() {
        int left   = this.left < 1 ? (int)(Renderer.lastWidth * this.left) : (int)this.left;
        //int top    = this.top < 1 ? (int)(Renderer.lastHeight * this.top) : (int)this.top;
        int right  = this.right < 1 ? (int)(Renderer.lastWidth * this.right) : (int)this.right;
        //int bottom = this.bottom < 1 ? (int)(Renderer.lastHeight * this.bottom) : (int)this.bottom;

        int width  = Renderer.lastWidth - left - right;
        //int height = Renderer.lastHeight - top - bottom;

        int x = left + 1;
        Ansi.SetBgColor(UiFrame.BG_COLOR);
        for (int i = 0; i < items.Length; i++) {
            int length = items[i].text.Length + 3;
            if (x + length > width) {
                break;
            }

            Ansi.SetCursorPosition(x, 1);

            Ansi.SetFgColor(focused && i == index ? UiFrame.SELECT_COLOR : UiFrame.CONTROL_COLOR);
            Console.Write(new String('\u2583', items[i].text.Length + 2));
            Ansi.SetFgColor(UiFrame.TOOLBAR_COLOR);
            Console.Write('\u2583');

            x += length;
        }

        Console.Write(new string('\u2583', Renderer.lastWidth - x + 1 - right));

        x = left + 1;
        for (int i = 0; i < items.Length; i++) {
            int length = items[i].text.Length + 3;
            if (x + length > width) {
                break;
            }

            Ansi.SetCursorPosition(x, 2);
            Ansi.SetFgColor(focused && i == index ? [16, 16, 16] : UiFrame.FG_COLOR);

            Ansi.SetBgColor(focused && i == index ? UiFrame.SELECT_COLOR : UiFrame.CONTROL_COLOR);
            Console.Write($" {items[i].text} ");
            Ansi.SetBgColor(UiFrame.TOOLBAR_COLOR);
            Console.Write(' ');

            x += length;
        }

        Console.Write(new string(' ', Renderer.lastWidth - x + 1 - right));
    }

    public override void HandleKey(ConsoleKeyInfo key) {
        if (items is null || items.Length == 0) {
            return;
        }

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

    public override void Focus() {
        focused = true;
        Draw();
    }

    public override void Blur() {
        focused = false;
        Draw();
    }
}