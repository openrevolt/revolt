using System.ComponentModel.DataAnnotations;

namespace Revolt.Ui;

public sealed class Textbox(Frame parentFrame) : Element(parentFrame) {
    private int index = 0;
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
        int calculatedWidth = width - 2;

        Ansi.SetFgColor(Data.FG_COLOR);
        Ansi.SetBgColor(Data.INPUT_COLOR);
        Ansi.SetCursorPosition(left, top);

        Console.Write(new String(' ', calculatedWidth));

        Ansi.SetFgColor(isFocused ? Data.SELECT_COLOR : Data.INPUT_COLOR);
        Ansi.SetBgColor(Data.PANE_COLOR);
        Ansi.SetCursorPosition(left, top + 1);

        Console.Write(new String(Data.UPPER_1_8TH_BLOCK, calculatedWidth));

        DrawValue();
    }

    private void DrawValue() {
        (int left, int top, int width, _) = GetBounding();
        int calculatedWidth = width - 2;

        Ansi.SetFgColor(Data.FG_COLOR);
        Ansi.SetBgColor(Data.INPUT_COLOR);
        Ansi.SetCursorPosition(left, top);
        
        if (_value.Length < calculatedWidth) {
            Console.Write(_value);
            Console.Write(new String(' ', calculatedWidth - _value.Length));
            Ansi.SetCursorPosition(left + index, top);
        }
        else {
            //TODO:
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
                index = Math.Max(Math.Min(spaceIndex + 1, _value.Length), 0);
                Ansi.SetCursorPosition(left + index, top);
            }
            else {
                index = Math.Max(index - 1, 0);
                index = Math.Min(index, _value.Length);
                Ansi.SetCursorPosition(left + index, top);
            }
            break;

        case ConsoleKey.RightArrow:
            if (key.Modifiers == ConsoleModifiers.Control) {
                int spaceIndex = _value.IndexOf(' ', Math.Min(index + 1, _value.Length));
                if (spaceIndex == -1) {
                    spaceIndex = _value.Length;
                }
                index = spaceIndex;
                Ansi.SetCursorPosition(left + index, top);
            }
            else {
                index = Math.Min(index + 1, _value.Length);
                index = Math.Max(index, 0);
                Ansi.SetCursorPosition(left + index, top);
            }
            break;

        case ConsoleKey.Home:
            index = 0;
            DrawValue();
            break;

        case ConsoleKey.End:
            index = _value.Length;
            DrawValue();
            break;

        case ConsoleKey.Backspace:
            if (index == 0) {
                Ansi.SetCursorPosition(left + index, top);
                break;
            }
            
            if (key.Modifiers == ConsoleModifiers.Control) {
                //TODO:
            }
            else {
                _value = _value[..(Math.Max(index - 1, 0))] + _value[index..];
                index--;
                DrawValue();
            }
            break;

        case ConsoleKey.Delete:
            if (key.Modifiers == ConsoleModifiers.Control) {
                //TODO:
            }
            else if (index < _value.Length) {
                _value = _value[..index] + _value[(index + 1)..];
                DrawValue();
            }
            break;

        default:
            byte asci = (byte)key.KeyChar;
            if (asci > 31 && asci < 127) {
                _value = _value[..index] + key.KeyChar + _value[index..];
                index++;
                DrawValue();
            }
            break;
        }
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