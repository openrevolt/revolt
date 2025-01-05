namespace Revolt;

public static class Renderer {
    const int MAX_WIDTH = 240, MAX_HEIGHT = 60;
    public static int LastWidth { get; set; }
    public static int LastHeight { get; set; }
    public static Tui.Frame ActiveFrame { get; set; }
    public static Tui.DialogBox ActiveDialog { get; set; }

    private static bool isRunning;

    static Renderer() {
        LastWidth = 80;
        LastHeight = 20;
        Frames.MainMenu.Instance.Show(false);
    }

    public static void Initialize() {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        isRunning = true;

        new Thread(ResizeLoop) {
            IsBackground = true
        }.Start();

        HandleKeys();
    }

    public static void HandleKeys() {
        while (isRunning) {
            ConsoleKeyInfo key = Console.ReadKey(true);

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
                QuitDialog();
                continue;
            }
        }

        CleanUp();
    }

    public static void ResizeLoop() {
        while (isRunning) {
            Thread.Sleep(200);

            int newWidth = Math.Min(Console.WindowWidth, MAX_WIDTH);
            int newHeight = Math.Min(Console.WindowHeight, MAX_HEIGHT);

            if (LastWidth == newWidth && LastHeight == newHeight) continue;

            if (newWidth <= 1 || newHeight <= 1) continue;

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

    private static void QuitDialog() {
        Tui.ConfirmDialog dialog = new Tui.ConfirmDialog() {
            text = "Are you sure you want to quit?"
        };

        dialog.okButton.action = () => {
            isRunning = false;
        };

        dialog.Show();
    }

    private static void CleanUp() {
        foreach (CancellationTokenSource token in Tokens.dictionary.Keys) {
            token.Cancel();
        }
        
        Ansi.ResetAll();
        Ansi.Push();

        Console.WriteLine();
    }
}
