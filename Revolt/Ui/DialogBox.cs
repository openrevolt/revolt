namespace Revolt.Ui;

public class DialogBox : Frame {
    public Button okButton;
    public Button cancelButton;

    public DialogBox() {
        okButton = new Button(this, "   OK   ");
        cancelButton = new Button(this, " Cancel ");

        elements.Add(okButton);
        elements.Add(cancelButton);

        cancelButton.action = Close;
    }

    public virtual void Draw() { }

    public override bool HandleKey(ConsoleKeyInfo key) {
        switch (key.Key) {
        case ConsoleKey.Tab:
            if (key.Modifiers == ConsoleModifiers.Shift) {
                FocusPrevious();
            }
            else {
                FocusNext();
            }
            break;

        case ConsoleKey.Escape:
            Close();
            break;

        default:
            focusedElement?.HandleKey(key);
            break;
        }

        return true;
    }

    public virtual void Close() {
        okButton.action = null;
        cancelButton.action = null;
        Renderer.ActiveDialog = null;
        Renderer.Redraw();
    }

    public void WriteLabel(string text, int x, int y, int width) {
        if (text is not null) {
            string[] words = text.Split(' ');
            int xOffset = 0;

            for (int i = 0; i < words.Length; i++) {
                Ansi.SetCursorPosition(x + xOffset, y);
                Ansi.Write(' ');

                Ansi.Write(words[i]);
                xOffset += words[i].Length + 1;
                if (xOffset >= width) break;
            }

            if (xOffset < width) {
                Ansi.Write(new String(' ', width - xOffset));
            }
        }
    }
}
