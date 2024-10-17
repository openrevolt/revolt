namespace Revolt;

public abstract class UiFrame {
    protected List<UiElement> elements = [];
    protected UiElement defaultElement;
    public UiElement focusedElement;

    public virtual void Draw(int width, int height) {
        int top = 0;

        Ansi.SetBgColor(Data.BG_COLOR);
        for (int y = top; y <= height; y++) {
            if (y > Console.WindowHeight) break;

            Ansi.SetCursorPosition(0, y);
            for (int x = 0; x < width; x++) {
                Console.Write(" ");
            }
        }

        if (elements is null) {
            return;
        }

        for (int i = 0; i < elements?.Count; i++) {
            elements[i].Draw();
        }
    }

    public void FocusPrevious() {
        if (elements is null || elements.Count == 0) {
            return;
        }

        if (focusedElement is null) {
            focusedElement = defaultElement;
            focusedElement.Focus();
            return;
        }

        int index = elements.IndexOf(focusedElement);
        if (index < 0) return;

        focusedElement.Blur();

        index = Math.Max(index - 1, 0);
        focusedElement = elements[index];
        focusedElement.Focus();
    }

    public void FocusNext() {
        if (elements is null || elements.Count == 0) {
            return;
        }

        if (focusedElement is null) {
            focusedElement = defaultElement;
            focusedElement.Focus();
            return;
        }

        int index = elements.IndexOf(focusedElement);
        if (index < 0) return;

        focusedElement.Blur();

        index = (index + 1) % elements.Count;
        focusedElement = elements[index];
        focusedElement.Focus();
    }

    public abstract bool HandleKey(ConsoleKeyInfo key);
}