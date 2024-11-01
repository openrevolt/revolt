using System.Collections.Concurrent;
using System.Text;

namespace Revolt;

public static class Ansi {
    private static readonly ConcurrentQueue<string> queue = new ConcurrentQueue<string>();
    private static readonly object mutex = new object();

    public static void Push() {
        lock (mutex) {
            StringBuilder builder = new StringBuilder();
            while (queue.TryDequeue(out string result)) {
                builder.Append(result);
            }
            Console.Write(builder.ToString());
        }

        queue.Clear();
    }

    public static void Write(char text) =>
        queue.Enqueue(text.ToString());

    public static void Write(string text) =>
        queue.Enqueue(text);

    public static void WriteLine(string text) =>
        queue.Enqueue($"{text}\n");

    public static void ClearScreen() =>
        queue.Enqueue("\x1b[2J");

    public static void ClearLine() =>
        queue.Enqueue("\x1b[2K");

    public static void ResetAll() =>
        queue.Enqueue("\x1b[0m");

    public static void SetBold(bool value) =>
        queue.Enqueue($"\x1b[{(value ? 1 : 22)}m");

    public static void SetFaint(bool value) =>
        queue.Enqueue($"\x1b[{(value ? 2 : 22)}m");

    public static void SetItalic(bool value) =>
        queue.Enqueue($"\x1b[{(value ? 3 : 23)}m");

    public static void SetUnderline(bool value) =>
        queue.Enqueue($"\x1b[{(value ? 4 : 24)}m");

    public static void SetBlinking(bool value) =>
        queue.Enqueue($"\x1b[{(value ? 5 : 25)}m");

    public static void SetFgColor(byte r, byte g, byte b) =>
        queue.Enqueue($"\x1b[38;2;{r};{g};{b}m");

    public static void SetFgColor(byte[] rgb) =>
        queue.Enqueue($"\x1b[38;2;{rgb[0]};{rgb[1]};{rgb[2]}m");

    public static void SetBgColor(byte r, byte g, byte b) =>
        queue.Enqueue($"\x1b[48;2;{r};{g};{b}m");

    public static void SetBgColor(byte[] rgb) =>
        queue.Enqueue($"\x1b[48;2;{rgb[0]};{rgb[1]};{rgb[2]}m");

    public static void SetBlinkOn() =>
        queue.Enqueue("\x1b[6m");

    public static void SetBlinkOff() =>
        queue.Enqueue("\x1b[25m");

    public static void HideCursor() =>
        queue.Enqueue("\x1b[?25l");

    public static void ShowCursor() =>
        queue.Enqueue("\x1b[?25h");

    public static void SetCursorPosition(int x, int y) =>
        queue.Enqueue($"\x1b[{y};{x}H");

}