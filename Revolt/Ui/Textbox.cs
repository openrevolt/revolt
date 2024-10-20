﻿namespace Revolt.Ui;

public sealed class Textbox(Frame parentFrame) : Element(parentFrame) {
    const int scrollInterval = 16;

    private int index = 0;
    private int offset = 0;

    private string _value = String.Empty;

    public string Value {
        get {
            return _value;
        }
        set {
            _value = value;
            index = value.Length;
            DrawValue();
        }
    }

    public override void Draw() {
        (int left, int top, int width, _) = GetBounding();
        int usableWidth = width - 2;

        Ansi.SetFgColor(Data.FG_COLOR);
        Ansi.SetBgColor(Data.INPUT_COLOR);
        Ansi.SetCursorPosition(left, top);
        Console.Write(new String(' ', usableWidth));

        Ansi.SetFgColor(isFocused ? Data.SELECT_COLOR : Data.INPUT_COLOR);
        Ansi.SetBgColor(Data.PANE_COLOR);
        Ansi.SetCursorPosition(left, top + 1);

        Console.Write(new String(Data.UPPER_1_8TH_BLOCK, usableWidth));

        DrawValue();
    }

    private void DrawValue() {
        (int left, int top, int width, _) = GetBounding();
        int usableWidth = width - 3;

        Ansi.SetFgColor(Data.FG_COLOR);
        Ansi.SetBgColor(Data.INPUT_COLOR);
        Ansi.SetCursorPosition(left, top);

        if (_value.Length <= usableWidth) {
            Console.Write(_value);
            Console.Write(new String(' ', usableWidth - _value.Length));
            Ansi.SetCursorPosition(left + index, top);
        }
        else {
            if (index < offset) {
                offset = Math.Max(0, index - (index % scrollInterval));
            }
            else if (index >= offset + usableWidth) {
                offset = Math.Min(_value.Length - usableWidth, index - usableWidth + scrollInterval);
            }

            if (_value.Length - offset < usableWidth) {
                offset = Math.Max(0, _value.Length - usableWidth);
            }

            string visible = _value.Substring(offset, Math.Min(usableWidth, _value.Length - offset));
            Console.Write(visible);

            Ansi.SetCursorPosition(left + usableWidth, top);
            Console.Write(' ');

            Ansi.SetCursorPosition(left + (index - offset), top);
        }
    }

    public override void HandleKey(ConsoleKeyInfo key) {
        (int left, int top, _, _) = GetBounding();

        switch (key.Key) {
        case ConsoleKey.LeftArrow:
            if (key.Modifiers == ConsoleModifiers.Control) {
                int spaceIndex = _value.LastIndexOf(' ', Math.Max(index - 1, 0));
                if (spaceIndex == index - 1) {
                    spaceIndex = _value.LastIndexOf(' ', Math.Max(spaceIndex - 1, 0));
                }
                index = Math.Clamp(spaceIndex + 1, 0, _value.Length);
            }
            else {
                index = Math.Clamp(index - 1, 0, _value.Length);
            }
            break;

        case ConsoleKey.RightArrow:
            if (key.Modifiers == ConsoleModifiers.Control) {
                int spaceIndex = _value.IndexOf(' ', Math.Min(index + 1, _value.Length));
                if (spaceIndex == -1) {
                    spaceIndex = _value.Length;
                }
                index = spaceIndex;
            }
            else {
                index = Math.Clamp(index + 1, 0, _value.Length);
            }
            break;

        case ConsoleKey.Home:
            index = 0;
            break;

        case ConsoleKey.End:
            index = _value.Length;
            break;

        case ConsoleKey.Backspace:
            if (index == 0) {
                Ansi.SetCursorPosition(left + index, top);
                break;
            }
            
            if (key.Modifiers == ConsoleModifiers.Control) {
                int spaceIndex = Math.Max(_value.LastIndexOf(' ', Math.Max(index - 1, 0)), 0);

                _value = _value[..spaceIndex] + _value[index..];

                index = spaceIndex;
            }
            else {
                _value = _value[..(Math.Max(index - 1, 0))] + _value[index..];
                index--;
            }
            break;

        case ConsoleKey.Delete:
            if (key.Modifiers == ConsoleModifiers.Control) {
                int spaceIndex = _value.IndexOf(' ', Math.Min(index + 1, _value.Length));
                if (spaceIndex == -1) {
                    spaceIndex = _value.Length;
                }
                _value = _value[..index] + _value[spaceIndex..];
            }
            else if (index < _value.Length) {
                _value = _value[..index] + _value[(index + 1)..];
            }
            break;

        default:
            byte asci = (byte)key.KeyChar;
            if (asci > 31 && asci < 127) {
                _value = _value[..index] + key.KeyChar + _value[index..];
                index++;
            }
            break;
        }

        DrawValue();
    }

    public override void Focus(bool draw = true) {
        base.Focus(draw);
        Ansi.ShowCursor();
    }

    public override void Blur(bool draw = true) {
        base.Blur(draw);
        Ansi.HideCursor();
    }
}