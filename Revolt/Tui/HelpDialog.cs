namespace Revolt.Tui;

public sealed class HelpDialog : DialogBox {
    private ShortcutListBox list;

    public HelpDialog(string[][] shortcuts) {
        elements.Remove(okButton);
        elements.Remove(cancelButton);

        list = new ShortcutListBox(this, shortcuts);
        elements.Add(list);
    }

    public override void Draw(int width, int height) {
        int left, top;
        if (width < 80 || height < 20) {
            left   = 0;
            top    = 0;
            width  = Renderer.LastWidth;
            height = Renderer.LastHeight;
        }
        else {
            left   = 16;
            top    = 4;
            width  -= 32;
            height -= 8;
        }

        string blank = new String(' ', width);

        Ansi.SetFgColor([16, 16, 16]);
        Ansi.SetBgColor(Glyphs.DIALOG_COLOR);

        for (int i = 0; i <= height; i++) {
            Ansi.SetCursorPosition(left, top + i);
            Ansi.Write(blank);
        }

        list.left = left;
        list.top = top;
        list.right = left;
        list.bottom = top;

        for (int i = 0; i < elements.Count; i++) {
            elements[i].Draw(false);
        }

        Ansi.Push();
    }

    public override void Draw() =>
        Draw(Renderer.LastWidth, Renderer.LastHeight);

    public override bool HandleKey(ConsoleKeyInfo key) {
        this.Close();
        return true;
    }

    public override void Close() {
        Ansi.HideCursor();
        base.Close();
    }
}

public sealed class ShortcutListBox(Frame parentFrame, string[][] shortcuts) : Element(parentFrame) {
    string[][] shortcuts = shortcuts;

    public override void Draw(bool push) {
        (int left, int top, int width, int height) = GetBounding();

        Ansi.SetFgColor(Glyphs.DARK_COLOR);

        for (int i = 0; i < shortcuts.Length; i++ ) {
            Ansi.SetBgColor(i % 2 == 0 ? [144, 144, 144] : Glyphs.DIALOG_COLOR);

            Frame.WriteLabel(shortcuts[i][0], left + 2, top + 1 + i, 8);
            Frame.WriteLabel(shortcuts[i][1], left + 8, top + 1 + i, width - 12);
        }

        if (push) {
            Ansi.Push();
        }
    }

    public override void HandleKey(ConsoleKeyInfo key) { }
}
