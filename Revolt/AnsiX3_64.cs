namespace Revolt;

public static class Ansi {

    public static void ClearScreen() {
        Console.Write("\x1b[2J");
    }

    public static void SetFgColor(byte r, byte g, byte b) {
        Console.Write($"\x1b[38;2;{r};{g};{b}m");
    }
    public static void SetFgColor(byte[] rgb) {
        Console.Write($"\x1b[38;2;{rgb[0]};{rgb[1]};{rgb[2]}m");
    }

    public static void SetBgColor(byte r, byte g, byte b) {
        Console.Write($"\x1b[48;2;{r};{g};{b}m");
    }
    public static void SetBgColor(byte[] rgb) {
        Console.Write($"\x1b[48;2;{rgb[0]};{rgb[1]};{rgb[2]}m");
    }

    public static void ResetAll() {
        Console.Write($"\x1b[0m");
    }
    public static void HideCursor() {
        Console.Write($"\x1b[?25l");
    }

    public static void ShowCursor() {
        Console.Write($"\x1b[?25h");
    }

    public static void SetCursorPosition(int x, int y) {
        Console.Write($"\x1b[{y};{x}H");
    }

}
