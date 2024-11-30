namespace Revolt.Frames;

public sealed class MainMenu : Ui.Frame {
    public string[] menu;
    public int index;

    public static MainMenu Instance { get; } = new MainMenu();

    public MainMenu() {
        menu = [
            "Ping",
            "DNS lookup",
            "Trace route",
            "IP discovery",
            //"Reverse proxy",
            "Packet sniffer",
            null,
            "Configuration",
            "Quit"
        ];
    }

    public override void Show(bool draw = true) {
        Ansi.HideCursor();
        base.Show(draw);
    }

    public override void Draw(int width, int height) {
        base.Draw(width, height);

        Ansi.SetFgColor(Data.LIGHT_COLOR);
        Ansi.SetBgColor(Data.DARK_COLOR);

        Ansi.SetCursorPosition(0, 0);

        Program.DrawBanner();

        for (int i = 0; i < menu.Length; i++) {
            DrawItem(i, width, height);
        }

        Ansi.Push();
    }

    public void DrawItem(int i, int width, int height) {
        int y = i + 9;
        if (y > height) return;

        int length = Math.Min(width / 2, 28);

        if (String.IsNullOrEmpty(menu[i])) {
            Ansi.SetFgColor([64, 64, 64]);
            Ansi.SetBgColor(Data.DARK_COLOR);
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
            Ansi.SetFgColor([16, 16, 16]);
            Ansi.SetBgColor(Data.SELECT_COLOR);
        }
        else {
            Ansi.SetFgColor(Data.LIGHT_COLOR);
            Ansi.SetBgColor(Data.DARK_COLOR);
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

    public bool Enter() {
        switch (menu[index]) {
        case "Ping":
            PingFrame.Instance.Show();
            return true;

        case "DNS lookup":
            DnsFrame.Instance.Show();
            return true;

        case "Trace route":
            TraceRouteFrame.Instance.Show();
            return true;

        case "IP discovery":
            IpDiscoveryFrame.Instance.Show();
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
