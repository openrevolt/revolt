using System.Text;

namespace Revolt.Tui;

public abstract class Frame {
    protected List<Element> elements = [];
    protected Element defaultElement;
    public Element focusedElement;

    public virtual void Show(bool draw = true) {
        Renderer.ActiveFrame = this;

        if (defaultElement is not null && focusedElement is null) {
            focusedElement = defaultElement;
            focusedElement.Focus(false);
        }
        
        if (draw) {
            Renderer.Redraw();
        }

        defaultElement?.Focus(true);
    }

    public virtual void Draw(int width, int height) {
        string blank = new string(' ', width);

        int top = 0;
        Ansi.SetBgColor(Data.DARK_COLOR);
        for (int y = top; y <= height; y++) {
            if (y > Console.WindowHeight) break;

            Ansi.SetCursorPosition(0, y);
            Ansi.Write(blank);
        }

        if (elements is null) return;

        for (int i = 0; i < elements?.Count; i++) {
            elements[i].Draw(false);
        }

        Ansi.Push();
    }

    public void FocusPrevious() {
        if (elements is null || elements.Count == 0) return;

        if (focusedElement is null && defaultElement is not null) {
            focusedElement = defaultElement;
            focusedElement.Focus();
            return;
        }

        int index = elements.IndexOf(focusedElement);
        if (index < 0) return;

        focusedElement.Blur();

        index--;
        if (index < 0) {
            index = elements.Count - 1;
        }

        focusedElement = elements[index];
        focusedElement.Focus();
    }

    public void FocusNext() {
        if (elements is null || elements.Count == 0) return;

        if (focusedElement is null && defaultElement is not null) {
            focusedElement = defaultElement;
            focusedElement.Focus();
            return;
        }

        int index = elements.IndexOf(focusedElement);
        if (index < 0) return;

        focusedElement.Blur();

        index = (index + 1) % elements.Count;
        focusedElement = elements[index];
        focusedElement.Focus();
    }

    public static void WriteLabel(string text, int x, int y, int width, bool alignCenter = false) {
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

    public abstract bool HandleKey(ConsoleKeyInfo key);

}