﻿namespace Revolt;

public static class Renderer {
    private static int width = 80;
    private static int height = 20;

    private static UiFrame? activeFrame = null;

    public static void Start() {
        Ansi.HideCursor();

        new Thread(ResizeLoop) {
            IsBackground = true
        }.Start();
    }

    public static void ResizeLoop() {
        while (true) {
            Thread.Sleep(200);

            int newWidth = Math.Min(Console.WindowWidth, 200);
            int newHeight = Math.Min(Console.WindowHeight, 50);

            if (width == newWidth && height == newHeight) {
                continue;
            }

            if (newWidth <= 0 || newHeight <= 0) {
                continue;
            }

            Redraw();
        }
    }

    public static void Redraw(bool clean = false) {
        if (clean) {
            Ansi.ClearScreen();
        }

        width = Math.Min(Console.WindowWidth, 200);
        height = Math.Min(Console.WindowHeight, 50);

        for (int i = 0; i < activeFrame?.elements.Length; i++) {
            activeFrame.elements[i].Draw();
        }

        for (int y = 0; y <= height; y++) {
            if (y > Console.WindowHeight) {
                break;
            }

            Ansi.SetCursorPosition(0, y);

            for (int x = 0; x < width; x++) {
                Console.Write(x % 10);
            }
        }

        Ansi.SetCursorPosition(0, 0);
        Ansi.SetFgColor(255, 0, 0);
        Ansi.SetBgColor(127, 127, 127);
        Console.Write("-" + DateTime.Now.Second + "-");
        Console.Write($"\x1b[0m");
    }
}
