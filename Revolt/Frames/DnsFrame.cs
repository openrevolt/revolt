﻿using Revolt.Protocols;
using Revolt.Tui;

namespace Revolt.Frames;

public sealed class DnsFrame : Tui.Frame {
    public struct DnsItem {
        public string     questionString;
        public int        questionType;
        public Ansi.Color questionColor;
        public string     answerString;
        public int        answerType;
        public Ansi.Color answerColor;
        public int        ttl;
    }

    public static readonly DnsFrame instance = new DnsFrame();

    public Tui.ListBox<DnsItem> list;
    public Tui.Toolbar toolbar;

    private readonly List<string> queryHistory     = [];
    private Dns.RecordType        type             = Dns.RecordType.A;
    private Dns.TransportMethod   transport        = Dns.TransportMethod.Auto;
    private string                server           = null;
    private int                   timeout          = 2000;
    private bool                  isStandard       = false;
    private bool                  isInverse        = false;
    private bool                  showServerStatus = false;
    private bool                  isTruncated      = false;
    private bool                  isRecursive      = true;

    private int lastStatusLength = 0;

    public DnsFrame() {
        list = new Tui.ListBox<DnsItem>(this) {
            left            = 1,
            right           = 1,
            top             = 1,
            bottom          = 2,
            drawItemHandler = DrawDnsItem
        };

        toolbar = new Tui.Toolbar(this) {
            left  = 0,
            right = 0,
            items = [
                new Tui.Toolbar.ToolbarItem() { text="Add",     key="INS", action=AddDialog},
                new Tui.Toolbar.ToolbarItem() { text="Remove",  key="DEL", action=RemoveSelected},
                new Tui.Toolbar.ToolbarItem() { text="Options", key="F4",  action=OptionsDialog },
                new Tui.Toolbar.ToolbarItem() { text="Clear",   key="F6",  action=Clear },
            ],
            drawStatus = DrawStatus
        };

        elements.Add(list);
        elements.Add(toolbar);

        defaultElement = list;
        FocusNext();
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
            MainMenu.instance.Show();
            break;

        case ConsoleKey.Insert:
            AddDialog();
            break;

        case ConsoleKey.Delete:
            RemoveSelected();
            break;

        case ConsoleKey.F4:
            OptionsDialog();
            break;

        case ConsoleKey.F6:
            Clear();
            break;

        default:
            focusedElement.HandleKey(key);
            break;
        }

        return true;
    }

    public override string[][] GetKeyShortcuts() {
        List<string[]> list = new List<string[]>();

        list.Add(["F1", "Help"]);
        list.Add(["F5", "Refresh"]);
        list.Add([String.Empty, String.Empty]);

        for (int i = 0; i < toolbar.items.Length; i++) {
            list.Add([toolbar.items[i].key, toolbar.items[i].text]);
        }

        return list.ToArray();
    }

    private void DrawDnsItem(int index, int x, int y, int width) {
        if (list.items is null || list.items.Count == 0) return;
        if (index >= list.items.Count) return;

        int adjustedY = y + index - list.scrollOffset * list.itemHeight;
        if (adjustedY < y || adjustedY > Renderer.LastHeight) return;

        int questionWidth = Math.Min(width / 3, 40);
        int ttlWidth      = 10;
        int answerWidth   = Math.Max(width - questionWidth - ttlWidth, 0);

        Ansi.SetCursorPosition(2, adjustedY);

        DnsItem item = list.items[index];
        bool isSelected = index == list.index;
        Ansi.Color foreColor, backColor;

        string questionTypeString = Dns.typeStrings[item.questionType];

        if (isSelected) {
            foreColor = list.isFocused ? Glyphs.DARKGRAY_COLOR : Glyphs.LIGHT_COLOR;
            backColor = list.isFocused ? Glyphs.FOCUS_COLOR : Glyphs.HIGHLIGHT_COLOR;
        }
        else {
            foreColor = Glyphs.LIGHT_COLOR;
            backColor = Glyphs.DARK_COLOR;
        }

        Ansi.SetBgColor(backColor);
        Ansi.SetFgColor(foreColor);
        Ansi.Write(new String(' ', Math.Max(6 - questionTypeString.Length, 0)));

        Ansi.SetFgColor(Glyphs.DARKGRAY_COLOR);
        Ansi.SetBgColor(item.questionColor);
        Ansi.Write($" {questionTypeString} ");


        Ansi.SetBgColor(backColor);
        Ansi.Write(' ');

        int adjustedQuestionWidth = Math.Max(questionWidth - 9, 0);
        int adjustedAnswerWidth = Math.Max(answerWidth - 10, 1);

        Ansi.SetFgColor(foreColor);
        Ansi.Write(item.questionString.Length > adjustedQuestionWidth ? item.questionString[..(adjustedQuestionWidth-1)] + Glyphs.ELLIPSIS : item.questionString.PadRight(adjustedQuestionWidth));

        Ansi.SetBgColor(isSelected ? Glyphs.HIGHLIGHT_COLOR : Glyphs.DARK_COLOR);
        Ansi.Write(' ');

        if (item.answerType >= 0) {
            string answerTypeString = Dns.typeStrings[item.answerType];

            Ansi.Write(new String(' ', Math.Max(6 - answerTypeString.Length, 0)));

            Ansi.SetFgColor(Glyphs.DARKGRAY_COLOR);
            Ansi.SetBgColor(item.answerColor);
            Ansi.Write($" {answerTypeString} ");

            Ansi.SetBgColor(isSelected ? Glyphs.HIGHLIGHT_COLOR : Glyphs.DARK_COLOR);
            Ansi.Write(' ');
        }
        else {
            Ansi.Write(new String(' ', 12));
        }

        Ansi.SetCursorPosition(questionWidth + 12, adjustedY);
        Ansi.SetFgColor(item.answerType < 0 ? new Ansi.Color(176, 0, 0) : Glyphs.LIGHT_COLOR);
        Ansi.Write(item.answerString.Length > adjustedAnswerWidth ? item.answerString[..(adjustedAnswerWidth - 1)] + Glyphs.ELLIPSIS : item.answerString.PadRight(adjustedAnswerWidth));

        if (item.answerType >= 0) {
            string ttlString = item.ttl.ToString();
            Ansi.Write($"{ttlString}s");
            Ansi.Write(new String(' ', ttlWidth - ttlString.Length - 1));
        }
        else {
            Ansi.Write(new String(' ', 10));
        }

        Ansi.SetBgColor(Glyphs.DARK_COLOR);
    }

    private void DrawStatus() {
        int total = list.items.Count;
        string totalString = $" {total} ";
        int statusLength = totalString.Length;

        if (statusLength != lastStatusLength) {
            Ansi.SetCursorPosition(Renderer.LastWidth - lastStatusLength, Math.Max(Renderer.LastHeight, 0));
            Ansi.SetBgColor(Glyphs.TOOLBAR_COLOR);
            Ansi.Write(new String(' ', lastStatusLength));
        }

        Ansi.SetCursorPosition(Renderer.LastWidth - totalString.Length + 1, Math.Max(Renderer.LastHeight, 0));
        Ansi.SetFgColor(Glyphs.DARKGRAY_COLOR);
        Ansi.SetBgColor(Glyphs.LIGHT_COLOR);
        Ansi.Write(totalString);
        Ansi.SetBgColor(Glyphs.DARK_COLOR);
    }

    private void AddItem(string question, Dns.RecordType questionType, Dns.Answer answer) {
        DnsItem item;

        if (answer.error > 0) {
            string errorMessage = Dns.errorMessages.TryGetValue(answer.error, out string err) ? err : "unknown error";
            int questionTypeIndex = Array.IndexOf(Dns.types, questionType);

            item = new DnsItem() {
                questionString = question,
                questionType   = questionTypeIndex,
                questionColor  = Dns.typesColors[questionTypeIndex],
                answerString   = errorMessage,
                answerType     = -1,
                answerColor    = Glyphs.WHITE_COLOR,
                ttl            = 0,
            };
        }
        else if (String.IsNullOrEmpty(answer.answerString)) {
            string errorMessage = "unknown error";
            int questionTypeIndex = Array.IndexOf(Dns.types, questionType);

            item = new DnsItem() {
                questionString = question,
                questionType   = questionTypeIndex,
                questionColor  = Dns.typesColors[questionTypeIndex],
                answerString   = errorMessage,
                answerType     = -1,
                answerColor    = Glyphs.WHITE_COLOR,
                ttl            = 0,
            };
        }
        else {
            int questionTypeIndex = Array.IndexOf(Dns.types, questionType);
            int answerTypeIndex = Array.IndexOf(Dns.types, type);

            item = new DnsItem() {
                questionString = question,
                questionType   = questionTypeIndex,
                questionColor  = Dns.typesColors[questionTypeIndex],
                answerString   = answer.answerString,
                answerType     = answerTypeIndex,
                answerColor    = Dns.typesColors[answerTypeIndex],
                ttl            = answer.ttl,
            };
        }

        list.Add(item);
        list.index = list.items.Count - 1;

        //(int left, int top, int width, _) = list.GetBounding();
        //list.drawItemHandler(list.items.Count - 1, left, top, width);
    }

    private void AddDialog() {
        AddDialog dialog = new AddDialog();

        dialog.nameTextbox.enableHistory = true;
        dialog.nameTextbox.history = queryHistory;

        dialog.okButton.action = () => {
            type = Dns.types[dialog.typeSelectBox.index];

            string question = dialog.nameTextbox.Value.Trim();
            if (String.IsNullOrEmpty(question)) return;

            if (type == Dns.RecordType.PTR) {
                string[] labels = question.Split('.');
                if (labels.Length == 4 && labels.All(o => int.TryParse(o, out int n) && n >= 0 && n <= 255)) {
                    question = $"{String.Join(".", labels.Reverse())}.in-addr.arpa";
                }
            }

            queryHistory.Add(question);

            Dns.Answer[] answers = Dns.Resolve(question, server, type, timeout, transport, isStandard, isInverse, showServerStatus, isTruncated, isRecursive);

            for (int i = 0; i < answers.Length; i++) {
                AddItem(question, type, answers[i]);
            }

            dialog.Close();
        };

        dialog.typeSelectBox.index = Array.IndexOf(Dns.types, type);



        dialog.Show();
    }

    private void RemoveSelected() {
        list.RemoveSelected();
        DrawStatus();
        Ansi.Push();
    }

    private void Clear() {
        Tui.ConfirmDialog dialog = new Tui.ConfirmDialog() {
            text = "Are you sure you want to clear?"
        };

        dialog.okButton.action = () => {
            list.Clear();
            DrawStatus();
            Ansi.Push();
            dialog.Close();
        };

        dialog.Show();
    }

    private void OptionsDialog() {
        OptionsDialog dialog = new OptionsDialog();

        dialog.okButton.action = () => {
            _ = int.TryParse(dialog.timeoutTextbox.Value, out timeout);

            server           = dialog.serverTextbox.Value;
            timeout          = Math.Clamp(timeout, 50, 10_000);
            transport        = (Dns.TransportMethod)dialog.transportSelectBox.index;
            isStandard       = dialog.standardToggle.Value;
            isInverse        = dialog.inverseLookupToggle.Value;
            showServerStatus = dialog.serverStatusToggle.Value;
            isTruncated      = dialog.truncatedToggle.Value;
            isRecursive      = dialog.recursiveToggle.Value;

            dialog.Close();
        };

        dialog.Show();

        dialog.transportSelectBox.index = (int)transport;

        dialog.timeoutTextbox.Value      = timeout.ToString();
        dialog.standardToggle.Value      = isStandard;
        dialog.inverseLookupToggle.Value = isInverse;
        dialog.serverStatusToggle.Value  = showServerStatus;
        dialog.truncatedToggle.Value     = isTruncated;
        dialog.recursiveToggle.Value     = isRecursive;

        dialog.serverTextbox.Value = server ?? String.Empty;
    }
}

file sealed class AddDialog : Tui.DialogBox {
    public Tui.Textbox nameTextbox;
    public Tui.SelectBox typeSelectBox;

    public AddDialog() {
        nameTextbox = new Tui.Textbox(this) {
            backColor = Glyphs.DIALOG_COLOR,
        };

        typeSelectBox = new Tui.SelectBox(this) {
            options = Dns.typeFullNames
        };

        elements.Add(nameTextbox);
        elements.Add(typeSelectBox);

        defaultElement = nameTextbox;
    }

    public override void Draw(int width, int height) {
        int left = (Renderer.LastWidth - width) / 2 + 1;
        int top = 1;

        string blank = new String(' ', width);

        Ansi.SetFgColor(Glyphs.DARKGRAY_COLOR);
        Ansi.SetBgColor(Glyphs.DIALOG_COLOR);

        Ansi.SetCursorPosition(left, top);
        Ansi.Write(blank);

        WriteLabel("Name:", left, ++top, width);
        nameTextbox.left = left;
        nameTextbox.right = Renderer.LastWidth - width - left + 2;
        nameTextbox.top = top++;

        Ansi.SetCursorPosition(left, top++);
        Ansi.Write(blank);
        Ansi.SetCursorPosition(left, top);
        Ansi.Write(blank);

        WriteLabel("Record type:", left, ++top, width);
        typeSelectBox.left = left;
        typeSelectBox.right = Renderer.LastWidth - width - left + 10;
        typeSelectBox.top = top++;

        Ansi.SetCursorPosition(left, top++);
        Ansi.Write(blank);
        Ansi.SetCursorPosition(left, top);
        Ansi.Write(blank);

        for (int i = 0; i < 3; i++) {
            Ansi.SetCursorPosition(left, top + i);
            Ansi.Write(blank);
        }

        okButton.left = left + (width - 20) / 2;
        okButton.top = top;

        cancelButton.left = left + (width - 20) / 2 + 10;
        cancelButton.top = top;

        typeSelectBox.afterChange = () => {
            Ansi.Color typeColor = Dns.typesColors[typeSelectBox.index];

            string text = Dns.typeStrings[typeSelectBox.index];
            string padding = new String(' ', (7 - text.Length) / 2);

            Ansi.SetCursorPosition(left + width - 8, 6);

            Ansi.SetFgColor(Glyphs.DARKGRAY_COLOR);
            Ansi.SetBgColor(typeColor);
            Ansi.Write(padding);
            Ansi.Write(text);
            Ansi.Write(padding);

            Ansi.SetBgColor(Glyphs.DIALOG_COLOR);
            if (text.Length % 2 == 0) {
                Ansi.Write(' ');
            }

            Ansi.Push();
        };

        typeSelectBox.afterChange();

        if (elements is null) return;

        for (int i = 0; i < elements?.Count; i++) {
            elements[i].Draw(false);
        }

        if (focusedElement == nameTextbox) {
            nameTextbox.Focus();
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
            Close();
            return true;

        case ConsoleKey.Enter:
            if (focusedElement == nameTextbox
                || focusedElement == typeSelectBox) {
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

file sealed class OptionsDialog : Tui.DialogBox {
    public Tui.Textbox serverTextbox;
    public Tui.IntegerBox timeoutTextbox;
    public Tui.SelectBox transportSelectBox;

    public Tui.Toggle standardToggle;
    public Tui.Toggle inverseLookupToggle;
    public Tui.Toggle serverStatusToggle;
    public Tui.Toggle truncatedToggle;
    public Tui.Toggle recursiveToggle;

    public OptionsDialog() {
        serverTextbox = new Tui.Textbox(this) {
            backColor = Glyphs.DIALOG_COLOR,
            placeholder = "System default"
        };

        timeoutTextbox = new Tui.IntegerBox(this) {
            backColor = Glyphs.DIALOG_COLOR,
            min = 50,
            max = 5_000
        };

        transportSelectBox = new Tui.SelectBox(this) {
            options = ["Auto", "UDP", "TCP", "TCP over TLS", "HTTPS"] //TODO: "QUIC"
        };

        standardToggle      = new Tui.Toggle(this, "Standard");
        inverseLookupToggle = new Tui.Toggle(this, "Inverse lookup");
        serverStatusToggle  = new Tui.Toggle(this, "Request server status");
        truncatedToggle     = new Tui.Toggle(this, "Truncated");
        recursiveToggle     = new Tui.Toggle(this, "Recursive");

        elements.Add(serverTextbox);
        elements.Add(timeoutTextbox);
        elements.Add(transportSelectBox);
        elements.Add(standardToggle);
        elements.Add(inverseLookupToggle);
        elements.Add(serverStatusToggle);
        elements.Add(truncatedToggle);
        elements.Add(recursiveToggle);

        defaultElement = serverTextbox;
    }

    public override void Draw(int width, int height) {
        int left = (Renderer.LastWidth - width) / 2 + 1;
        int top = 1;

        string blank = new String(' ', width);

        Ansi.SetFgColor(Glyphs.DARKGRAY_COLOR);
        Ansi.SetBgColor(Glyphs.DIALOG_COLOR);

        Ansi.SetCursorPosition(left, top);
        Ansi.Write(blank);

        WriteLabel("Server:", left, ++top, width);
        serverTextbox.left = left + 16;
        serverTextbox.right = Renderer.LastWidth - width - left + 2;
        serverTextbox.top = top++ - 1;

        Ansi.SetCursorPosition(left, top);
        Ansi.Write(blank);

        WriteLabel("Timed out (ms):", left, ++top, width);
        timeoutTextbox.left = left + 16;
        timeoutTextbox.right = Renderer.LastWidth - width - left + 2;
        timeoutTextbox.top = top++ - 1;

        Ansi.SetCursorPosition(left, top);
        Ansi.Write(blank);

        WriteLabel("Transp. method:", left, ++top, width);
        transportSelectBox.left = left + 16;
        transportSelectBox.right = Renderer.LastWidth - width - left + 2;
        transportSelectBox.top = top++ - 1;

        Ansi.SetCursorPosition(left, top++);
        Ansi.Write(blank);

        Ansi.SetCursorPosition(left, top);
        Ansi.Write(blank);

        standardToggle.left = left;
        standardToggle.top = top - 1;

        Ansi.SetCursorPosition(left, top++);
        Ansi.Write(blank);

        inverseLookupToggle.left = left;
        inverseLookupToggle.top = top - 1;

        Ansi.SetCursorPosition(left, top++);
        Ansi.Write(blank);

        serverStatusToggle.left = left;
        serverStatusToggle.top = top - 1;

        Ansi.SetCursorPosition(left, top++);
        Ansi.Write(blank);

        truncatedToggle.left = left;
        truncatedToggle.top = top - 1;

        Ansi.SetCursorPosition(left, top++);
        Ansi.Write(blank);

        recursiveToggle.left = left;
        recursiveToggle.top = top - 1;

        Ansi.SetCursorPosition(left, top++);
        Ansi.Write(blank);

        for (int i = 0; i < 3; i++) {
            Ansi.SetCursorPosition(left, top + i);
            Ansi.Write(blank);
        }

        okButton.left = left + (width - 20) / 2;
        okButton.top = top;

        cancelButton.left = left + (width - 20) / 2 + 10;
        cancelButton.top = top;

        if (elements is null) return;

        for (int i = 0; i < elements?.Count; i++) {
            elements[i].Draw(false);
        }

        if (focusedElement == serverTextbox) {
            serverTextbox.Focus();
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
            Close();
            return true;

        case ConsoleKey.Enter:
            if (focusedElement == serverTextbox
                || focusedElement == timeoutTextbox
                || focusedElement == transportSelectBox) {
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