using System.Collections.Concurrent;
using System.Net;
using Protest.Protocols;
using Revolt.Protocols;
using Revolt.Tui;

namespace Revolt.Frames;

public sealed class NetMapperFrame : Tui.Frame {
    public struct DiscoverItem {
        public int    status;
        public string name;
        public string ip;
        public string mac;
        public string manufacturer;
        public string other;
        public uint   ipInt;
    }

    public static NetMapperFrame Instance { get; } = new NetMapperFrame();

    public Tui.ListBox<DiscoverItem> list;
    public Tui.Toolbar toolbar;

    private CancellationTokenSource cancellationTokenSource;
    private CancellationToken cancellationToken;

    private ConcurrentBag<uint> discovered = new ConcurrentBag<uint>();

    private bool icmp     = true;
    private bool mdns     = true;
    private bool ubiquiti = false;

    private int  lastStatusLength = 0;

    private (IPAddress, IPAddress, IPAddress) networkRange = (IPAddress.Loopback, IPAddress.Broadcast, IPAddress.IPv6None);

    public NetMapperFrame() {
        list = new Tui.ListBox<DiscoverItem>(this) {
            left            = 1,
            right           = 1,
            top             = 1,
            bottom          = 4,
            itemHeight      = 1,
            drawItemHandler = DrawDiscoverItem
        }; 
        
        toolbar = new Tui.Toolbar(this) {
            left  = 0,
            right = 0,
            items = [
                new Tui.Toolbar.ToolbarItem() { text="Discover", key="F2", action=Start },
                new Tui.Toolbar.ToolbarItem() { text="Clear",    key="F3", action=Clear }
            ],
            drawStatus = DrawStatus
        };

        elements.Add(list);
        elements.Add(toolbar);

        defaultElement = list;
        FocusNext();
    }

    public override void Draw(int width, int height) {
        base.Draw(width, height);
        DrawMap(width, height);
        Ansi.Push();
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

    private void DrawMap(int width, int height) {
        uint gate  = networkRange.Item1.ToUInt32();
        uint mask  = networkRange.Item2.ToUInt32();
        uint start = gate & mask;
        uint end   = gate | ~mask;
        uint span  = end - start;

        uint actualWidth  = Math.Max((uint)width - 4, 1);
        uint hostPerDot   = Math.Max(span / actualWidth / 8, 1);
        uint hostPerGlyph = hostPerDot * 8;

        Ansi.SetCursorPosition(3, Math.Max(height - 2, 0));
        Ansi.SetBgColor(Glyphs.TOOLBAR_COLOR);
        Ansi.SetFgColor(Glyphs.LIGHT_COLOR);

        if (discovered.IsEmpty) {
            Ansi.SetBgColor([48, 48, 48]);
            Ansi.Write(new string(' ', (int)actualWidth));
            return;
        }

        uint i;
        for (i = 0; i < actualWidth; i++) {
            if (start + i * hostPerGlyph > end) break;

            byte braille = 0x00;

            for (uint j = 0; j < 8; j++) {
                for (uint k = 0; k < hostPerDot; k++) {
                    uint address = start + i * hostPerGlyph + j * hostPerDot + k;
                    if (!discovered.Contains(address)) continue;
                    braille |= (byte)(1 << (byte)j);
                    break;
                }
            }

            if (braille == 0) {
                Ansi.Write(' ');
            }
            else {
                Ansi.Write((char)(Glyphs.BRAILLE_BASE | braille));
            }
        }

        Ansi.SetBgColor([48, 48, 48]);
        Ansi.Write(new String(' ', (int)(actualWidth - i)));
    }

    private void DrawStatus() {
        int total = list.items.Count;
        string totalString = $" {total} ";
        int statusLength = totalString.Length;

        if (statusLength != lastStatusLength) {
            Ansi.SetCursorPosition(Renderer.LastWidth - lastStatusLength, Math.Max(Renderer.LastHeight, 0));
            Ansi.SetBgColor(Glyphs.TOOLBAR_COLOR);
            Ansi.Write(new String(' ', lastStatusLength));

            Ansi.SetCursorPosition(Renderer.LastWidth - totalString.Length + 1, Math.Max(Renderer.LastHeight, 0));
            Ansi.SetFgColor([16, 16, 16]);
            Ansi.SetBgColor(Glyphs.LIGHT_COLOR);
            Ansi.Write(totalString);
            Ansi.SetBgColor(Glyphs.DARK_COLOR);
        }
    }

    private void DrawDiscoverItem(int index, int x, int y, int width) {
        if (list.items is null || list.items.Count == 0) return;
        if (index < 0) return;
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
            foreColor = list.isFocused ? [16, 16, 16] : Glyphs.LIGHT_COLOR;
            backColor = list.isFocused ? Glyphs.FOCUS_COLOR : Glyphs.HIGHLIGHT_COLOR;
        }
        else {
            foreColor = Glyphs.LIGHT_COLOR;
            backColor = Glyphs.DARK_COLOR;
        }

        Ansi.SetBgColor(backColor);
        Ansi.SetFgColor(foreColor);
        
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
            Ansi.Write(item.manufacturer.Length > manufacturerWidth ? item.manufacturer[..(manufacturerWidth - 1)] + Glyphs.ELLIPSIS : item.manufacturer.PadRight(manufacturerWidth));
        }

        if (String.IsNullOrEmpty(item.name)) {
            Ansi.Write(new string(' ', nameWidth));
        }
        else {
            Ansi.Write(item.name.Length > nameWidth ? item.name[..(nameWidth - 1)] + Glyphs.ELLIPSIS : item.name.PadRight(nameWidth));
        }

        if (String.IsNullOrEmpty(item.other) && otherWidth > 0) {
            Ansi.Write(new string(' ', otherWidth));
        }
        else if (otherWidth > 0) {
            Ansi.Write(item.other.Length > otherWidth ? item.other[..(otherWidth - 1)] + Glyphs.ELLIPSIS : item.other.PadRight(otherWidth));
        }

        Ansi.SetBgColor(Glyphs.DARK_COLOR);
    }

    private async Task Discover() {
        cancellationTokenSource = new CancellationTokenSource();
        cancellationToken = cancellationTokenSource.Token;
        Tokens.dictionary.TryAdd(cancellationTokenSource, cancellationToken);

        try {
            for (int i = 0; i < list.items.Count; i++) {
                if (discovered.Contains(list.items[i].ipInt)) continue;
                discovered.Add(list.items[i].ipInt);
            }

            if (ubiquiti) {
                DiscoverUbiquiti(cancellationToken);
                for (int i = 0; i < list.items.Count; i++) {
                    if (discovered.Contains(list.items[i].ipInt)) continue;
                    discovered.Add(list.items[i].ipInt);
                }
            }

            if (mdns) {
                DiscoverMdns();
                for (int i = 0; i < list.items.Count; i++) {
                    if (discovered.Contains(list.items[i].ipInt)) continue;
                    discovered.Add(list.items[i].ipInt);
                }
            }

            if (icmp) {
                await DiscoverIcmp(cancellationToken);
            }
        }
        finally {
            Tokens.dictionary.TryRemove(cancellationTokenSource, out _);
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;

            toolbar.items[0].text = "Discover";
            toolbar.items[1].disabled = false;
            toolbar.Draw(true);
        }

        if (Renderer.ActiveDialog is not null) return;
        if (Renderer.ActiveFrame != this) return;

        list.Draw(true);
    }

    private async Task DiscoverIcmp(CancellationToken cancellationToken) {
        await Task.Delay(500, cancellationToken);

        uint gate = networkRange.Item1.ToUInt32();
        uint mask = networkRange.Item2.ToUInt32();

        uint start = gate & mask;
        uint end   = gate | ~mask;

        const uint batchSize = 32;
        System.Net.NetworkInformation.Ping[] pingInstances = new System.Net.NetworkInformation.Ping[batchSize];
        for (int i = 0; i < batchSize; i++) {
            pingInstances[i] = new System.Net.NetworkInformation.Ping();
        }

        try {
            List<Task<short>> tasks = new List<Task<short>>();

            for (uint j = start; j <= end; j += batchSize) {
                if (cancellationToken.IsCancellationRequested) {
                    break;
                }
                tasks.Clear();

                for (uint i = j; i <= Math.Min(j + batchSize - 1, end); i++) {
                    if (discovered.Contains(i)) {
                        tasks.Add(Task.FromResult((short)-1));
                    }
                    else {
                        uint index = i - j;
                        tasks.Add(Icmp.PingAsync(IPAddress.Parse(i.ToString()).ToString(), 250, pingInstances[index]));
                    }
                }

                short[] result = await Task.WhenAll(tasks);
                for (uint i = 0; i < result.Length; i++) {
                    if (result[i] < 0) continue;

                    uint ipInt = i + j;
                    string ipString = IPAddress.Parse(ipInt.ToString()).ToString();

                    string mac = Arp.ArpRequest(ipString);

                    discovered.Add(ipInt);

                    list.Add(new DiscoverItem() {
                        ip           = ipString,
                        ipInt        = ipInt,
                        mac          = mac,
                        manufacturer = MacLookup.Lookup(mac)
                    });
                }

                if (Renderer.ActiveDialog is not null) continue;
                if (Renderer.ActiveFrame != this) continue;

                DrawStatus();
                DrawMap(Renderer.LastWidth, Renderer.LastHeight);
                list.Draw(true);
            }
        }
        catch (Exception ex) {
            Console.WriteLine(ex);
        }
        finally {
            for (int i = 0; i < batchSize; i++) {
                pingInstances[i].Dispose();
            }
        }
    }

    private void DiscoverMdns() {
        List<Mdns.Answer> answers;
        try {
            answers = Mdns.Resolve(Mdns.anyDeviceQuery, 1000, Protocols.Dns.RecordType.ANY);
        }
        catch {
            return;
        }

        for (int i = 0; i < answers.Count; i++) {
            uint ipInt = answers[i].remote.ToUInt32();
            
            if (discovered.Contains(ipInt)) continue;

            list.Add(new DiscoverItem() {
                 ip    = answers[i].remote.ToString(),
                 ipInt = ipInt
            });
        }

        if (Renderer.ActiveDialog is not null) return;
        if (Renderer.ActiveFrame != this) return;

        DrawStatus();
        DrawMap(Renderer.LastWidth, Renderer.LastHeight);
        list.Draw(true);
    }

    private void DiscoverUbiquiti(CancellationToken cancellationToken) {
        List<DiscoverItem> items = Proprietary.Ubiquiti.Discover(networkRange.Item1, cancellationToken);
        for (int i = 0; i < items.Count; i++) {
            if (list.items.FindIndex(o => o.mac == items[i].mac) > -1) continue;
            list.Add(items[i]);
        }

        if (Renderer.ActiveDialog is not null) return;
        if (Renderer.ActiveFrame != this) return;

        DrawStatus();
        DrawMap(Renderer.LastWidth, Renderer.LastHeight);
        list.Draw(true);
    }

    private void Start() {
        if (cancellationTokenSource is not null && cancellationToken.CanBeCanceled) {
            Stop();
            return;
        }

        OptionsDialog dialog = new OptionsDialog();

        dialog.okButton.action = () => {
            icmp     = dialog.icmpToggle.Value;
            mdns     = dialog.mdnsToggle.Value;
            ubiquiti = dialog.ubiquitiToggle.Value;

            networkRange = dialog.networks[dialog.rangeSelectBox.index];

            toolbar.items[0].text = "Stop";
            toolbar.items[1].disabled = true;

            dialog.Close();
            Task.Run(Discover);
        };

        dialog.Show(true);

        dialog.icmpToggle.Value     = icmp;
        dialog.mdnsToggle.Value     = mdns;
        dialog.ubiquitiToggle.Value = ubiquiti;
    }

    private void Stop() {
        Tui.ConfirmDialog dialog = new Tui.ConfirmDialog() {
            text = "Are you sure you want to stop?"
        };

        dialog.okButton.action = () => {
            cancellationTokenSource?.Cancel();
            dialog.Close();
        };

        dialog.Show();
    }

    private void Clear() {
        if (cancellationTokenSource is not null && cancellationToken.CanBeCanceled) return;

        Tui.ConfirmDialog dialog = new Tui.ConfirmDialog() {
            text = "Are you sure you want to clear?"
        };

        dialog.okButton.action = () => {
            discovered.Clear();
            list.Clear();

            DrawMap(Renderer.LastWidth, Renderer.LastHeight);
            Ansi.Push();

            dialog.Close();
        };

        dialog.Show();
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
            networksString = [];
        }
        else {
            networksString = networks.Select(o => $"{o.Item1}/{NetTools.SubnetMaskToCidr(o.Item2)}").ToArray(); ;
        }

        okButton.text = "  Start  ";

        rangeSelectBox = new Tui.SelectBox(this) {
            options     = networksString,
            placeholder = "no nic found"
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
        Ansi.SetBgColor(Glyphs.DIALOG_COLOR);

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