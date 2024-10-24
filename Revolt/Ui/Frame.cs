﻿namespace Revolt.Ui;

public abstract class Frame {
    protected List<Element> elements = [];
    protected Element defaultElement;
    public Element focusedElement;

    public virtual void Show(bool draw = true) {
        Renderer.ActiveFrame = this;

        if (defaultElement is not null) {
            focusedElement = defaultElement;
            focusedElement.Focus(false);
        }
        
        if (draw) {
            Renderer.Redraw();
        }
    }

    public virtual void Draw(int width, int height) {
        int top = 0;

        Ansi.SetBgColor(Data.BG_COLOR);
        for (int y = top; y <= height; y++) {
            if (y > Console.WindowHeight) break;

            Ansi.SetCursorPosition(0, y);
            for (int x = 0; x < width; x++) {
                Console.Write(' ');
            }
        }

        if (elements is null) return;

        for (int i = 0; i < elements?.Count; i++) {
            elements[i].Draw();
        }
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

    public abstract bool HandleKey(ConsoleKeyInfo key);

}