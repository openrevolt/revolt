using System.Net;
using Revolt.Protocols;

namespace Revolt.Frames;

public sealed class NetMapperFrame : Tui.Frame {
    public struct DiscoverItem {
        public int    status;
        public string name;
        public string ip;
        public string mac;
        public string manufacturer;
        public string other;
    }

    public static NetMapperFrame Instance { get; } = new NetMapperFrame();

    public Tui.ListBox<DiscoverItem> list;
    public Tui.Toolbar toolbar;

    private CancellationTokenSource cancellationTokenSource;
    private CancellationToken cancellationToken;

    private bool icmp     = false;
    private bool mdns     = false;
    private bool ubiquiti = true;

    private (IPAddress, IPAddress, IPAddress) networkRange = (IPAddress.Loopback, IPAddress.Broadcast, IPAddress.IPv6None);

    public NetMapperFrame() {
        list = new Tui.ListBox<DiscoverItem>(this) {
            left              = 1,
            right             = 1,
            top               = 1,
            bottom            = 3,
            itemHeight        = 1,
            drawItemHandler   = DrawDiscoverItem,
            drawStatusHandler = DrawStatus
        }; 
        
        toolbar = new Tui.Toolbar(this) {
            left  = 0,
            right = 0,
            items = [
                new Tui.Toolbar.ToolbarItem() { text="Discover", key="F2", action=Start },
                new Tui.Toolbar.ToolbarItem() { text="Clear",    key="F3", action=Clear },
            ]
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
            MainMenu.Instance.Show();
            break;

        case ConsoleKey.Delete:
            list.RemoveSelected();
            break;

        case ConsoleKey.F2:
            Start();
            break;

        case ConsoleKey.F3:
            Clear();
            break;

        default:
            focusedElement.HandleKey(key);
            break;
        }

        return true;
    }

    private void DrawDiscoverItem(int index, int x, int y, int width) {
        if (list.items is null || list.items.Count == 0) return;
        if (index >= list.items.Count) return;

        int adjustedY = y + index - list.scrollOffset * list.itemHeight;
        if (adjustedY < y || adjustedY > Renderer.LastHeight) return;

        int nameWidth         = Math.Min(width / 4, 40);
        int ipWidth           = 18;
        int macWidth          = 20;
        int manufacturerWidth = Math.Min(width / 4, 40);
        int otherWidth        = width - nameWidth - ipWidth - macWidth - manufacturerWidth;

        Ansi.SetCursorPosition(2, adjustedY);

        DiscoverItem item = list.items[index];
        bool isSelected = index == list.index;
        byte[] foreColor, backColor;

        if (isSelected) {
            foreColor = list.isFocused ? [16, 16, 16] : Data.LIGHT_COLOR;
            backColor = list.isFocused ? Data.SELECT_COLOR : Data.SELECT_COLOR_LIGHT;
        }
        else {
            foreColor = Data.LIGHT_COLOR;
            backColor = Data.DARK_COLOR;
        }

        Ansi.SetBgColor(backColor);
        Ansi.SetFgColor(foreColor);
        
        if (String.IsNullOrEmpty(item.name)) {
            Ansi.Write(new string(' ', nameWidth));
        }
        else {
            Ansi.Write(item.name.Length > nameWidth ? item.name[..(nameWidth - 1)] + Data.ELLIPSIS : item.name.PadRight(nameWidth));
        }

        if (String.IsNullOrEmpty(item.ip)) {
            Ansi.Write(new String(' ', ipWidth));
        }
        else {
            Ansi.Write(item.ip);
            Ansi.Write(new String(' ', Math.Max(ipWidth - item.ip.Length, 0)));
        }

        if (String.IsNullOrEmpty(item.mac)) {
            Ansi.Write(new String(' ', macWidth));
        }
        else {
            Ansi.Write(item.mac);
            Ansi.Write(new String(' ', Math.Max(macWidth - item.mac.Length, 0)));
        }

        if (String.IsNullOrEmpty(item.manufacturer)) {
            Ansi.Write(new string(' ', manufacturerWidth));
        }
        else if (manufacturerWidth > 0) {
            Ansi.Write(item.manufacturer.Length > manufacturerWidth ? item.manufacturer[..(manufacturerWidth - 1)] + Data.ELLIPSIS : item.manufacturer.PadRight(manufacturerWidth));
        }

        if (String.IsNullOrEmpty(item.other)) {
            Ansi.Write(new string(' ', otherWidth));
        }
        else if (otherWidth > 0) {
            Ansi.Write(item.other.Length > otherWidth ? item.other[..(otherWidth - 1)] + Data.ELLIPSIS : item.other.PadRight(otherWidth));
        }

        Ansi.SetBgColor(Data.DARK_COLOR);
    }

    private void DrawStatus() {
        int total = list.items.Count;
        string totalString = $" {total} ";

        Ansi.SetCursorPosition(Renderer.LastWidth - totalString.Length + 1, Math.Max(Renderer.LastHeight - 1, 0));

        Ansi.SetFgColor([16, 16, 16]);
        Ansi.SetBgColor(Data.LIGHT_COLOR);
        Ansi.Write(totalString);

        Ansi.SetBgColor(Data.DARK_COLOR);
    }

    private async Task Discover() {
        cancellationTokenSource = new CancellationTokenSource();
        cancellationToken = cancellationTokenSource.Token;

        Tokens.dictionary.TryAdd(cancellationTokenSource, cancellationToken);

        if (ubiquiti) {
            DiscoverUbiquiti(cancellationToken);
        }

        if (mdns) {
            DiscoverMdns();
        }

        if (icmp) {
            await DiscoverIcmp(cancellationToken);
        }

        Tokens.dictionary.TryRemove(cancellationTokenSource, out _);
        cancellationTokenSource.Dispose();

        list.Draw(true);
    }

    private async Task DiscoverIcmp(CancellationToken cancellationToken) {
        while (!cancellationToken.IsCancellationRequested) {
            if (Renderer.ActiveFrame == this && Renderer.ActiveDialog is null) {
                DrawStatus();
                Ansi.Push();
            }
            await Task.Delay(500, cancellationToken);

            if (Renderer.ActiveDialog is not null) continue;
            if (Renderer.ActiveFrame != this) continue;
        }
    }

    private void DiscoverMdns() {
        List<Mdns.Answer> answers = Mdns.Resolve(Mdns.anyDeviceQuery, 1000, Protocols.Dns.RecordType.ANY);
        for (int i = 0; i < answers.Count; i++) {
            list.Add(new DiscoverItem() {
                 ip = answers[i].remote.ToString(),
            });
        }

       list.Draw(true);
    }

    private void DiscoverUbiquiti(CancellationToken cancellationToken) {
        List<DiscoverItem> items = Proprietary.Ubiquiti.Discover(networkRange.Item1, cancellationToken);
        for (int i = 0; i < items.Count; i++) {
            if (list.items.FindIndex(o => o.mac == items[i].mac) > -1) continue;
            list.Add(items[i]);
        }

        list.Draw(true);
    }

    private void Start() {
        OptionsDialog dialog = new OptionsDialog();

        dialog.okButton.action = () => {
            icmp     = dialog.icmpToggle.Value;
            mdns     = dialog.mdnsToggle.Value;
            ubiquiti = dialog.ubiquitiToggle.Value;

            networkRange = dialog.networks[dialog.rangeSelectBox.index];

            dialog.Close();
            Task.Run(Discover);
        };

        dialog.Show(true);

        dialog.icmpToggle.Value     = icmp;
        dialog.mdnsToggle.Value     = mdns;
        dialog.ubiquitiToggle.Value = ubiquiti;
    }

    private void Stop() => cancellationTokenSource?.Cancel();

    private void Clear() {
        list.Clear();
    }
}

file sealed class OptionsDialog : Tui.DialogBox {
    public Tui.SelectBox rangeSelectBox;
    public Tui.Toggle    icmpToggle;
    public Tui.Toggle    mdnsToggle;
    public Tui.Toggle    ubiquitiToggle;

    public (IPAddress, IPAddress, IPAddress)[] networks;

    public OptionsDialog() {
        networks = NetTools.GetDirectNetworks();

        string[] networksString;
        if (networks.Length == 0) {
            networksString = [String.Empty];
        }
        else {
            networksString = networks.Select(o => $"{o.Item1}/{NetTools.SubnetMaskToCidr(o.Item2)}").ToArray(); ;
        }

        okButton.text = "  Start  ";

        rangeSelectBox = new Tui.SelectBox(this) {
            options = networksString
        };

        icmpToggle     = new Tui.Toggle(this, "ICMP");
        mdnsToggle     = new Tui.Toggle(this, "mDNS");
        ubiquitiToggle = new Tui.Toggle(this, "Ubiquiti discover");

        elements.Add(rangeSelectBox);

        elements.Add(icmpToggle);
        elements.Add(mdnsToggle);
        elements.Add(ubiquitiToggle);

        defaultElement = rangeSelectBox;
    }

    public override void Draw(int width, int height) {
        int left = (Renderer.LastWidth - width) / 2 + 1;
        int top = 1;

        string blank = new String(' ', width);

        Ansi.SetFgColor([16, 16, 16]);
        Ansi.SetBgColor(Data.PANE_COLOR);

        Ansi.SetCursorPosition(left, top);
        Ansi.Write(blank);

        WriteLabel("Range:", left, ++top, width);
        rangeSelectBox.left = left + 16;
        rangeSelectBox.right = Renderer.LastWidth - width - left + 2;
        rangeSelectBox.top = top++ - 1;

        Ansi.SetCursorPosition(left, top++);
        Ansi.Write(blank);

        icmpToggle.left = left;
        icmpToggle.top = top - 1;
        Ansi.SetCursorPosition(left, top++);
        Ansi.Write(blank);

        mdnsToggle.left = left;
        mdnsToggle.top = top - 1;
        Ansi.SetCursorPosition(left, top++);
        Ansi.Write(blank);

        ubiquitiToggle.left = left;
        ubiquitiToggle.top = top - 1;
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

        if (focusedElement == rangeSelectBox) {
            rangeSelectBox.Focus();
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
            if (focusedElement == rangeSelectBox) {
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