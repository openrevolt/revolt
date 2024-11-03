using System.Text;

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

    public void WriteLabel(string text, int x, int y, int width, bool alignCenter = false) {
        if (text is null) return;

        StringBuilder builder = new StringBuilder();
        string[] words = text.Split(' ');
        int xOffset = 0;

        for (int i = 0; i < words.Length; i++) {
            builder.Append(' ');
            builder.Append(words[i]);
            xOffset += words[i].Length + 1;
            if (xOffset >= width) break;
        }

        Ansi.SetCursorPosition(x, y);

        if (xOffset < width) {
            if (alignCenter) {
                string padding = new String(' ', (width - xOffset) / 2);
                Ansi.Write(padding);
                Ansi.Write(builder.ToString());
                Ansi.Write(new String(' ', width - padding.Length - builder.Length));
            }
            else {
                Ansi.Write(builder.ToString());
                Ansi.Write(new String(' ', width - xOffset));
            }
        }

    }
}
