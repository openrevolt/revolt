namespace Revolt;

public sealed class UiPopup : UiFrame {
    public UiButton okButton;
    public UiButton cancelButton;

    public UiPopup() {
        okButton = new UiButton(this, "   OK   ");
        cancelButton = new UiButton(this, " Cancel ");

        elements.Add(okButton);
        elements.Add(cancelButton);

        defaultElement = okButton;
    }

    public override void Draw(int width, int height) {
        int left = (Renderer.LastWidth - width) / 2 + 1;
        int top = 0;

        Ansi.SetBgColor(Data.PANE_COLOR);
        for (int y = top; y <= height; y++) {
            if (y > Console.WindowHeight) break;

            Ansi.SetCursorPosition(left, y);
            for (int x = 0; x < width; x++) {
                Console.Write(" ");
            }
        }

        if (elements is null) return;

        for (int i = 0; i < elements?.Count; i++) {
            elements[i].Draw();
        }
    }

    public void Draw() {
        int width = Math.Min(Renderer.LastWidth, 48);
        int left = (Renderer.LastWidth - width) / 2 + 1;

        okButton.left = left + (width - 20) / 2;
        cancelButton.left = left + (width - 20) / 2 + 10;

        okButton.top = 6;
        cancelButton.top = 6;

        Draw(width, 8);
    }

    public override bool HandleKey(ConsoleKeyInfo key) {
        switch (key.Key) {

        case ConsoleKey.LeftArrow:
            //FocusPrevious();
            break;

        case ConsoleKey.RightArrow:
            //FocusNext();
            break;

        case ConsoleKey.Tab:
            if (key.Modifiers == ConsoleModifiers.Shift) {
                FocusPrevious();
            }
            else {
                FocusNext();
            }
            break;

        case ConsoleKey.Enter:
            break;

        case ConsoleKey.Escape:
            Close();
            break;
        }

        return true;
    }
    public void Close() {
        Renderer.Popup = null;
        Renderer.Redraw();
    }

}