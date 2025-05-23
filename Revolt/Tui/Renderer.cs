﻿namespace Revolt;

public static class Renderer {
    const int MAX_WIDTH = 320, MAX_HEIGHT = 80;
    public static int LastWidth { get; set; }
    public static int LastHeight { get; set; }
    public static Tui.Frame ActiveFrame { get; set; }
    public static Tui.DialogBox ActiveDialog { get; set; }

    private static bool isRunning;

    static Renderer() {
        LastWidth = 80;
        LastHeight = 20;
        Frames.MainMenu.instance.Show(false);
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
            if (ActiveFrame is null) continue;

            ConsoleKeyInfo key;
            try {
                key = Console.ReadKey(true);
            }
            catch (InvalidOperationException) {
                continue;
            }

            switch (key.Key) {
            case ConsoleKey.F1:
                if (ActiveDialog is not null) break;
                Tui.HelpDialog popup = new Tui.HelpDialog(ActiveFrame.GetKeyShortcuts());
                popup.Show();
                continue;

            case ConsoleKey.F5:
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
            Thread.Sleep(400);

            int width = Console.WindowWidth;
            int height = Console.WindowHeight;

            int newWidth = Math.Min(width, MAX_WIDTH);
            int newHeight = Math.Min(height, MAX_HEIGHT);

            if (LastWidth == newWidth && LastHeight == newHeight) continue;

            if (newWidth <= 1 || newHeight <= 1) continue;

            LastWidth = Math.Min(width, MAX_WIDTH);
            LastHeight = Math.Min(height, MAX_HEIGHT);

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
