namespace Revolt;

public abstract class UiFrame {
    //private static int width = 80;
    //private static int height = 20;

    public static readonly byte[] FG_COLOR = [192, 192, 192];
    public static readonly byte[] BG_COLOR = [32, 32, 32];
    public static readonly byte[] SELECT_COLOR = [255, 192, 0];

    public int[] rows;
    public int[] cols;
    public UiElement[] elements;
    public UiElement defaultElement;
    public UiElement focusedElement;

    public UiFrame() {
        rows = [];
        cols = [];
        elements = [];
    }

    public virtual void Draw(int width, int height) {
        if (elements is null) {
            return;
        }

        Ansi.SetBgColor(BG_COLOR);

        for (int y = 0; y <= height; y++) {
            if (y > Console.WindowHeight) break;

            Ansi.SetCursorPosition(0, y);
            for (int x = 0; x < width; x++) {
                Console.Write(" ");
            }
        }

        /*for (int i = 0; i < elements.Length; i++) {
            elements[i].Draw();
        }

        Ansi.SetCursorPosition(0, 0);
        Ansi.ResetAll();*/
    }

    public abstract bool HandleKey(ConsoleKey key);
}