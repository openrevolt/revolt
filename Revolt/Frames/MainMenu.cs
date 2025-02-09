using Revolt.Tui;
using System.Reflection;

namespace Revolt.Frames;

public sealed class MainMenu : Tui.Frame {
    public int index;

    public string[] menu = [
        "Ping",
        "DNS lookup",
        "Network mapper",
        //"Relay server",
        "Packet sniffer",
        null,
        "Configuration",
        "Quit"
    ];

    public static readonly MainMenu instance = new MainMenu();

    public override void Show(bool draw = true) {
        Ansi.HideCursor();
        base.Show(draw);
    }

    public override void Draw(int width, int height) {
        base.Draw(width, height);

        Ansi.SetFgColor(Glyphs.LIGHT_COLOR);
        Ansi.SetBgColor(Glyphs.DARK_COLOR);

        Ansi.SetCursorPosition(0, 0);

        WriteBanner();

        for (int i = 0; i < menu.Length; i++) {
            DrawItem(i, width, height);
        }

        Ansi.Push();
    }

    public static void WriteBanner() {
        Ansi.WriteLine(@"  ______                _ _");
        Ansi.WriteLine(@"  | ___ \              | | |");
        Ansi.WriteLine(@"  | |_/ /_____   _____ | | |_");
        Ansi.WriteLine(@"  |    // _ \ \ / / _ \| | __|");
        Ansi.WriteLine(@"  | |\ \  __/\ V / (_) | | |_ ");
        Ansi.WriteLine(@"  \_| \_\___| \_/ \___/|_|\__|");

        Version ver = Assembly.GetExecutingAssembly().GetName()?.Version;
        string version = $"{ver?.Major ?? 0}.{ver?.Minor ?? 0}.{ver?.Build ?? 0}.{ver?.Revision ?? 0}";
        Ansi.WriteLine($"{version,30}");
    }

    public void DrawItem(int i, int width, int height) {
        int y = i + 9;
        if (y > height) return;

        int length = Math.Min(width / 2, 28);

        if (String.IsNullOrEmpty(menu[i])) {
            Ansi.SetFgColor(Glyphs.DIMGRAY_COLOR);
            Ansi.SetBgColor(Glyphs.DARK_COLOR);
            Ansi.SetCursorPosition(3, y);

            Ansi.Write(new String('-', length));
            return;
        }

        string item = " " + menu[i].PadRight(length - 1);
        if (item.Length > length) {
            item = item[..(length - 2)] + "..";
        }

        Ansi.SetCursorPosition(3, y);

        if (i == index) {
            Ansi.SetFgColor(Glyphs.DARKGRAY_COLOR);
            Ansi.SetBgColor(Glyphs.FOCUS_COLOR);
        }
        else {
            Ansi.SetFgColor(Glyphs.LIGHT_COLOR);
            Ansi.SetBgColor(Glyphs.DARK_COLOR);
        }

        Ansi.Write(item);
    }

    public override bool HandleKey(ConsoleKeyInfo key) {
        switch (key.Key) {
        case ConsoleKey.Escape:
            return false;

        case ConsoleKey.Enter:
            return Enter();

        case ConsoleKey.Tab:
            Tab(key.Modifiers == ConsoleModifiers.Shift);
            return true;

        case ConsoleKey.LeftArrow:
        case ConsoleKey.UpArrow:
            SelectPrevious();
            return true;

        case ConsoleKey.RightArrow:
        case ConsoleKey.DownArrow:
            SelectNext();
            return true;
        }

        return true;
    }

    public override string[][] GetKeyShortcuts() =>
    [
        ["ESC", "Quit"],
        ["F1",  "Help"],
        ["F5",  "Refresh"],
    ];

    public bool Enter() {
        switch (menu[index]) {
        case "Ping":
            PingFrame.instance.Show();
            return true;

        case "DNS lookup":
            DnsFrame.instance.Show();
            return true;

        case "Network mapper":
            NetMapperFrame.instance.Show();
            return true;

        case "Packet sniffer":
            SnifferFrame.instance.Show();
            return true;

        case "Quit":
             return false;

        default:
            return true;
        }
    }

    public void Tab(bool invert) {
        if (menu is null) return;

        int lastIndex = index;

        if (!invert && lastIndex == menu.Length - 1) {
            index = 0;
        }
        else if (invert && lastIndex == 0) {
            index = menu.Length - 1;
        }
        else {
            index = Math.Min(menu.Length - 1, index + (invert ? -1 : 1));
        }

        if (String.IsNullOrEmpty(menu[index])) {
            index = Math.Min(menu.Length - 1, index + (invert ? -1 : 1));
        }

        DrawItem(lastIndex, Renderer.LastWidth, Renderer.LastHeight);
        DrawItem(index, Renderer.LastWidth, Renderer.LastHeight);
        Ansi.Push();
    }

    public void SelectPrevious() {
        if (menu is null) return;

        int lastIndex = index;
        index = Math.Max(0, index - 1);

        if (String.IsNullOrEmpty(menu[index])) {
            index = Math.Max(0, index - 1);
        }

        if (index == lastIndex) return;

        DrawItem(lastIndex, Renderer.LastWidth, Renderer.LastHeight);
        DrawItem(index, Renderer.LastWidth, Renderer.LastHeight);
        Ansi.Push();
    }

    public void SelectNext() {
        if (menu is null) return;

        int lastIndex = index;
        index = Math.Min(menu.Length - 1, index + 1);

        if (String.IsNullOrEmpty(menu[index])) {
            index = Math.Min(menu.Length - 1, index + 1);
        }

        if (index == lastIndex) return;

        DrawItem(lastIndex, Renderer.LastWidth, Renderer.LastHeight);
        DrawItem(index, Renderer.LastWidth, Renderer.LastHeight);
        Ansi.Push();
    }
}
