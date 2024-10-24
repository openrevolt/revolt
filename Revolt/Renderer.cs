using Revolt.Frames;
using Revolt.Ui;

namespace Revolt;

public static class Renderer {
    const int MAX_WIDTH = 200, MAX_HEIGHT = 50;
    public static int LastWidth { get; set; }
    public static int LastHeight { get; set; }
    public static Frame ActiveFrame { get; set; }
    public static InputDialog ActiveDialog { get; set; }

    static Renderer() {
        LastWidth = 80;
        LastHeight = 20;
        MainMenu.singleton.Show(false);
    }

    public static void Initialize() {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

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

            if (ActiveDialog is not null) {
                ActiveDialog.HandleKey(key);
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

            int newWidth = Math.Min(Console.WindowWidth, MAX_WIDTH);
            int newHeight = Math.Min(Console.WindowHeight, MAX_HEIGHT);

            if (LastWidth == newWidth && LastHeight == newHeight) continue;

            if (newWidth <= 0 || newHeight <= 0) continue;

            LastWidth = Math.Min(Console.WindowWidth, MAX_WIDTH);
            LastHeight = Math.Min(Console.WindowHeight, MAX_HEIGHT);

            Redraw();
        }
    }

    public static void Redraw(bool clean = false) {
        if (clean) {
            Ansi.ClearScreen();
        }

        ActiveFrame?.Draw(LastWidth, LastHeight);
        ActiveDialog?.Draw();
    }
}
