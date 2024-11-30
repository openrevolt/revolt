using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Revolt.Frames;

public sealed class IpDiscoveryFrame : Ui.Frame {
    public struct DiscoverItem {
        public int    status;
        public string name;
        public string ip;
        public string mac;
        public string manufacturer;
        public string other;
        public byte[] bytes;
    }

    public static IpDiscoveryFrame Instance { get; } = new IpDiscoveryFrame();

    public Ui.Toolbar toolbar;
    public Ui.ListBox<DiscoverItem> list;

    private CancellationTokenSource cancellationTokenSource;
    private CancellationToken cancellationToken;

    public bool icmp     = true;
    public bool mdns     = false;
    public bool ssdp     = false;
    public bool mikrotik = false;
    public bool ubiquiti = false;

    public IpDiscoveryFrame() {
        toolbar = new Ui.Toolbar(this) {
            left  = 1,
            right = 1,
            items = [
                new Ui.Toolbar.ToolbarItem() { text="Discover", action=Start },
                new Ui.Toolbar.ToolbarItem() { text="Clear",    action=Clear },
            ]
        };

        list = new Ui.ListBox<DiscoverItem>(this) {
            left              = 1,
            right             = 1,
            top               = 3,
            bottom            = 1,
            itemHeight        = 1,
            drawItemHandler   = DrawDiscoverItem,
            drawStatusHandler = DrawStatus
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

        case ConsoleKey.Delete:
            list.RemoveSelected();
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

        int nameWidth = Math.Min(width / 3, 40);
        int ipWidth = 18;
        int macWidth = 20;
        int otherWidth = width - nameWidth - ipWidth - macWidth;

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

        Ansi.Write(item.ip);
        Ansi.Write(new String(' ', Math.Max(ipWidth - item.ip.Length, 0)));

        Ansi.Write(item.mac);
        Ansi.Write(new String(' ', Math.Max(macWidth - item.mac.Length, 0)));


        if (String.IsNullOrEmpty(item.other)) {
            Ansi.Write(new string(' ', otherWidth));
        }
        else {
            Ansi.Write(item.other.Length > otherWidth ? item.other[..(otherWidth - 1)] + Data.ELLIPSIS : item.other.PadRight(otherWidth));
        }

        Ansi.SetBgColor(Data.DARK_COLOR);
    }

    private void DrawStatus() {
        int total = list.items.Count;

        Ansi.SetCursorPosition(2, Renderer.LastHeight);

        Ansi.SetFgColor([16, 16, 16]);
        Ansi.SetBgColor(Data.LIGHT_COLOR);
        Ansi.Write($" {total} ");

        Ansi.SetBgColor(Data.DARK_COLOR);
    }

    private async Task Discover() {
        cancellationTokenSource = new CancellationTokenSource();
        cancellationToken = cancellationTokenSource.Token;

        Tokens.dictionary.TryAdd(cancellationTokenSource, cancellationToken);

        if (mikrotik) {

        }

        if (ubiquiti) {
            DiscoverItem[] items = Proprietary.Ubiquiti.Discover();
            for (int i = 0; i < items.Length; i++) {
                list.Add(items[i]);
            }
        }

        list.Draw(true);


        while (!cancellationToken.IsCancellationRequested) {
            if (Renderer.ActiveFrame == this && Renderer.ActiveDialog is null) {
                DrawStatus();
                Ansi.Push();
            }
            await Task.Delay(500);

            if (Renderer.ActiveDialog is not null) continue;
            if (Renderer.ActiveFrame != this) continue;
        }

        Tokens.dictionary.TryRemove(cancellationTokenSource, out _);
        cancellationTokenSource.Dispose();

        list.Draw(true);
    }

    private void AddItem(string host) {
        if (String.IsNullOrEmpty(host)) return;

        list.Add(new DiscoverItem {
            status = 0,
        });
    }

    private void Start() {
        OptionsDialog dialog = new OptionsDialog();

        dialog.okButton.action = () => {
            icmp     = dialog.icmpToggle.Value;
            mdns     = dialog.mdnsToggle.Value;
            ssdp     = dialog.ssdpToggle.Value;
            mikrotik = dialog.mikrotikToggle.Value;
            ubiquiti = dialog.ubiquitiToggle.Value;

            dialog.Close();
            Task.Run(Discover);
        };

        dialog.Show(true);

        dialog.icmpToggle.Value     = icmp;
        dialog.mdnsToggle.Value     = mdns;
        dialog.ssdpToggle.Value     = ssdp;
        dialog.mikrotikToggle.Value = mikrotik;
        dialog.ubiquitiToggle.Value = ubiquiti;
    }

    private void Stop() => cancellationTokenSource?.Cancel();

    private void Clear() {
        list.Clear();
    }
}

file sealed class OptionsDialog : Ui.DialogBox {
    public Ui.SelectBox nicSelectBox;

    public Ui.Toggle icmpToggle;
    public Ui.Toggle mdnsToggle;
    public Ui.Toggle ssdpToggle;
    public Ui.Toggle mikrotikToggle;
    public Ui.Toggle ubiquitiToggle;

    public OptionsDialog() {
        string[] nics = GetNics();

        if (nics.Length == 0) {
            nics = [String.Empty];
        }

        okButton.text = "  Start  ";

        nicSelectBox = new Ui.SelectBox(this) {
            options = nics,
        };

        icmpToggle     = new Ui.Toggle(this, "ICMP");
        mdnsToggle     = new Ui.Toggle(this, "mDNS");
        ssdpToggle     = new Ui.Toggle(this, "SSDP");
        mikrotikToggle = new Ui.Toggle(this, "Mikrotik discover");
        ubiquitiToggle    = new Ui.Toggle(this, "Ubiquiti discover");

        elements.Add(nicSelectBox);

        elements.Add(icmpToggle);
        elements.Add(mdnsToggle);
        elements.Add(ssdpToggle);
        elements.Add(mikrotikToggle);
        elements.Add(ubiquitiToggle);

        defaultElement = nicSelectBox;
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
        nicSelectBox.left = left + 16;
        nicSelectBox.right = Renderer.LastWidth - width - left + 2;
        nicSelectBox.top = top++ - 1;

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

        ssdpToggle.left = left;
        ssdpToggle.top = top - 1;
        Ansi.SetCursorPosition(left, top++);
        Ansi.Write(blank);

        mikrotikToggle.left = left;
        mikrotikToggle.top = top - 1;
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

        if (focusedElement == nicSelectBox) {
            nicSelectBox.Focus();
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
            if (focusedElement == nicSelectBox) {
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

    private static string[] GetNics() {
        List<string> filtered = [];

        NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
        foreach (NetworkInterface nic in nics) {
            UnicastIPAddressInformationCollection unicast = nic.GetIPProperties().UnicastAddresses;
            GatewayIPAddressInformationCollection gateway = nic.GetIPProperties().GatewayAddresses;

            if (unicast.Count == 0) continue;

            IPAddress localIpV4 = null;
            IPAddress subnetMask = null;

            foreach (UnicastIPAddressInformation address in unicast) {
                if (address.Address.AddressFamily == AddressFamily.InterNetwork) {
                    localIpV4 = address.Address;
                    subnetMask = address.IPv4Mask;
                }
            }

            if (localIpV4 is null || IPAddress.IsLoopback(localIpV4) || localIpV4.IsApipa()) continue;


            filtered.Add($"{localIpV4}/{Data.SubnetMaskToCidr(subnetMask)}");
        }

        return filtered.ToArray();
    }
}