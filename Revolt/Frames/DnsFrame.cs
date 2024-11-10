using Revolt.Protocols;

namespace Revolt.Frames;

public sealed class DnsFrame : Ui.Frame {
    public struct DnsItem {
        public string questionString;
        public int    questionType;
        public byte[] questionColor;
        public string answerString;
        public int    answerType;
        public byte[] answerColor;
        public int    ttl;
    }

    public static DnsFrame Instance { get; } = new DnsFrame();

    public Ui.Toolbar toolbar;
    public Ui.ListBox<DnsItem> list;

    private readonly List<string> queryHistory = [];
    private Dns.RecordType      type             = Dns.RecordType.A;
    private Dns.TransportMethod transport        = Dns.TransportMethod.Auto;
    private string              server           = null;
    private int                 timeout          = 2000;
    private bool                isStandard       = false;
    private bool                isInverse        = false;
    private bool                showServerStatus = false;
    private bool                isTruncated      = false;
    private bool                isRecursive      = true;

    public DnsFrame() {
        toolbar = new Ui.Toolbar(this) {
            left  = 1,
            right = 1,
            items = [
                new Ui.Toolbar.ToolbarItem() { text="Add",     action=AddDialog},
                new Ui.Toolbar.ToolbarItem() { text="Clear",   action=Clear },
                new Ui.Toolbar.ToolbarItem() { text="Options", action=OptionsDialog },
            ]
        };

        list = new Ui.ListBox<DnsItem>(this) {
            left              = 1,
            right             = 1,
            top               = 3,
            bottom            = 1,
            drawItemHandler   = DrawDnsItem
        };

        elements.Add(toolbar);
        elements.Add(list);

        defaultElement = toolbar;
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
            MainMenu.Instance.Show();
            break;

        case ConsoleKey.Insert:
            AddDialog();
            break;

        case ConsoleKey.Delete:
            list.RemoveSelected();
            break;

        default:
            focusedElement.HandleKey(key);
            break;
        }

        return true;
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
        byte[] foreColor, backColor;

        string questionTypeString = Dns.typeStrings[item.questionType];

        if (isSelected) {
            foreColor = list.isFocused ? [16, 16, 16] : Data.FG_COLOR;
            backColor = list.isFocused ? Data.SELECT_COLOR : Data.SELECT_COLOR_LIGHT;
        }
        else {
            foreColor = Data.FG_COLOR;
            backColor = Data.BG_COLOR;
        }

        Ansi.SetBgColor(backColor);
        Ansi.SetFgColor(foreColor);
        Ansi.Write(new String(' ', Math.Max(6 - questionTypeString.Length, 0)));

        Ansi.SetFgColor(item.questionColor);
        Ansi.SetBgColor(backColor);
        Ansi.Write(Data.LEFT_HALF_CIRCLE);

        Ansi.SetFgColor([16, 16, 16]);
        Ansi.SetBgColor(item.questionColor);
        Ansi.Write(questionTypeString);

        Ansi.SetFgColor(item.questionColor);
        Ansi.SetBgColor(backColor);
        Ansi.Write(Data.RIGHT_HALF_CIRCLE);

        Ansi.Write(' ');

        int adjustedQuestionWidth = Math.Max(questionWidth - 9, 0);
        int adjustedAnswerWidth = Math.Max(answerWidth - 10, 0);

        Ansi.SetFgColor(foreColor);
        Ansi.Write(item.questionString.Length > adjustedQuestionWidth ? item.questionString[..(adjustedQuestionWidth-1)] + Data.ELLIPSIS : item.questionString.PadRight(adjustedQuestionWidth));

        Ansi.SetFgColor(backColor);
        Ansi.SetBgColor(isSelected ? Data.SELECT_COLOR_LIGHT : Data.BG_COLOR);
        Ansi.Write(isSelected ? Data.BIG_RIGHT_TRIANGLE : ' ');

        if (item.answerType >= 0) {
            string answerTypeString = Dns.typeStrings[item.answerType];

            Ansi.Write(new String(' ', Math.Max(6 - answerTypeString.Length, 0)));

            Ansi.SetFgColor(item.answerColor);
            Ansi.SetBgColor(isSelected ? Data.SELECT_COLOR_LIGHT : Data.BG_COLOR);
            Ansi.Write(Data.LEFT_HALF_CIRCLE);

            Ansi.SetFgColor([16, 16, 16]);
            Ansi.SetBgColor(item.answerColor);
            Ansi.Write(answerTypeString);

            Ansi.SetFgColor(item.answerColor);
            Ansi.SetBgColor(isSelected ? Data.SELECT_COLOR_LIGHT : Data.BG_COLOR);
            Ansi.Write(Data.RIGHT_HALF_CIRCLE);
            Ansi.Write(' ');
        }
        else {
            Ansi.Write(new String(' ', 12));
        }

        Ansi.SetCursorPosition(questionWidth + 12, adjustedY);
        Ansi.SetFgColor(item.answerType < 0 ? [176, 0, 0] : Data.FG_COLOR);
        Ansi.Write(item.answerString.Length > adjustedAnswerWidth ? item.answerString[..(adjustedAnswerWidth-1)] + Data.ELLIPSIS : item.answerString.PadRight(adjustedAnswerWidth));
        
        if (item.answerType >= 0) {
            string ttlString = item.ttl.ToString();
            Ansi.Write(ttlString);
            Ansi.Write(new String(' ', ttlWidth - ttlString.Length));
        }
        else {
            Ansi.Write(new String(' ', 10));
        }

        Ansi.SetBgColor(Data.BG_COLOR);
    }

    private void AddItem(string question, Dns.RecordType questionType, Dns.Answer answer) {
        DnsItem item;

        if (answer.error > 0) {
            string errorMessage = Dns.errorMessages.TryGetValue(answer.error, out var err) ? err : "unknown error";
            int questionTypeIndex = Array.IndexOf(Dns.types, questionType);

            item = new DnsItem() {
                questionString = question,
                questionType  = questionTypeIndex,
                questionColor = Dns.typesColors[questionTypeIndex],
                answerString  = errorMessage,
                answerType    = -1,
                answerColor   = [255, 255, 255],
                ttl           = 0,
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

        (int left, int top, int width, _) = list.GetBounding();
        list.drawItemHandler(list.items.Count - 1, left, top, width);
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

        Renderer.ActiveDialog = dialog;
        dialog.typeSelectBox.index = Array.IndexOf(Dns.types, type);
        
        dialog.Draw();
        
        dialog.nameTextbox.Focus();
    }

    private void Clear() {
        list.Clear();
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

        Renderer.ActiveDialog = dialog;


        dialog.transportSelectBox.index = (int)transport;

        dialog.Draw();

        dialog.serverTextbox.Value       = server ?? String.Empty;
        dialog.timeoutTextbox.Value      = timeout.ToString();
        dialog.standardToggle.Value      = isStandard;
        dialog.inverseLookupToggle.Value = isInverse;
        dialog.serverStatusToggle.Value  = showServerStatus;
        dialog.truncatedToggle.Value     = isTruncated;
        dialog.recursiveToggle.Value     = isRecursive;

        dialog.serverTextbox.Focus();
    }
}

file sealed class AddDialog : Ui.DialogBox {
    public Ui.Textbox nameTextbox;
    public Ui.SelectBox typeSelectBox;

    public AddDialog() {
        nameTextbox = new Ui.Textbox(this) {
            backColor = Data.PANE_COLOR,
        };

        typeSelectBox = new Ui.SelectBox(this) {
            options = Dns.typeFullNames
        };

        elements.Add(nameTextbox);
        elements.Add(typeSelectBox);

        defaultElement = nameTextbox;
        nameTextbox.Focus(false);
        focusedElement = nameTextbox;
    }

    public override void Draw(int width, int height) {
        int left = (Renderer.LastWidth - width) / 2 + 1;
        int top = 1;

        string blank = new String(' ', width);

        Ansi.SetFgColor([16, 16, 16]);
        Ansi.SetBgColor(Data.PANE_COLOR);

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
            byte[] typeColor = Dns.typesColors[typeSelectBox.index];

            string text = Dns.typeStrings[typeSelectBox.index];
            string padding = new String(' ', (5 - text.Length) / 2);

            Ansi.SetCursorPosition(left + width - 8, 6);

            Ansi.SetFgColor(typeColor);
            Ansi.SetBgColor(Data.PANE_COLOR);
            Ansi.Write(Data.LEFT_HALF_CIRCLE);

            Ansi.SetFgColor([16, 16, 16]);
            Ansi.SetBgColor(typeColor);
            Ansi.Write(padding);
            Ansi.Write(text);
            Ansi.Write(padding);

            Ansi.SetFgColor(typeColor);
            Ansi.SetBgColor(Data.PANE_COLOR);
            Ansi.Write(Data.RIGHT_HALF_CIRCLE);

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

file sealed class OptionsDialog : Ui.DialogBox {
    public Ui.Textbox serverTextbox;
    public Ui.IntegerBox timeoutTextbox;
    public Ui.SelectBox transportSelectBox;

    public Ui.Toggle standardToggle;
    public Ui.Toggle inverseLookupToggle;
    public Ui.Toggle serverStatusToggle;
    public Ui.Toggle truncatedToggle;
    public Ui.Toggle recursiveToggle;

    public OptionsDialog() {
        serverTextbox = new Ui.Textbox(this) {
            backColor = Data.PANE_COLOR,
            placeholder = "System default"
        };

        timeoutTextbox = new Ui.IntegerBox(this) {
            backColor = Data.PANE_COLOR,
            min = 50,
            max = 5_000
        };

        transportSelectBox = new Ui.SelectBox(this) {
            options = ["Auto", "UDP", "TCP", "TCP over TLS", "HTTPS"],
        };

        standardToggle      = new Ui.Toggle(this, "Standard");
        inverseLookupToggle = new Ui.Toggle(this, "Inverse lookup");
        serverStatusToggle  = new Ui.Toggle(this, "Request server status");
        truncatedToggle     = new Ui.Toggle(this, "Truncated");
        recursiveToggle     = new Ui.Toggle(this, "Recursive");

        elements.Add(serverTextbox);
        elements.Add(timeoutTextbox);
        elements.Add(transportSelectBox);
        elements.Add(standardToggle);
        elements.Add(inverseLookupToggle);
        elements.Add(serverStatusToggle);
        elements.Add(truncatedToggle);
        elements.Add(recursiveToggle);

        defaultElement = serverTextbox;
        serverTextbox.Focus(false);
        focusedElement = serverTextbox;
    }

    public override void Draw(int width, int height) {
        int left = (Renderer.LastWidth - width) / 2 + 1;
        int top = 1;

        string blank = new String(' ', width);

        Ansi.SetFgColor([16, 16, 16]);
        Ansi.SetBgColor(Data.PANE_COLOR);

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