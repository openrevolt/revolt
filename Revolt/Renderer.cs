namespace Revolt;

public static class Renderer {
    public static int lastWidth = 80, lastHeight = 20;
    private static UiFrame activeFrame = null;

    private static UiFrame mainMenuFrame;

    static Renderer() {
        mainMenuFrame = new UiMainMenu();
        activeFrame = mainMenuFrame;
    }

    public static void Start() {
        Ansi.HideCursor();

        new Thread(ResizeLoop) {
            IsBackground = true
        }.Start();

        while (true) {
            ConsoleKeyInfo key = Console.ReadKey();

            if (activeFrame is null) continue;

            if (key.Key == ConsoleKey.F5) {
                Redraw(true);
            }
            else if (!activeFrame.HandleKey(key.Key))  {
                return;
            }
        }
    }

    public static void ResizeLoop() {
        while (true) {
            Thread.Sleep(200);

            int newWidth = Math.Min(Console.WindowWidth, 200);
            int newHeight = Math.Min(Console.WindowHeight, 50);

            if (lastWidth == newWidth && lastHeight == newHeight) continue;

            if (newWidth <= 0 || newHeight <= 0) continue;

            lastWidth = Math.Min(Console.WindowWidth, 200);
            lastHeight = Math.Min(Console.WindowHeight, 50);

            Redraw();
        }
    }

    public static void Redraw(bool clean = false) {
        if (clean) {
            Ansi.ClearScreen();
        }

        activeFrame?.Draw(lastWidth, lastHeight);
    }
}
