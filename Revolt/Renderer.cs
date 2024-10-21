using Revolt.Frames;
using Revolt.Ui;

namespace Revolt;

public static class Renderer {
    public static int LastWidth { get; set; }
    public static int LastHeight { get; set; }
    public static Frame ActiveFrame { get; set; }
    public static InputDialog Dialog { get; set; }

    static Renderer() {
        ActiveFrame = MainMenu.singleton;
        LastWidth = 80;
        LastHeight = 20;
    }

    public static void Start() {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Ansi.HideCursor();

        new Thread(ResizeLoop) {
            IsBackground = true
        }.Start();

        HandleKeys();
    }

    public static void HandleKeys() {
        while (true) {
            ConsoleKeyInfo key = Console.ReadKey();

            if (ActiveFrame is null) continue;

            if (key.Key == ConsoleKey.F5) {
                Ansi.ResetAll();
                Redraw(true);
                continue;
            }

            if (Dialog is not null) {
                Dialog.HandleKey(key);
                continue;
            }

            if (!ActiveFrame.HandleKey(key)) {
                Ansi.ResetAll();
                Console.WriteLine();
                return;
            }
        }
    }

    public static void ResizeLoop() {
        while (true) {
            Thread.Sleep(200);

            int newWidth = Math.Min(Console.WindowWidth, 200);
            int newHeight = Math.Min(Console.WindowHeight, 50);

            if (LastWidth == newWidth && LastHeight == newHeight) continue;

            if (newWidth <= 0 || newHeight <= 0) continue;

            LastWidth = Math.Min(Console.WindowWidth, 200);
            LastHeight = Math.Min(Console.WindowHeight, 50);

            Redraw();
        }
    }

    public static void Redraw(bool clean = false) {
        if (clean) {
            Ansi.ClearScreen();
        }

        ActiveFrame?.Draw(LastWidth, LastHeight);
        Dialog?.Draw();
    }
}
