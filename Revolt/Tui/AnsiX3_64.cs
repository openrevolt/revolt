using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Concurrent;
using System.Drawing;
using System.Text;
using static Revolt.Ansi;

namespace Revolt;

public static class Ansi {

    public readonly struct Color {
        public readonly byte r, g, b;

        public Color(byte r, byte g, byte b) {
            this.r = r;
            this.g = g;
            this.b = b;
        }

        public static Color operator +(Color left, int right) =>
            new Color(
                (byte)Math.Min(left.r + right, 255),
                (byte)Math.Min(left.g + right, 255),
                (byte)Math.Min(left.b + right, 255)
            );

        public static bool operator ==(Color left, Color right) =>
             left.r == right.r && left.g == right.g && left.b == right.b;
        
        public static bool operator !=(Color left, Color right) =>
             left.r != right.r || left.g != right.g || left.b != right.b;

        public override bool Equals(object obj) {
            if (obj is Color other) return this == other;
            return false;
        }

        public override int GetHashCode() =>
            (r << 16) + (g << 8) + b;

    }

    private static readonly ConcurrentQueue<string> queue = new ConcurrentQueue<string>();

#if NET9_0_OR_GREATER
    private static readonly Lock mutex = new Lock();
#else
    private static readonly object mutex = new object();
#endif

    public static bool colorMode256 = true;
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

    private static int Basic256Color(Color color) {
        if (color.r == color.g && color.g == color.b) {
            if (color.g < 8) return 16;
            if (color.g > 248) return 231;
            return 232 + (color.g - 8) / 10;
        }
        
        return (color.r / 51) * 36 + (color.g / 51) * 6 + (color.b / 51) + 16;
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

    public static void SetFgColor(Color color) {
        if (colorMode256) {
            queue.Enqueue($"\x1b[38;5;{Basic256Color(color)}m");
        }
        else {
            queue.Enqueue($"\x1b[38;2;{color.r};{color.g};{color.b}m");
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

    public static void SetBgColor(Color color) {
        if (colorMode256) {
            queue.Enqueue($"\x1b[48;5;{Basic256Color(color)}m");
        }
        else {
            queue.Enqueue($"\x1b[48;2;{color.r};{color.g};{color.b}m");
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