namespace Revolt.Tui;

public sealed class ConfirmDialog : DialogBox {
    public string text;

    public ConfirmDialog() {
        defaultElement = okButton;
    }

    public override void Draw(int width, int height) {
        int left = (Renderer.LastWidth - width) / 2 + 1;
        int top = 1;

        string blank = new String(' ', width);

        Ansi.SetFgColor([16, 16, 16]);
        Ansi.SetBgColor(Data.DIALOG_COLOR);
        Ansi.SetCursorPosition(left, top++);
        Ansi.Write(blank);

        WriteLabel(text, left, top, width, true);

        top++;

        for (int i = 0; i < 4; i++) {
            Ansi.SetCursorPosition(left, top + i);
            Ansi.Write(blank);
        }

        Ansi.SetCursorPosition(left, top++);
        Ansi.Write(blank);

        okButton.left = left + (width - 20) / 2;
        okButton.top = top;

        cancelButton.left = left + (width - 20) / 2 + 10;
        cancelButton.top = top;

        if (elements is null) return;
        for (int i = 0; i < elements?.Count; i++) {
            elements[i].Draw(false);
        }

        Ansi.Push();
    }

    public override void Draw() {
        int width = Math.Min(Renderer.LastWidth, 48);
        Draw(width, 0);
    }

    public override bool HandleKey(ConsoleKeyInfo key) =>
    base.HandleKey(key);

    public override void Close() {
        Ansi.HideCursor();
        base.Close();
    }
}