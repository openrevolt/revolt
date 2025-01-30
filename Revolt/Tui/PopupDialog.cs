namespace Revolt.Tui;

public sealed class PopupDialog : DialogBox {
    public string text;

    public PopupDialog() {
        okButton.action = Close;
        okButton.text = "Close";
        defaultElement = okButton;
        elements.Remove(cancelButton);
    }

    public override void Draw(int width, int height) {
        int left = 8;
        int top = 4;

        if (width < 80 || height < 20) {
            left   = 0;
            top    = 0;
            width  = Renderer.LastWidth;
            height = Renderer.LastHeight;
        }
        else {
            left   = 8;
            top    = 4;
            width  -= 16;
            height -= 8;
        }

        string blank = new String(' ', width);

        Ansi.SetFgColor([16, 16, 16]);
        Ansi.SetBgColor(Glyphs.DIALOG_COLOR);

        for (int i = 0; i < height; i++) {
            Ansi.SetCursorPosition(left, top + i);
            Ansi.Write(blank);
        }


        okButton.left = left + (width - 8) / 2;
        okButton.top = top;

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