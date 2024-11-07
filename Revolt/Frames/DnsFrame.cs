namespace Revolt.Frames;

public sealed class DnsFrame : Ui.Frame {

    public struct DnsItem {
        //string name;
    }

    public static DnsFrame Instance { get; } = new DnsFrame();

    public Ui.Toolbar toolbar;
    public Ui.ListBox<DnsItem> list;

    private readonly List<string> queryHistory = [];
    private string   server   = null;
    private int      timeout  = 2000;
    private bool     standard      = false;
    private bool     inverseLookup = false;
    private bool     serverStatus  = false;
    private bool     truncated     = false;
    private bool     recursive     = true;

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
            itemHeight        = 2,
            drawItemHandler   = DrawDnsItem,
            //drawStatusHandler = DrawStatus
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

        int adjustedY = y + index * 2 - list.scrollOffset * list.itemHeight;
        if (adjustedY < y || adjustedY > Renderer.LastHeight) return;

        DnsItem item = list.items[index];

        //TODO:
    }
    
    private void AddItem(string host) {
        if (String.IsNullOrEmpty(host)) return;

    }

    private void AddDialog() {
        Ui.InputDialog dialog = new Ui.InputDialog() {
            text = "Enter name:",
        };

        dialog.valueTextbox.enableHistory = true;
        dialog.valueTextbox.history = queryHistory;

        dialog.okButton.action = () => {
            if (!String.IsNullOrWhiteSpace(dialog.valueTextbox.Value)) {
                queryHistory.Add(dialog.valueTextbox.Value.Trim());
            }
            AddItem(dialog.valueTextbox.Value.Trim());
            dialog.Close();
        };

        Renderer.ActiveDialog = dialog;
        dialog.Draw();
    }

    private void Clear() {
        list.Clear();
    }

    private void OptionsDialog() {
        OptionsDialog dialog = new OptionsDialog();

        dialog.okButton.action = () => {
            _ = int.TryParse(dialog.timeoutTextbox.Value, out timeout);

            server        = dialog.serverTextbox.Value;
            timeout       = Math.Clamp(timeout, 50, 10_000);
            standard      = dialog.standardToggle.Value;
            inverseLookup = dialog.inverseLookupToggle.Value;
            serverStatus  = dialog.serverStatusToggle.Value;
            truncated     = dialog.truncatedToggle.Value;
            recursive     = dialog.recursiveToggle.Value;

            dialog.Close();
        };

        Renderer.ActiveDialog = dialog;
        dialog.Draw();

        dialog.serverTextbox.Value       = server ?? String.Empty;
        dialog.timeoutTextbox.Value      = timeout.ToString();
        dialog.standardToggle.Value      = standard;
        dialog.inverseLookupToggle.Value = inverseLookup;
        dialog.serverStatusToggle.Value  = serverStatus;
        dialog.truncatedToggle.Value     = truncated;
        dialog.recursiveToggle.Value     = recursive;

        dialog.serverTextbox.Focus();
    }
}

file sealed class OptionsDialog : Ui.DialogBox {
    public Ui.Textbox serverTextbox;
    public Ui.IntegerBox timeoutTextbox;
    public Ui.SelectBox typeSelectBox;
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

        typeSelectBox = new Ui.SelectBox(this) {
            options = [
                "A - IPv4 Address",
                "AAAA - IPv6 Address",
                "NS - Name Server",
                "CNAME - Canonical Name",
                "SOA - Start Of Authority", 
                "PTR - Pointer",
                "MX - Mail Exchange",
                "TXT - Text",
                "SRV - Service",
                "ANY - All types known",
            ],
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
        elements.Add(typeSelectBox);
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

        WriteLabel("Record type:", left, ++top, width);
        typeSelectBox.left = left + 16;
        typeSelectBox.right = Renderer.LastWidth - width - left + 2;
        typeSelectBox.top = top++ - 1;

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
                || focusedElement == typeSelectBox
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