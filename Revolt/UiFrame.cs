namespace Revolt;

public abstract class UiFrame {
    public static readonly byte[] FG_COLOR      = [192, 192, 192];
    public static readonly byte[] BG_COLOR      = [32, 32, 32];
    public static readonly byte[] TOOLBAR_COLOR = [64, 64, 64];
    public static readonly byte[] CONTROL_COLOR = [72, 72, 72];
    public static readonly byte[] INPUT_COLOR   = [128, 128, 128];
    public static readonly byte[] SELECT_COLOR  = [255, 192, 0];

    protected List<UiElement> elements;
    protected UiElement defaultElement;
    protected UiElement focusedElement;

    public UiFrame() {
        elements = new List<UiElement>();
    }

    public virtual void Draw(int width, int height) {
        int top = 0;

        Ansi.SetBgColor(BG_COLOR);
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