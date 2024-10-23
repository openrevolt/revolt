namespace Revolt.Frames;

public sealed class MainMenu : Ui.Frame {
    public string[] menu;
    public int index;

    public static readonly MainMenu singleton;
    static MainMenu() {
        singleton = new MainMenu();
    }

    public MainMenu() {
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

    public override void Show(bool draw = true) {
        Ansi.HideCursor();
        base.Show(draw);
    }

    public override void Draw(int width, int height) {
        base.Draw(width, height);

        Ansi.SetFgColor(Data.FG_COLOR);
        Ansi.SetBgColor(Data.BG_COLOR);

        Ansi.SetCursorPosition(0, 0);

        Program.DrawBanner();

        for (int i = 0; i < menu.Length; i++) {
            DrawItem(i, width, height);
        }
    }

    public void DrawItem(int i, int width, int height) {
        int y = i + 9;
        if (y > height) return;

        int length = Math.Min(width / 2, 28);

        if (string.IsNullOrEmpty(menu[i])) {
            Ansi.SetFgColor([64, 64, 64]);
            Ansi.SetBgColor(Data.BG_COLOR);
            Ansi.SetCursorPosition(3, y);

            Console.Write(new String(Data.LINE_H, length));
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
            Ansi.SetFgColor(Data.FG_COLOR);
            Ansi.SetBgColor(Data.BG_COLOR);
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
            PingFrame.singleton.Show();
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

        if (string.IsNullOrEmpty(menu[index])) {
            index = Math.Max(0, index - 1);
        }

        if (index == lastIndex) return;

        DrawItem(lastIndex, Renderer.LastWidth, Renderer.LastHeight);
        DrawItem(index, Renderer.LastWidth, Renderer.LastHeight);
    }

    public void SelectNext() {
        if (menu is null) return;

        int lastIndex = index;
        index = Math.Min(menu.Length - 1, index + 1);

        if (string.IsNullOrEmpty(menu[index])) {
            index = Math.Min(menu.Length - 1, index + 1);
        }

        if (index == lastIndex) return;

        DrawItem(lastIndex, Renderer.LastWidth, Renderer.LastHeight);
        DrawItem(index, Renderer.LastWidth, Renderer.LastHeight);
    }
}
