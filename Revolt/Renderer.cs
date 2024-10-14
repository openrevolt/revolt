namespace Revolt;

internal static class Renderer {
    private static int width = 80;
    private static int height = 20;

    public static void Start() {
        new Thread(SizeLoop) {
            IsBackground = true
        }.Start();
    }

    public static void SizeLoop() {
        while (true) {
            Thread.Sleep(200);

            if (width == Console.WindowWidth && height == Console.WindowHeight) {
                continue;
            }

            if (Console.WindowWidth <= 0 || Console.WindowHeight <= 0) {
                continue;
            }

            if (OperatingSystem.IsWindows()) {
                int w = Math.Min(Console.WindowWidth, Console.LargestWindowWidth);
                int h = Math.Min(Console.WindowHeight, Console.LargestWindowHeight);
                Console.BufferWidth = w;
                Console.BufferHeight = h;
            }

            Redraw();
        }
    }

    public static void Redraw() {
        width = Console.WindowWidth;
        height = Console.WindowHeight;

        for (int y = 0; y < height; y++) {
            if (y >= Console.WindowHeight) {
                break;
            }

            Console.SetCursorPosition(0, y);

            for (int x = 0; x < width; x++) {
                Console.Write(x % 10);
            }
        }
        
    }
}
