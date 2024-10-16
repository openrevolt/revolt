﻿namespace Revolt;

public sealed class UiButton : UiElement {
    public string text;

    public UiButton(UiFrame parentFrame, string text) : base(parentFrame) {
        this.text = text;
    }

    public override void Draw() {
        (int left, int top, _, _) = GetBounding();

        byte[] foreColor = isFocused ? [16,16,16] : Data.FG_COLOR;
        byte[] backColor = isFocused ? Data.SELECT_COLOR : Data.CONTROL_COLOR;

        Ansi.SetFgColor(foreColor);
        Ansi.SetBgColor(backColor);
        Ansi.SetCursorPosition(left, top);

        Console.Write(text);

        Ansi.SetFgColor(backColor);
        Ansi.SetBgColor(Data.PANE_COLOR);
        Ansi.SetCursorPosition(left, top + 1);

        for (int i = 0; i < text.Length; i++) {
            Console.Write(Data.UPPER_1_8TH_BLOCK);
        }
    }

    public override void HandleKey(ConsoleKeyInfo key) {

    }
}
