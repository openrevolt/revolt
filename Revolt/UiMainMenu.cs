namespace Revolt;

public sealed class UiMainMenu : UiFrame {
    public string[] menu;
    public int index;

    public UiMainMenu() {
        //rows = [1];
        //cols = [2];

        menu = [
            " Ping",
            " DNS lookup",
            " Trace route",
            " IP discovery",
            " Reverse proxy",
            " Packet capture",
            null,
            " Options",
            " Exit"
        ];
    }

    public override void Draw(int width, int height) {
        base.Draw(width, height);

        for (int i = 0; i < menu.Length; i++) {
            DrawItem(i, width, height);
        }
    }

    public void DrawItem(int i, int width, int height) {
        if (String.IsNullOrEmpty(menu[i])) {
            return;
        }

        int length = Math.Min(width / 2, 32);
        string item = menu[i].PadRight(length);
        if (item.Length > length) {
            item = item.Substring(0, length - 2) + "..";
        }

        Ansi.SetCursorPosition(4, i + 2);

        if (i == index) {
            Ansi.SetFgColor([0, 0, 0]);
            Ansi.SetBgColor(SELECT_COLOR);
        }
        else {
            Ansi.SetFgColor(FG_COLOR);
            Ansi.SetBgColor(BG_COLOR);
        }

        Console.Write(item);
    }

    public override bool HandleKey(ConsoleKey key) {
        switch (key) {
            case ConsoleKey.Escape : return false;
            case ConsoleKey.Enter      : Enter();          return true;
            case ConsoleKey.LeftArrow  : SelectPrevious(); return true;
            case ConsoleKey.UpArrow    : SelectPrevious(); return true;
            case ConsoleKey.RightArrow : SelectNext();     return true;
            case ConsoleKey.DownArrow  : SelectNext();     return true;
        }

        return true;
    }

    public void Enter() {

    }

    public void SelectPrevious() {
        if (menu is null) return;

        int lastIndex = index;
        index = Math.Max(0, index - 1);

        if (String.IsNullOrEmpty(menu[index])) {
            index = Math.Max(0, index - 1);
        }

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

        DrawItem(lastIndex, Renderer.lastWidth, Renderer.lastHeight);
        DrawItem(index, Renderer.lastWidth, Renderer.lastHeight);
    }
}
