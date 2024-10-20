namespace Revolt.Ui;

public sealed class Popup : Frame {
    public int lastWidth = 48;

    public string text;
    public Textbox valueTextbox;
    public Button okButton;
    public Button cancelButton;

    public Popup() {
        valueTextbox = new Textbox(this);
        okButton = new Button(this, "   OK   ");
        cancelButton = new Button(this, " Cancel ");

        elements.Add(valueTextbox);
        elements.Add(okButton);
        elements.Add(cancelButton);

        defaultElement = valueTextbox;
        valueTextbox.Focus(false);
        focusedElement = valueTextbox;
    }

    public override void Draw(int width, int height) {
        lastWidth = width;

        int left = (Renderer.LastWidth - width) / 2 + 1;
        int top = 1;

        string gap = new string(' ', width);

        Ansi.SetBgColor(Data.PANE_COLOR);
        Ansi.SetCursorPosition(left, top++);
        Console.Write(gap);

        if (text is not null) {
            string[] words = text.Split(' ');
            int xOffset = 0;

            for (int i = 0; i < words.Length; i++) {
                Ansi.SetCursorPosition(left + xOffset, top);
                Console.Write(' ');

                Console.Write(words[i]);
                xOffset += words[i].Length + 1;
                if (xOffset >= width) break;
            }

            if (xOffset < width) {
                Console.Write(new string(' ', width - xOffset));
            }
        }

        valueTextbox.left = left;
        valueTextbox.top = top;

        top++;

        for (int i = 0; i < 4; i++) {
            Ansi.SetCursorPosition(left, top + i);
            Console.Write(gap);
        }

        Ansi.SetCursorPosition(left, top++);
        Console.Write(gap);

        okButton.left = left + (width - 20) / 2;
        okButton.top = top;

        cancelButton.left = left + (width - 20) / 2 + 10;
        cancelButton.top = top;

        if (elements is null) return;
        for (int i = 0; i < elements?.Count; i++) {
            elements[i].Draw();
        }

        if (focusedElement == valueTextbox) {
            valueTextbox.Focus();
        }
    }

    public void Draw() {
        int width = Math.Min(Renderer.LastWidth, 48);
        Draw(width, 0);
    }

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

    public static void Close() {
        Renderer.Popup = null;
        Renderer.Redraw();
    }

}