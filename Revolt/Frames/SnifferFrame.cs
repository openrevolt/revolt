using System.Collections.Generic;
using System.Net;
using System.Reflection;
using Revolt.Protocols;
using Revolt.Sniff;
using Revolt.Tui;
using SharpPcap;

using static Revolt.Sniff.Sniffer;

namespace Revolt.Frames;

internal class SnifferFrame : Tui.Frame {

    public static SnifferFrame Instance { get; } = new SnifferFrame();

    public Tui.Tabs tabs;
    public Tui.Toolbar toolbar;

    private Tui.ShadowIndexListBox<Mac, TrafficData>       framesList;
    private Tui.ShadowIndexListBox<IPAddress, TrafficData> packetList;
    private Tui.ShadowIndexListBox<ushort, TrafficData>    segmentList;
    private Tui.ShadowIndexListBox<ushort, TrafficData>    datagramList;
    private Tui.ShadowIndexListBox<ushort, Count>         layer3ProtocolList;
    private Tui.ListBox<byte>                              layer4ProtocolList;

    private ICaptureDevice captureDevice;
    private Sniffer sniffer;

    public SnifferFrame() {
        tabs = new Tui.Tabs(this) {
            left  = 1,
            right = 1,
            top   = 0,
            items = [
                new Tui.Tabs.TabItem() { text="Frames",    key="F" },
                new Tui.Tabs.TabItem() { text="Packets",   key="P" },
                new Tui.Tabs.TabItem() { text="Segments",  key="S" },
                new Tui.Tabs.TabItem() { text="Datagrams", key="D" },
                new Tui.Tabs.TabItem() { text="L3",        key="3" },
                new Tui.Tabs.TabItem() { text="L4",        key="4" },
                new Tui.Tabs.TabItem() { text="Overview",  key="O" },
                new Tui.Tabs.TabItem() { text="Issues",    key="I" },
            ],
            OnChange = Tabs_OnChange
        };

        framesList = new Tui.ShadowIndexListBox<Mac, TrafficData>(this) {
            left            = 1,
            right           = 1,
            top             = 3,
            bottom          = 2,
            backgroundColor = Glyphs.PANE_COLOR,
            drawItemHandler = DrawFrameItem
        };

        packetList = new Tui.ShadowIndexListBox<IPAddress, TrafficData>(this) {
            left            = 1,
            right           = 1,
            top             = 3,
            bottom          = 2,
            backgroundColor = Glyphs.PANE_COLOR,
            drawItemHandler = DrawPacketItem
        };

        segmentList = new Tui.ShadowIndexListBox<ushort, TrafficData>(this) {
            left            = 1,
            right           = 1,
            top             = 3,
            bottom          = 2,
            backgroundColor = Glyphs.PANE_COLOR,
            drawItemHandler = DrawSegmentItem
        };

        datagramList = new Tui.ShadowIndexListBox<ushort, TrafficData>(this) {
            left            = 1,
            right           = 1,
            top             = 3,
            bottom          = 2,
            backgroundColor = Glyphs.PANE_COLOR,
            drawItemHandler = DrawDatagramItem
        };

        layer3ProtocolList = new Tui.ShadowIndexListBox<ushort, Count>(this) {
            left = 1,
            right = 1,
            top = 3,
            bottom = 2,
            backgroundColor = Glyphs.PANE_COLOR,
            drawItemHandler = DrawLayer3Item
        };

        layer4ProtocolList = new Tui.ListBox<byte>(this) {
            left = 1,
            right = 1,
            top = 3,
            bottom = 2,
            backgroundColor = Glyphs.PANE_COLOR,
            drawItemHandler = DrawLayer4Item
        };

        toolbar = new Tui.Toolbar(this) {
            left  = 0,
            right = 0,
            items = [
                new Tui.Toolbar.ToolbarItem() { text="Start",  key="F2", action=StartDialog },
                new Tui.Toolbar.ToolbarItem() { text="Filter", key="F4", action=FiltersDialog },
            ],
        };

        elements.Add(tabs);
        elements.Add(framesList);
        elements.Add(toolbar);

        defaultElement = framesList;
        FocusNext();

        for (short i = 0; i < 256; i++) {
            layer4ProtocolList.Add((byte)i);
        }
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

        case ConsoleKey.F:
            tabs.SetIndex(0);
            break;

        case ConsoleKey.P:
            tabs.SetIndex(1);
            break;

        case ConsoleKey.S:
            tabs.SetIndex(2);
            break;

        case ConsoleKey.D:
            tabs.SetIndex(3);
            break;

        case ConsoleKey.D3:
        case ConsoleKey.NumPad3:
            tabs.SetIndex(4);
            break;

        case ConsoleKey.D4:
        case ConsoleKey.NumPad4:
            tabs.SetIndex(5);
            break;

        case ConsoleKey.O:
            tabs.SetIndex(6);
            break;

        case ConsoleKey.I:
            tabs.SetIndex(7);
            break;

        case ConsoleKey.F2:
            StartDialog();
            break;

        case ConsoleKey.F4:
            FiltersDialog();
            break;

        default:
            focusedElement.HandleKey(key);
            break;
        }

        return true;
    }

    private void Tabs_OnChange() {
        bool flag = focusedElement == elements[1];

        if (flag) {
            focusedElement.Blur(false);
        }

        elements[1] = tabs.index switch {
            0 => framesList,
            1 => packetList,
            2 => segmentList,
            3 => datagramList,
            4 => layer3ProtocolList,
            5 => layer4ProtocolList,
            _ => datagramList
        };

        if (flag) {
            focusedElement = elements[1];
            elements[1].Focus();
        }

        elements[1].Draw(true);
    }

    private void DrawFrameItem(int index, int x, int y, int width) {
        if (framesList.Count == 0) return;
        if (index < 0) return;
        if (index >= framesList.Count) return;

        int adjustedY = y + index - framesList.scrollOffset;
        if (adjustedY < y || adjustedY > Renderer.LastHeight) return;

        TrafficData traffic = framesList[index];
        bool isSelected = index == framesList.index;

        int vendorWidth = Math.Max(width - 74, 0);
        Mac mac = framesList.shadow.GetKeyByIndex(index);

        Ansi.SetCursorPosition(2, adjustedY);
        
        if (isSelected) {
            Ansi.SetFgColor(framesList.isFocused ? [16, 16, 16] : Glyphs.LIGHT_COLOR);
            Ansi.SetBgColor(framesList.isFocused ? Glyphs.FOCUS_COLOR : Glyphs.HIGHLIGHT_COLOR);
        }
        else {
            Ansi.SetFgColor(Glyphs.LIGHT_COLOR);
            Ansi.SetBgColor(Glyphs.PANE_COLOR);
        }

        Ansi.Write(' ');
        Ansi.Write(mac.ToFormattedString());
        Ansi.Write(' ');

        Ansi.SetFgColor(Glyphs.LIGHT_COLOR);
        Ansi.SetBgColor(isSelected ? Glyphs.HIGHLIGHT_COLOR : Glyphs.PANE_COLOR);

        Ansi.Write("    ");

        string vendorString;

        if (mac.IsBroadcast()) {
            Ansi.SetFgColor([0, 160, 255]);
            vendorString = "Broadcast";
        }
        else if (mac.IsEthernetMulticast()) {
            Ansi.SetFgColor([0, 224, 255]);
            vendorString = "Ethernet multicast";
        }
        else if (mac.IsPVv4Multicast()) {
            Ansi.SetFgColor([0, 255, 255]);
            vendorString = "IPv4 multicast";
        }
        else if (mac.IsPVv6Multicast()) {
            Ansi.SetFgColor([0, 255, 255]);
            vendorString = "IPv6 multicast";
        }
        else {
            vendorString = MacLookup.Lookup(mac);
        }

        if (vendorWidth > 0) {
            Ansi.Write(vendorString.Length > vendorWidth
            ? vendorString[..(vendorWidth - 1)] + Glyphs.ELLIPSIS
            : vendorString.PadRight(vendorWidth));
        }

        DrawTraffic(traffic);

        Ansi.Write(' ');
        Ansi.SetBgColor(Glyphs.DARK_COLOR);
    }

    private void DrawPacketItem(int index, int x, int y, int width) {
        if (packetList.Count == 0) return;
        if (index < 0) return;
        if (index >= packetList.Count) return;

        int adjustedY = y + index - packetList.scrollOffset;
        if (adjustedY < y || adjustedY > Renderer.LastHeight) return;

        TrafficData traffic = packetList[index];
        bool isSelected = index == packetList.index;

        IPAddress ip       = packetList.shadow.GetKeyByIndex(index) ?? new IPAddress(0);
        string    ipString = ip.ToString();
        
        int noteWidth = Math.Max(width - 93, 0);

        Ansi.SetCursorPosition(2, adjustedY);

        if (isSelected) {
            Ansi.SetFgColor(packetList.isFocused ? [16, 16, 16] : Glyphs.LIGHT_COLOR);
            Ansi.SetBgColor(packetList.isFocused ? Glyphs.FOCUS_COLOR : Glyphs.HIGHLIGHT_COLOR);
        }
        else {
            Ansi.SetFgColor(Glyphs.LIGHT_COLOR);
            Ansi.SetBgColor(Glyphs.PANE_COLOR);
        }

        Ansi.Write(' ');
        Ansi.Write(ipString.PadRight(40));

        Ansi.SetFgColor(Glyphs.LIGHT_COLOR);
        Ansi.SetBgColor(isSelected ? Glyphs.HIGHLIGHT_COLOR : Glyphs.PANE_COLOR);

        string noteString;
        if (IPAddress.IsLoopback(ip)) {
            Ansi.SetFgColor([0, 255, 255]);
            noteString = "Loopback";
        }
        else if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) {
            byte[] bytes = ip.GetAddressBytes();
            if (bytes.All(o => o == 255)) {
                Ansi.SetFgColor([0, 160, 255]);
                noteString = "Broadcast";
            }
            else if (bytes[0] > 223 && bytes[0] < 240) {
                Ansi.SetFgColor([0, 255, 255]);
                noteString = "Multicast";
            }
            else if (ip.IsApipa()) {
                Ansi.SetFgColor([128, 128, 128]);
                noteString = "APIPA";
            }
            else if (ip.IsPrivate()) {
                Ansi.SetFgColor([128, 128, 128]);
                noteString = "Private";
            }
            else {
                noteString = String.Empty;
            }
        }
        else if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6) {
            if (ip.IsIPv6Multicast) {
                Ansi.SetFgColor([0, 255, 255]);
                noteString = "Multicast";
            }
            else if (ip.IsIPv6Teredo) {
                Ansi.SetFgColor([128, 128, 128]);
                noteString = "Teredo";
            }
            else if (ip.IsIPv4MappedToIPv6) {
                Ansi.SetFgColor([128, 128, 128]);
                noteString = "Mapped IPv4";
            }
            else if (ip.IsIPv6UniqueLocal) {
                Ansi.SetFgColor([128, 128, 128]);
                noteString = "Unique local";
            }
            else if (ip.IsIPv6LinkLocal) {
                Ansi.SetFgColor([128, 128, 128]);
                noteString = "Link local";
            }
            else if (ip.IsIPv6SiteLocal) {
                byte b = 255;
                Ansi.SetFgColor([b, 32, 32]);
                noteString = "Site local";
            }
            else {
                noteString = String.Empty;
            }
        }
        else {
            noteString = String.Empty;
        }

        if (noteWidth > 0) {
            Ansi.Write(' ');
            Ansi.Write(noteString.Length > noteWidth
            ? noteString[..(noteWidth - 1)] + Glyphs.ELLIPSIS
            : noteString.PadRight(noteWidth));
        }

        DrawTraffic(traffic);

        Ansi.Write(' ');
        Ansi.SetBgColor(Glyphs.DARK_COLOR);
    }

    private void DrawSegmentItem(int index, int x, int y, int width) {
        if (segmentList.Count == 0) return;
        if (index < 0) return;
        if (index >= segmentList.Count) return;

        int adjustedY = y + index - segmentList.scrollOffset;
        if (adjustedY < y || adjustedY > Renderer.LastHeight) return;

        TrafficData traffic = segmentList[index];
        bool isSelected = index == segmentList.index;

        ushort port       = segmentList.shadow.GetKeyByIndex(index);
        string portString = port.ToString();

        int noteWidth = Math.Max(width - 72, 0);

        Ansi.SetCursorPosition(2, adjustedY);

        if (isSelected) {
            Ansi.SetFgColor(segmentList.isFocused ? [16, 16, 16] : Glyphs.LIGHT_COLOR);
            Ansi.SetBgColor(segmentList.isFocused ? Glyphs.FOCUS_COLOR : Glyphs.HIGHLIGHT_COLOR);
        }
        else {
            Ansi.SetFgColor(port < 1024 ? Glyphs.LIGHT_COLOR : [128, 128, 128]);
            Ansi.SetBgColor(Glyphs.PANE_COLOR);
        }

        Ansi.Write(' ');
        Ansi.Write(portString.PadRight(20));

        Ansi.SetFgColor(Glyphs.LIGHT_COLOR);
        Ansi.SetBgColor(isSelected ? Glyphs.HIGHLIGHT_COLOR : Glyphs.PANE_COLOR);

        Ansi.Write(new String(' ', noteWidth));

        DrawTraffic(traffic);

        Ansi.Write(' ');
        Ansi.SetBgColor(Glyphs.DARK_COLOR);
    }

    private void DrawDatagramItem(int index, int x, int y, int width) {
        if (datagramList.Count == 0) return;
        if (index < 0) return;
        if (index >= datagramList.Count) return;

        int adjustedY = y + index - datagramList.scrollOffset;
        if (adjustedY < y || adjustedY > Renderer.LastHeight) return;

        TrafficData traffic = datagramList[index];
        bool isSelected = index == datagramList.index;

        ushort port       = datagramList.shadow.GetKeyByIndex(index);
        string portString = port.ToString();

        int noteWidth = Math.Max(width - 72, 0);

        Ansi.SetCursorPosition(2, adjustedY);

        if (isSelected) {
            Ansi.SetFgColor(datagramList.isFocused ? [16, 16, 16] : Glyphs.LIGHT_COLOR);
            Ansi.SetBgColor(datagramList.isFocused ? Glyphs.FOCUS_COLOR : Glyphs.HIGHLIGHT_COLOR);
        }
        else {
            Ansi.SetFgColor(port < 1024 ? Glyphs.LIGHT_COLOR : [128, 128, 128]);
            Ansi.SetBgColor(Glyphs.PANE_COLOR);
        }

        Ansi.Write(' ');
        Ansi.Write(portString.PadRight(20));

        Ansi.SetFgColor(Glyphs.LIGHT_COLOR);
        Ansi.SetBgColor(isSelected ? Glyphs.HIGHLIGHT_COLOR : Glyphs.PANE_COLOR);

        Ansi.Write(new String(' ', noteWidth));

        DrawTraffic(traffic);

        Ansi.Write(' ');
        Ansi.SetBgColor(Glyphs.DARK_COLOR);
    }

    private void DrawLayer3Item(int index, int x, int y, int width) {
        if (layer3ProtocolList.Count == 0) return;
        if (index < 0) return;
        if (index >= layer3ProtocolList.Count) return;

        bool isSelected = index == layer3ProtocolList.index;

        int adjustedY = y + index - layer3ProtocolList.scrollOffset;
        if (adjustedY < y || adjustedY > Renderer.LastHeight) return;

        Count count = layer3ProtocolList[index];

        ushort protocol       = layer3ProtocolList.shadow.GetKeyByIndex(index);
        string protocolString = "0x" + protocol.ToString("X2").PadLeft(4, '0');

        int nameWidth = Math.Max(20, 0);
        int noteWidth = Math.Max(width - nameWidth - 34, 0);

        string nameString = GetNetworkProtocolName(protocol);

        Ansi.SetCursorPosition(2, adjustedY);

        if (isSelected) {
            Ansi.SetFgColor(layer3ProtocolList.isFocused ? [16, 16, 16] : Glyphs.LIGHT_COLOR);
            Ansi.SetBgColor(layer3ProtocolList.isFocused ? Glyphs.FOCUS_COLOR : Glyphs.HIGHLIGHT_COLOR);
        }
        else {
            Ansi.SetFgColor(Glyphs.LIGHT_COLOR);
            Ansi.SetBgColor(Glyphs.PANE_COLOR);
        }

        Ansi.Write(' ');
        Ansi.Write(protocolString.PadRight(7));

        Ansi.SetFgColor(Glyphs.LIGHT_COLOR);
        Ansi.SetBgColor(isSelected ? Glyphs.HIGHLIGHT_COLOR : Glyphs.PANE_COLOR);

        Ansi.Write(' ');

        Ansi.Write(nameString.Length > nameWidth
            ? nameString[..(nameWidth - 1)] + Glyphs.ELLIPSIS
            : nameString.PadRight(nameWidth));

        Ansi.Write(new String(' ', noteWidth));

        DrawNumber(count.packets, 12, Glyphs.LIGHT_COLOR);
        DrawBytes(count.bytes, 12, Glyphs.LIGHT_COLOR);

        Ansi.Write(' ');
        Ansi.SetBgColor(Glyphs.DARK_COLOR);
    }

    private void DrawLayer4Item(int index, int x, int y, int width) {
        int adjustedY = y + index - layer4ProtocolList.scrollOffset;
        if (adjustedY < y || adjustedY > Renderer.LastHeight) return;

        bool isSelected = index == layer4ProtocolList.index;

        int nameWidth = Math.Max(20, 0);
        int noteWidth = Math.Max(width - nameWidth - 34, 0);

        string nameString = transportProtocolNames[index];

        Ansi.SetCursorPosition(2, adjustedY);

        if (isSelected) {
            Ansi.SetFgColor(layer4ProtocolList.isFocused ? [16, 16, 16] : Glyphs.LIGHT_COLOR);
            Ansi.SetBgColor(layer4ProtocolList.isFocused ? Glyphs.FOCUS_COLOR : Glyphs.HIGHLIGHT_COLOR);
        }
        else {
            Ansi.SetFgColor(Glyphs.LIGHT_COLOR);
            Ansi.SetBgColor(Glyphs.PANE_COLOR);
        }

        Ansi.Write(' ');
        Ansi.Write(index.ToString().PadRight(7));

        Ansi.SetFgColor(Glyphs.LIGHT_COLOR);
        Ansi.SetBgColor(isSelected ? Glyphs.HIGHLIGHT_COLOR : Glyphs.PANE_COLOR);

        Ansi.Write(' ');

        Ansi.Write(nameString.Length > nameWidth
            ? nameString[..(nameWidth - 1)] + Glyphs.ELLIPSIS
            : nameString.PadRight(nameWidth));

        Ansi.Write(new String(' ', noteWidth));

        DrawNumber(sniffer?.transportPackets[index] ?? 0, 12, Glyphs.LIGHT_COLOR);
        DrawBytes(sniffer?.transportBytes[index] ?? 0, 12, Glyphs.LIGHT_COLOR);

        Ansi.Write(' ');

        Ansi.SetBgColor(Glyphs.DARK_COLOR);
    }

    private void DrawTraffic(TrafficData traffic) {
        DrawNumber(traffic.packetsTx, 12, [232, 118, 0]);
        DrawNumber(traffic.packetsRx, 12, [122, 212, 43]);
        DrawBytes(traffic.bytesTx, 12, [232, 118, 0]);
        DrawBytes(traffic.bytesRx, 12, [122, 212, 43]);

        long now = DateTime.UtcNow.Ticks;
        long delta = now - traffic.lastActivity;
        if (delta < 100_000_000) {
            byte r = (byte)(255 - delta * 223 / 100_000_000);
            Ansi.SetFgColor([r, 32, 32]);
            Ansi.Write($" {Glyphs.BULLET}");
        }
        else {
            Ansi.Write("  ");
        }
    }

    private void DrawNumber(long value, int padding, byte[] color) {
        if (value == 0) {
            Ansi.SetFgColor(color);
            Ansi.Write("-".PadLeft(padding));
            return;
        }

        string text = value.ToString();

        Ansi.Write(new String(' ', Math.Max(padding - text.Length, 0)));

        for (int i = 0; i < text.Length; i++) {
            int groupIndex = (text.Length - i - 1) / 3;
            byte[] groupColor = color.Select(c => (byte)Math.Min(c + groupIndex * 56, 255)).ToArray();
            Ansi.SetFgColor(groupColor);
            Ansi.Write(text[i]);
        }
    }

    private void DrawBytes(long value, int padding, byte[] color) {
        if (value == 0) {
            Ansi.SetFgColor(color);
            Ansi.Write("-   ".PadLeft(padding));
            return;
        }

        string text = SizeToString(value);

        Ansi.Write(new String(' ', Math.Max(padding - text.Length, 0)));

        if (text.Length > 6) {
            Ansi.SetFgColor(color.Select(c => (byte)Math.Min(c + 56, 255)).ToArray());
            Ansi.Write(text.Substring(0, text.Length - 6));

            Ansi.SetFgColor(color);
            Ansi.Write(text.Substring(text.Length - 6));
        }
        else {
            Ansi.SetFgColor(color);
            Ansi.Write(text);
        }
    }

    private static string SizeToString(long size) {
        if (size < 65_536) return $"{size} B ";
        if (size < 65_536 * 1024) return $"{Math.Floor(size / 1024f)} KB";
        if (size < 65_536 * Math.Pow(1024, 2)) return $"{Math.Floor(size / Math.Pow(1024, 2))} MB";
        if (size < 65_536 * Math.Pow(1024, 3)) return $"{Math.Floor(size / Math.Pow(1024, 3))} GB";
        if (size < 65_536 * Math.Pow(1024, 4)) return $"{Math.Floor(size / Math.Pow(1024, 4))} TB";
        if (size < 65_536 * Math.Pow(1024, 5)) return $"{Math.Floor(size / Math.Pow(1024, 5))} EB";
        if (size < 65_536 * Math.Pow(1024, 6)) return $"{Math.Floor(size / Math.Pow(1024, 6))} ZB";
        if (size < 65_536 * Math.Pow(1024, 7)) return $"{Math.Floor(size / Math.Pow(1024, 7))} YB";
        if (size < 65_536 * Math.Pow(1024, 8)) return $"{Math.Floor(size / Math.Pow(1024, 8))} BB";
        return size.ToString();
    }

    private void StartDialog() {
        if (captureDevice is not null && captureDevice.Started) {
            StopDialog();
            return;
        }

        StartDialog dialog = new StartDialog();

        dialog.okButton.action = () => {
            captureDevice      = dialog.devices[dialog.rangeSelectBox.index];

            dialog.Close();

            try {
                sniffer = new Revolt.Sniff.Sniffer(captureDevice);

                framesList.BindDictionary(sniffer.framesCount);
                packetList.BindDictionary(sniffer.packetCount);
                segmentList.BindDictionary(sniffer.segmentCount);
                datagramList.BindDictionary(sniffer.datagramCount);
                layer3ProtocolList.BindDictionary(sniffer.networkCount);

                sniffer.Start();
            }
            catch (Exception ex) {
                Tui.MessageDialog message = new Tui.MessageDialog() {
                    text = ex.Message
                };
                message.Show();
                return;
            }

            toolbar.items[0].text = "Stop";
            toolbar.Draw(true);
        };

        dialog.Show(true);
    }

    private void StopDialog() {
        Tui.ConfirmDialog dialog = new Tui.ConfirmDialog() {
            text = "Are you sure you want to stop?"
        };

        dialog.okButton.action = () => {
            captureDevice?.StopCapture();
            captureDevice?.Close();
            //sniffer?.Dispose();

            toolbar.items[0].text = "Start";

            dialog.Close();
        };

        dialog.Show();
    }

    private void FiltersDialog() {

    }
}

file sealed class StartDialog : Tui.DialogBox {
    public Tui.SelectBox rangeSelectBox;

    public List<ILiveDevice> devices;

    public StartDialog() {
        CaptureDeviceList captureDevices = CaptureDeviceList.Instance;

        devices = [];
        List<string> strings = [];

        for (int i = 0; i < captureDevices.Count; i++) {
            if (captureDevices[i].MacAddress is null) continue;
            devices.Add(captureDevices[i]);

            if (captureDevices[i].Description is null) {
                string macString = string.Join(':', captureDevices[i].MacAddress.GetAddressBytes().Select(b => b.ToString("X2")));
                strings.Add($"{macString} - {captureDevices[i].Name}");
            }
            else {
                strings.Add(captureDevices[i].Description);
            }
        }

        okButton.text = "  Start  ";

        rangeSelectBox = new Tui.SelectBox(this) {
            options     = strings.ToArray(),
            placeholder = "no nic found"
        };

        elements.Add(rangeSelectBox);

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

        WriteLabel("NIC:", left, ++top, width);
        rangeSelectBox.left = left + 5;
        rangeSelectBox.right = Renderer.LastWidth - width - left + 2;
        rangeSelectBox.top = top++ - 1;


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