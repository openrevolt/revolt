using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Revolt.Frames;

public sealed class IpDiscoveryFrame : Ui.Frame {

    public static IpDiscoveryFrame Instance { get; } = new IpDiscoveryFrame();

    public struct DiscoverItem {
        public int status;
        public string name;
        public string ip;
        public string mac;
        public string manufacturer;
    }

    public Ui.Toolbar toolbar;
    public Ui.ListBox<DiscoverItem> list;

    private CancellationTokenSource cancellationTokenSource;
    private CancellationToken cancellationToken;

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
            itemHeight        = 2,
            drawItemHandler   = DrawPingItem,
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

    private void DrawPingItem(int index, int x, int y, int width) {
        if (list.items is null || list.items.Count == 0) return;
        if (index >= list.items.Count) return;

        int adjustedY = y + index * 2 - list.scrollOffset * list.itemHeight;
        if (adjustedY < y || adjustedY > Renderer.LastHeight) return;

        DiscoverItem item = list.items[index];

    }

    private void DrawStatus() {
        int total = list.items.Count;

        Ansi.SetCursorPosition(2, Renderer.LastHeight);

        Ansi.SetFgColor([16, 16, 16]);
        Ansi.SetBgColor(Data.FG_COLOR);
        Ansi.Write($" {total} ");
    }

    private async Task Discover() {
        cancellationTokenSource = new CancellationTokenSource();
        cancellationToken = cancellationTokenSource.Token;

        Tokens.dictionary.TryAdd(cancellationTokenSource, cancellationToken);

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
            dialog.Close();
            Task.Run(Discover);
        };

        dialog.Show(true);
    }

    private void Stop() => cancellationTokenSource?.Cancel();

    private void Clear() {
        list.Clear();
    }
}

file sealed class OptionsDialog : Ui.DialogBox {
    public Ui.SelectBox nicSelectBox;

    public OptionsDialog() {
        string[] nics = GetNics();

        if (nics.Length == 0) {
            nics = [String.Empty];
        }

        okButton.text = "  Start  ";

        nicSelectBox = new Ui.SelectBox(this) {
            options = nics,
        };

        elements.Add(nicSelectBox);

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

        WriteLabel("Network:", left, ++top, width);
        nicSelectBox.left = left + 16;
        nicSelectBox.right = Renderer.LastWidth - width - left + 2;
        nicSelectBox.top = top++ - 1;

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