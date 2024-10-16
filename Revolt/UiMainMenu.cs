namespace Revolt;

public sealed class UiMainMenu : UiFrame {
    public string[] menu;
    public int index;

    public UiMainMenu() {
        menu = [
            "Ping",
            "DNS lookup",
            "Trace route",
            "IP discovery",
            "Reverse proxy",
            "Packet capture",
            null,
            "Options",
            "Exit"
        ];
    }

    public override void Draw(int width, int height) {
        base.Draw(width, height);

        Ansi.SetFgColor(FG_COLOR);
        Ansi.SetBgColor(BG_COLOR);

        Ansi.SetCursorPosition(0, 0);

        Program.DrawBanner();

        for (int i = 0; i < menu.Length; i++) {
            DrawItem(i, width, height);
        }
    }

    public void DrawItem(int i, int width, int height) {
        int length = Math.Min(width / 2, 28);

        if (String.IsNullOrEmpty(menu[i])) {
            Ansi.SetFgColor([64,64,64]);
            Ansi.SetBgColor(BG_COLOR);
            Ansi.SetCursorPosition(3, i + 9);

            Console.Write(new string('-', length));
            return;
        }

        string item = " " + menu[i].PadRight(length-1);
        if (item.Length > length) {
            item = item.Substring(0, length - 2) + "..";
        }

        Ansi.SetCursorPosition(3, i + 9);

        if (i == index) {
            Ansi.SetFgColor([16, 16, 16]);
            Ansi.SetBgColor(SELECT_COLOR);
        }
        else {
            Ansi.SetFgColor(FG_COLOR);
            Ansi.SetBgColor(BG_COLOR);
        }

        Console.Write(item);
    }

    public override bool HandleKey(ConsoleKeyInfo key) {
        switch (key.Key) {

        case ConsoleKey.Escape:
            return false;

        case ConsoleKey.Enter:
            return Enter();

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
            Renderer.pingFrame ??= new PingFrame();
            Renderer.activeFrame = Renderer.pingFrame;
            Renderer.Redraw();
            return true;

        case "Exit":
            return false;

        default:
            return true;
        }
    }

    public void SelectPrevious() {
        if (menu is null) return;

        int lastIndex = index;
        index = Math.Max(0, index - 1);

        if (String.IsNullOrEmpty(menu[index])) {
            index = Math.Max(0, index - 1);
        }

        if (index == lastIndex) return;

        DrawItem(lastIndex, Renderer.lastWidth, Renderer.lastHeight);
        DrawItem(index, Renderer.lastWidth, Renderer.lastHeight);
    }

    public void SelectNext() {
        if (menu is null) return;

        int lastIndex = index;
        index = Math.Min(menu.Length - 1, index + 1);
        
        if (String.IsNullOrEmpty(menu[index])) {
            index = Math.Min(menu.Length - 1, index + 1);
        }

        if (index == lastIndex) return;

        DrawItem(lastIndex, Renderer.lastWidth, Renderer.lastHeight);
        DrawItem(index, Renderer.lastWidth, Renderer.lastHeight);
    }
}
