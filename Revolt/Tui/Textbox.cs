﻿namespace Revolt.Tui;

public sealed class Textbox(Frame parentFrame) : Element(parentFrame) {
    const int scrollInterval = 16;

    public Ansi.Color backColor = Glyphs.DARK_COLOR;
    public string placeholder = String.Empty;
    public Action action;

    private int index = 0;
    private int offset = 0;

    public bool enableHistory = false;
    public List<string> history = [];
    private int historyIndex = -1;

    private string _value = String.Empty;
    public string Value {
        get {
            return _value;
        }
        set {
            _value = value;
            index = value.Length;

            if (parentFrame.elements.Contains(this)) {
                DrawValue();
            }
        }
    }

    public override void Draw(bool push) {
        (int left, int top, int width, _) = GetBounding();
        int usableWidth = Math.Max(width, 0);

        Ansi.SetFgColor(Glyphs.LIGHT_COLOR);
        Ansi.SetBgColor(Glyphs.INPUT_COLOR);
        Ansi.SetCursorPosition(left, top);
        Ansi.Write(new String(' ', usableWidth));

        Ansi.SetFgColor(isFocused ? Glyphs.FOCUS_COLOR : Glyphs.INPUT_COLOR);
        Ansi.SetBgColor(backColor);
        Ansi.SetCursorPosition(left, top + 1);
        Ansi.Write(new String(Glyphs.UPPER_1_8TH_BLOCK, usableWidth));

        DrawValue();
    }

    private void DrawValue(int left = -1, int top = -1, int width = -1) {
        if (width == -1) (left, top, width, _) = GetBounding();

        int usableWidth = Math.Max(width - 1, 0);

        Ansi.SetFgColor(Glyphs.LIGHT_COLOR);
        Ansi.SetBgColor(Glyphs.INPUT_COLOR);
        Ansi.SetCursorPosition(left, top);

        if (!String.IsNullOrEmpty(placeholder) && _value.Length == 0) {
            Ansi.SetFgColor(Glyphs.GRAY_COLOR);
            //Ansi.SetFaint(true);
            Ansi.Write(placeholder);
            Ansi.Write(new String(' ', usableWidth - placeholder.Length));
            //Ansi.SetFaint(false);
            Ansi.SetCursorPosition(left + index, top);
            Ansi.Push();
            return;
        }

        if (_value.Length <= usableWidth) {
            Ansi.Write(_value);
            Ansi.Write(new String(' ', usableWidth - _value.Length));
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
            Ansi.Write(visible);

            Ansi.SetCursorPosition(left + usableWidth, top);
            Ansi.Write(' ');

            Ansi.SetCursorPosition(left + (index - offset), top);
        }

        Ansi.Push();
    }

    public override void HandleKey(ConsoleKeyInfo key) {
        (int left, int top, _, _) = GetBounding();

        switch (key.Key) {
        case ConsoleKey.LeftArrow:
            if (key.Modifiers == ConsoleModifiers.Control) {
                while (index > 0 && _value[index - 1] == ' ') index--;
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
                while (spaceIndex < _value.Length - 1 && _value[spaceIndex + 1] == ' ') spaceIndex++;
                if (spaceIndex == -1) {
                    spaceIndex = _value.Length;
                }
                index = Math.Clamp(spaceIndex + 1, 0, _value.Length);
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

        case ConsoleKey.UpArrow:
            if (!enableHistory) break;
            if (history.Count > 0) {
                if (historyIndex == -1) {
                    historyIndex = history.Count - 1;
                }
                else if (historyIndex > 0) {
                    historyIndex--;
                }
                _value = history[historyIndex];
                index = _value.Length;
            }
            break;

        case ConsoleKey.DownArrow:
            if (!enableHistory) break;
            if (historyIndex >= 0) {
                historyIndex++;
                if (historyIndex >= history.Count) {
                    historyIndex = -1;
                    _value = String.Empty;
                }
                else {
                    _value = history[historyIndex];
                }
                index = _value.Length;
            }
            break;

        case ConsoleKey.Backspace:
            if (index == 0) {
                Ansi.SetCursorPosition(left + index, top);
                Ansi.Push();
                break;
            }

            if (key.Modifiers == ConsoleModifiers.Control) {
                int spaceIndex = Math.Max(_value.LastIndexOf(' ', Math.Max(index - 1, 0)), 0);
                while (spaceIndex > 0 && _value[spaceIndex - 1] == ' ') spaceIndex--;

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

        case ConsoleKey.Enter:
            if (enableHistory && !String.IsNullOrWhiteSpace(_value)) {
                history.Add(_value);
                historyIndex = -1;
            }

            if (action is not null) {
                action();
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
        Ansi.Push();
    }

    public override void Blur(bool draw = true) {
        base.Blur(draw);
        Ansi.HideCursor();
        Ansi.Push();
    }
}