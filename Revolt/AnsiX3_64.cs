using System.Collections.Concurrent;
using System.Text;

namespace Revolt;

public static class Ansi {
    private static readonly ConcurrentQueue<string> queue = new ConcurrentQueue<string>();

#if NET9_0_OR_GREATER
    private static readonly Lock mutex = new Lock();
#else
    private static readonly object mutex = new object();
#endif

    public static bool colorMode256 = false;
    static Ansi() => colorMode256 = OperatingSystem.IsMacOS();

    public static void Push() {
        if (queue.IsEmpty) return;

        lock (mutex) {
            StringBuilder builder = new StringBuilder();
            while (queue.TryDequeue(out string result)) {
                builder.Append(result);
            }
            Console.Write(builder.ToString());
        }
    }

    private static int Basic256Color(byte[] rgb) {
        if (rgb[0] == rgb[1] && rgb[1] == rgb[2]) {
            if (rgb[1] < 8) return 16;
            if (rgb[1] > 248) return 231;
            return 232 + (rgb[1] - 8) / 10;
        }
        
        return (rgb[0]/ 51) * 36 + (rgb[1] / 51) * 6 + (rgb[2] / 51) + 16;
    }
    
    private static int Basic256Color(byte r, byte g, byte b) {
        if (r == g && g == b) {
            if (g < 8) return 16;
            if (g > 248) return 231;
            return 232 + (g - 8) / 10;
        }
        
        return (r / 51) * 36 + (g / 51) * 6 + (b / 51) + 16;
    }

    public static void Write(char text) =>
    queue.Enqueue(text.ToString());

    public static void Write(string text) =>
    queue.Enqueue(text);

    public static void WriteLine(string text) =>
    queue.Enqueue($"{text}{Environment.NewLine}");

    public static void ClearScreen() =>
    Console.Clear();
    //queue.Enqueue("\x1b[2J");

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

    public static void SetFgColor(byte r, byte g, byte b) {
        if (colorMode256) {
            queue.Enqueue($"\x1b[38;5;{Basic256Color(r, g, b)}m");
        }
        else {
            queue.Enqueue($"\x1b[38;2;{r};{g};{b}m");
        }
    }

    public static void SetFgColor(byte[] rgb) {
        if (colorMode256) {
            queue.Enqueue($"\x1b[38;5;{Basic256Color(rgb)}m");
        }
        else {
            queue.Enqueue($"\x1b[38;2;{rgb[0]};{rgb[1]};{rgb[2]}m");
        }
    }

    public static void SetBgColor(byte r, byte g, byte b) {
        if (colorMode256) {
            queue.Enqueue($"\x1b[48;5;{Basic256Color(r, g, b)}m");
        }
        else {
            queue.Enqueue($"\x1b[48;2;{r};{g};{b}m");
        }
    }

    public static void SetBgColor(byte[] rgb) {
        if (colorMode256) {
            queue.Enqueue($"\x1b[48;5;{Basic256Color(rgb)}m");
        }
        else {
            queue.Enqueue($"\x1b[48;2;{rgb[0]};{rgb[1]};{rgb[2]}m");
        }
    }

    public static void SetBlinkOn() =>
    queue.Enqueue("\x1b[5m");

    public static void SetRapidBlinkOn() =>
    queue.Enqueue("\x1b[6m");

    public static void SetBlinkOff() =>
    queue.Enqueue("\x1b[25m");

    public static void HideCursor() =>
    queue.Enqueue("\x1b[?25l");

    public static void ShowCursor() =>
    queue.Enqueue("\x1b[?25h");

    public static void SetCursorPosition(int x, int y) =>
    queue.Enqueue($"\x1b[{y};{x}H");

    public static void ResetScrollRegion() =>
    queue.Enqueue("\x1b[r");

    public static void SetScrollRegion(int y0, int y1) =>
    queue.Enqueue($"\x1b[{y0};{y1}r");

    public static void ScrollUp() =>
    queue.Enqueue("\x1b[S");
    
    public static void ScrollUp(int n) =>
    queue.Enqueue($"\x1b[{n}S");

    public static void ScrollDown() =>
    queue.Enqueue("\x1b[T");
    public static void ScrollDown(int n) =>
    queue.Enqueue($"\x1b[{n}T");

}