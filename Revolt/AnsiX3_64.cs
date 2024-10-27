using System.Text;

namespace Revolt;

public static class Ansi {
    private static readonly StringBuilder builder = new StringBuilder();

    public static void Push() {
        Console.Write(builder.ToString());
        builder.Clear();
    }

    public static void Write(char text) =>
        builder.Append(text);

    public static void Write(string text) =>
        builder.Append(text);

    public static void WriteLine(string text) =>
        builder.Append(text).Append('\n');

    public static void ClearScreen() =>
        builder.Append("\x1b[2J");

    public static void ResetAll() =>
        builder.Append("\x1b[0m");

    public static void SetFgColor(byte r, byte g, byte b) =>
        builder.Append($"\x1b[38;2;{r};{g};{b}m");

    public static void SetFgColor(byte[] rgb) =>
        builder.Append($"\x1b[38;2;{rgb[0]};{rgb[1]};{rgb[2]}m");

    public static void SetBgColor(byte r, byte g, byte b) =>
        builder.Append($"\x1b[48;2;{r};{g};{b}m");

    public static void SetBgColor(byte[] rgb) =>
        builder.Append($"\x1b[48;2;{rgb[0]};{rgb[1]};{rgb[2]}m");

    public static void SetBlinkOn() =>
        builder.Append("\x1b[6m");

    public static void SetBlinkOff() =>
        builder.Append("\x1b[25m");

    public static void HideCursor() =>
         builder.Append("\x1b[?25l");

    public static void ShowCursor() =>
        builder.Append  ("\x1b[?25h");

    public static void SetCursorPosition(int x, int y) {
        if (x < 2) {
            Console.Title = x + " x " + y;
            builder.Append($"\x1b[{y};{x}H");
        }
        else {
            builder.Append($"\x1b[{y};{x}H");
        }
    }

}
