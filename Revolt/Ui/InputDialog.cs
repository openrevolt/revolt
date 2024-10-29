﻿namespace Revolt.Ui;

public sealed class InputDialog : DialogBox {
    public string text;
    public Textbox valueTextbox;

    public InputDialog() {
        valueTextbox = new Textbox(this) {
            backColor = Data.PANE_COLOR
        };

        elements.Add(valueTextbox);

        defaultElement = valueTextbox;
        valueTextbox.Focus(false);
        focusedElement = valueTextbox;
    }

    public override void Draw(int width, int height) {
        int left = (Renderer.LastWidth - width) / 2 + 1;
        int top = 1;

        string blank = new String(' ', width);

        Ansi.SetFgColor([16, 16, 16]);
        Ansi.SetBgColor(Data.PANE_COLOR);
        Ansi.SetCursorPosition(left, top++);
        Ansi.Write(blank);

        if (text is not null) {
            string[] words = text.Split(' ');
            int xOffset = 0;

            for (int i = 0; i < words.Length; i++) {
                Ansi.SetCursorPosition(left + xOffset, top);
                Ansi.Write(' ');

                Ansi.Write(words[i]);
                xOffset += words[i].Length + 1;
                if (xOffset >= width) break;
            }

            if (xOffset < width) {
                Ansi.Write(new String(' ', width - xOffset));
            }
        }

        valueTextbox.left = left;
        valueTextbox.right = Renderer.LastWidth - width - left + 2;
        valueTextbox.top = top;

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

        if (focusedElement == valueTextbox) {
            valueTextbox.Focus();
        }

        Ansi.Push();
    }

    public override void Draw() {
        int width = Math.Min(Renderer.LastWidth, 48);
        Draw(width, 0);
    }

    public override bool HandleKey(ConsoleKeyInfo key) {
        switch (key.Key) {
        case ConsoleKey.Escape:
            if (valueTextbox.Value.Length == 0) {
                Close();
            }
            else {
                valueTextbox.Value = String.Empty;
            }
            return true;

        case ConsoleKey.Enter:
            if (focusedElement == valueTextbox && okButton.action is not null) {
                okButton.action();
                return true;
            }
            else {
                return base.HandleKey(key);
            }

        default:
            return base.HandleKey(key);
        }
    }

    public override void Close() {
        Ansi.HideCursor();
        base.Close();
    }
}