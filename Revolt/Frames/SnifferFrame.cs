using System.Diagnostics;
using Revolt.Protocols;
using Revolt.Sniff;
using Revolt.Tui;
using SharpPcap;

using static Revolt.Sniff.Sniffer;

namespace Revolt.Frames;

public class SnifferFrame : Tui.Frame {
    public static readonly SnifferFrame instance = new SnifferFrame();

    public Tui.Tabs tabs;
    public Tui.Toolbar toolbar;

    private static readonly Ansi.Color TX_COLOR = new Ansi.Color(232, 118, 0);
    private static readonly Ansi.Color RX_COLOR = new Ansi.Color(122, 212, 43);

    private static readonly Ansi.Color BROADCAST_COLOR = new Ansi.Color(0, 172, 255);
    private static readonly Ansi.Color MULTICAST_COLOR = new Ansi.Color(0, 255, 255);
    private static readonly Ansi.Color INVALID_COLOR   = new Ansi.Color(176, 0, 0);

    private Tui.ShadowIndexListBox<Mac, TrafficData>         framesList;
    private Tui.ShadowIndexListBox<IP, TrafficData>          packetList;
    private Tui.ShadowIndexListBox<ushort, TrafficData>      segmentList;
    private Tui.ShadowIndexListBox<ushort, TrafficData>      datagramList;
    private Tui.ShadowIndexListBox<ushort, Count>            etherTypeList;
    private Tui.ShadowIndexListBox<byte, Count>              layer4ProtocolList;
    private Tui.ShadowIndexListBox<IPPair, StreamCount>      tcpStatList;

    private Tui.ListBox<SniffIssuesItem>                     issuesList;
    private Tui.ListBox<byte>                                overviewList;

    private Tui.Element currentList;

    private CancellationTokenSource cancellationTokenSource;
    private CancellationToken cancellationToken;

    private ICaptureDevice captureDevice;
    private Sniffer sniffer;
    
    private long lastUpdate = 0;

    static SnifferFrame() {
        PCap.Init();
    }

    public SnifferFrame() {
        tabs = new Tui.Tabs(this) {
            left  = 0,
            right = 0,
            top   = 0,
            items = [
                new Tui.Tabs.TabItem() { text="L2",         key="2" },
                new Tui.Tabs.TabItem() { text="L3",         key="3" },
                new Tui.Tabs.TabItem() { text="L4 TCP",     key="4" },
                new Tui.Tabs.TabItem() { text="L4 UDP",     key="4" },
                new Tui.Tabs.TabItem() { text="Ethertypes", key="E" },
                new Tui.Tabs.TabItem() { text="Transport",  key="T" },
                new Tui.Tabs.TabItem() { text="TCP stat",   key="S" },
                new Tui.Tabs.TabItem() { text="Issues",     key="I" },
                new Tui.Tabs.TabItem() { text="Overview",   key="O" },
            ],
            OnChange   = Tabs_OnChange,
            DrawLabels = WritePacketsLabels
        };

        framesList = new Tui.ShadowIndexListBox<Mac, TrafficData>(this) {
            left            = 0,
            right           = 0,
            top             = 3,
            bottom          = 2,
            backgroundColor = Glyphs.PANE_COLOR,
            drawItemHandler = DrawFrameItem
        };

        packetList = new Tui.ShadowIndexListBox<IP, TrafficData>(this) {
            left            = 0,
            right           = 0,
            top             = 3,
            bottom          = 2,
            backgroundColor = Glyphs.PANE_COLOR,
            drawItemHandler = DrawPacketItem
        };

        segmentList = new Tui.ShadowIndexListBox<ushort, TrafficData>(this) {
            left            = 0,
            right           = 0,
            top             = 3,
            bottom          = 2,
            backgroundColor = Glyphs.PANE_COLOR,
            drawItemHandler = DrawSegmentItem
        };

        datagramList = new Tui.ShadowIndexListBox<ushort, TrafficData>(this) {
            left            = 0,
            right           = 0,
            top             = 3,
            bottom          = 2,
            backgroundColor = Glyphs.PANE_COLOR,
            drawItemHandler = DrawDatagramItem
        };

        etherTypeList = new Tui.ShadowIndexListBox<ushort, Count>(this) {
            left            = 0,
            right           = 0,
            top             = 3,
            bottom          = 2,
            backgroundColor = Glyphs.PANE_COLOR,
            drawItemHandler = DrawEtherItem
        };

        layer4ProtocolList = new Tui.ShadowIndexListBox<byte, Count>(this) {
            left            = 0,
            right           = 0,
            top             = 3,
            bottom          = 2,
            backgroundColor = Glyphs.PANE_COLOR,
            drawItemHandler = DrawLayer4Item
        };

        tcpStatList = new Tui.ShadowIndexListBox<IPPair, StreamCount>(this) {
            left            = 0,
            right           = 0,
            top             = 3,
            bottom          = 2,
            backgroundColor = Glyphs.PANE_COLOR,
            drawItemHandler = DrawTcpStatItem
        };

        issuesList = new Tui.ListBox<SniffIssuesItem>(this) {
            left            = 0,
            right           = 0,
            top             = 3,
            bottom          = 2,
            backgroundColor = Glyphs.PANE_COLOR,
            drawItemHandler = DrawIssueItem
        };

        overviewList = new Tui.ListBox<byte>(this) {
            left            = 0,
            right           = 0,
            top             = 3,
            bottom          = 2,
            backgroundColor = Glyphs.PANE_COLOR,
            drawItemHandler = DrawOverviewItem,
            items           = [0, 0, 0, 0, 0, 0, 0]
        };

        toolbar = new Tui.Toolbar(this) {
            left  = 0,
            right = 0,
            items = [
                new Tui.Toolbar.ToolbarItem() { text="Start",  key="F2", action=StartDialog },
                //new Tui.Toolbar.ToolbarItem() { text="Filter", key="F4", action=FiltersDialog },
            ],
            drawStatus = DrawStatus
        };

        currentList = framesList;

        elements.Add(tabs);
        elements.Add(currentList);
        elements.Add(toolbar);

        defaultElement = currentList;
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

        case ConsoleKey.F2:
            StartDialog();
            break;

        case ConsoleKey.F4:
            FiltersDialog();
            break;

        case ConsoleKey.D2:
        case ConsoleKey.NumPad2:
            tabs.SetIndex(0);
            break;

        case ConsoleKey.D3:
        case ConsoleKey.NumPad3:
            tabs.SetIndex(1);
            break;

        case ConsoleKey.D4:
        case ConsoleKey.NumPad4:
            if (tabs.index == 2) {
                tabs.SetIndex(3);
            }
            else {
                tabs.SetIndex(2);
            }
            break;

        case ConsoleKey.E:
            tabs.SetIndex(4);
            break;

        case ConsoleKey.T:
            tabs.SetIndex(5);
            break;

        case ConsoleKey.S:
            tabs.SetIndex(6);
            break;

        case ConsoleKey.I:
            tabs.SetIndex(7);
            break;

        case ConsoleKey.O:
            tabs.SetIndex(8);
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

        list.Add([String.Empty, String.Empty]);

        for (int i = 0; i < tabs.items.Length; i++) {
            list.Add([tabs.items[i].key, tabs.items[i].text]);
        }

        return list.ToArray();
    }

    private void Tabs_OnChange() {
        bool flag = focusedElement == elements[1];

        if (flag) {
            focusedElement.Blur(false);
        }

        currentList = tabs.index switch {
            0 => framesList,
            1 => packetList,
            2 => segmentList,
            3 => datagramList,
            4 => etherTypeList,
            5 => layer4ProtocolList,
            6 => tcpStatList,
            7 => issuesList,
            _ => overviewList
        };

        elements[1] = currentList;
        defaultElement = currentList;

        if (flag) {
            focusedElement = elements[1];
            elements[1].Focus();
        }

        if (tabs.index < 4) {
            tabs.DrawLabels = WritePacketsLabels;
        }
        else if (tabs.index < 6) {
            tabs.DrawLabels = WriteProtocolsLabels;
        }
        else if (tabs.index == 6) {
            tabs.DrawLabels = WriteTcpStatLabels;
        }
        else {
            tabs.DrawLabels = null;
        }

        elements[1].Draw(false);

        lastUpdate = Stopwatch.GetTimestamp();
    }

    private void DrawStatus() {
        Ansi.SetCursorPosition(Renderer.LastWidth - 25, Math.Max(Renderer.LastHeight, 0));
        Ansi.SetBgColor(Glyphs.TOOLBAR_COLOR);
        WriteNumber(sniffer?.totalPackets ?? 0, 12, Glyphs.LIGHT_COLOR);
        WriteBytes(sniffer?.totalBytes ?? 0, 12, Glyphs.LIGHT_COLOR);
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

        Ansi.SetCursorPosition(1, adjustedY);
        
        if (isSelected) {
            Ansi.SetFgColor(framesList.isFocused ? Glyphs.DARKGRAY_COLOR : Glyphs.LIGHT_COLOR);
            Ansi.SetBgColor(framesList.isFocused ? Glyphs.FOCUS_COLOR : Glyphs.HIGHLIGHT_COLOR);
        }
        else {
            Ansi.SetFgColor(Glyphs.LIGHT_COLOR);
            Ansi.SetBgColor(Glyphs.PANE_COLOR);
        }

        Ansi.Write(' ');
        Ansi.Write(mac.ToString());
        Ansi.Write(' ');

        Ansi.SetFgColor(Glyphs.LIGHT_COLOR);
        Ansi.SetBgColor(isSelected ? Glyphs.HIGHLIGHT_COLOR : Glyphs.PANE_COLOR);

        Ansi.Write("    ");

        string vendorString;
        if (mac.IsBroadcast()) {
            Ansi.SetFgColor(BROADCAST_COLOR);
            vendorString = "Broadcast";
        }
        else if (mac.IsEthernetMulticast()) {
            Ansi.SetFgColor(0, 208, 255);
            vendorString = "Ethernet multicast";
        }
        else if (mac.IsPVv4Multicast()) {
            Ansi.SetFgColor(MULTICAST_COLOR);
            vendorString = "IPv4 multicast";
        }
        else if (mac.IsPVv6Multicast()) {
            Ansi.SetFgColor(MULTICAST_COLOR);
            vendorString = "IPv6 multicast";
        }
        else if ((mac.value & 0xfffffffffffe) == 0x01_00_0C_CC_CC_CC) {
            Ansi.SetFgColor(MULTICAST_COLOR);
            vendorString = "Multicast (CISCO)";
        }
        else if (mac.IsMulticast()) {
            Ansi.SetFgColor(MULTICAST_COLOR);
            vendorString = "Multicast";
        }
        else if (mac.IsLocallyAdministered()) {
            Ansi.SetFgColor(0, 255, 192);
            vendorString = "Locally administered";
        }
        else {
            vendorString = MacLookup.Lookup(mac);
        }

        if (vendorWidth > 0) {
            Ansi.Write(vendorString.Length > vendorWidth
            ? vendorString[..(vendorWidth - 1)] + Glyphs.ELLIPSIS
            : vendorString.PadRight(vendorWidth));
        }

        WriteTraffic(traffic);

        Ansi.Write(' ');
        Ansi.SetBgColor(Glyphs.DARK_COLOR);

        lastUpdate = Stopwatch.GetTimestamp();
    }

    private void DrawPacketItem(int index, int x, int y, int width) {
        if (packetList.Count == 0) return;
        if (index < 0) return;
        if (index >= packetList.Count) return;

        int adjustedY = y + index - packetList.scrollOffset;
        if (adjustedY < y || adjustedY > Renderer.LastHeight) return;

        TrafficData traffic = packetList[index];
        bool isSelected = index == packetList.index;

        IP     ip       = packetList.shadow.GetKeyByIndex(index);
        string ipString = ip.ToString();
        
        int noteWidth = Math.Max(width - 93, 0);

        Ansi.SetCursorPosition(1, adjustedY);

        if (isSelected) {
            Ansi.SetFgColor(packetList.isFocused ? Glyphs.DARKGRAY_COLOR : Glyphs.LIGHT_COLOR);
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

        if (ip.IsBroadcast()) {
            Ansi.SetFgColor(BROADCAST_COLOR);
            noteString = "Broadcast";
        }
        else if (ip.IsMulticast()) {
            Ansi.SetFgColor(MULTICAST_COLOR);
            noteString = "Multicast";
        }
        else if (ip.IsApipa()) {
            Ansi.SetFgColor(Glyphs.GRAY_COLOR);
            noteString = "APIPA";
        }
        else if (ip.IsIPv6LinkLocal()) {
            Ansi.SetFgColor(Glyphs.GRAY_COLOR);
            noteString = "Link local";
        }
        else if (ip.IsIPv6Teredo()) {
            Ansi.SetFgColor(Glyphs.GRAY_COLOR);
            noteString = "Teredo";
        }
        else if (ip.IsIPv6UniqueLocal()) {
            Ansi.SetFgColor(Glyphs.GRAY_COLOR);
            noteString = "Unique local";
        }
        else if (ip.IsIPv6SiteLocal()) {
            Ansi.SetFgColor(Glyphs.GRAY_COLOR);
            noteString = "Site local";
        }
        else if (ip.IsIPv4MappedIPv6()) {
            Ansi.SetFgColor(Glyphs.GRAY_COLOR);
            noteString = "Mapped IPv4";
        }
        else if (ip.IsIPv4Private()) {
            Ansi.SetFgColor(Glyphs.GRAY_COLOR);
            noteString = "Private";
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

        WriteTraffic(traffic);

        Ansi.Write(' ');
        Ansi.SetBgColor(Glyphs.DARK_COLOR);

        lastUpdate = Stopwatch.GetTimestamp();
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

        int noteWidth = Math.Max(width - 60, 0);
        string noteString = GetL4ProtocolName(port);

        Ansi.SetCursorPosition(1, adjustedY);

        if (isSelected) {
            Ansi.SetFgColor(segmentList.isFocused ? Glyphs.DARKGRAY_COLOR : Glyphs.LIGHT_COLOR);
            Ansi.SetBgColor(segmentList.isFocused ? Glyphs.FOCUS_COLOR : Glyphs.HIGHLIGHT_COLOR);
        }
        else {
            Ansi.SetFgColor(port < 1024 ? Glyphs.LIGHT_COLOR : Glyphs.GRAY_COLOR);
            Ansi.SetBgColor(Glyphs.PANE_COLOR);
        }

        Ansi.Write(' ');
        Ansi.Write(portString.PadRight(7));

        Ansi.SetFgColor(Glyphs.LIGHT_COLOR);
        Ansi.SetBgColor(isSelected ? Glyphs.HIGHLIGHT_COLOR : Glyphs.PANE_COLOR);
        Ansi.Write(' ');

        Ansi.Write(noteString.Length > noteWidth
            ? noteString[..(noteWidth - 1)] + Glyphs.ELLIPSIS
            : noteString.PadRight(noteWidth));

        WriteTraffic(traffic);

        Ansi.Write(' ');
        Ansi.SetBgColor(Glyphs.DARK_COLOR);

        lastUpdate = Stopwatch.GetTimestamp();
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

        int noteWidth = Math.Max(width - 60, 0);
        string noteString = GetL4ProtocolName(port);

        Ansi.SetCursorPosition(1, adjustedY);

        if (isSelected) {
            Ansi.SetFgColor(datagramList.isFocused ? Glyphs.DARKGRAY_COLOR : Glyphs.LIGHT_COLOR);
            Ansi.SetBgColor(datagramList.isFocused ? Glyphs.FOCUS_COLOR : Glyphs.HIGHLIGHT_COLOR);
        }
        else {
            Ansi.SetFgColor(port < 1024 ? Glyphs.LIGHT_COLOR : Glyphs.GRAY_COLOR);
            Ansi.SetBgColor(Glyphs.PANE_COLOR);
        }

        Ansi.Write(' ');
        Ansi.Write(portString.PadRight(7));

        Ansi.SetFgColor(Glyphs.LIGHT_COLOR);
        Ansi.SetBgColor(isSelected ? Glyphs.HIGHLIGHT_COLOR : Glyphs.PANE_COLOR);
        Ansi.Write(' ');

        Ansi.Write(noteString.Length > noteWidth
            ? noteString[..(noteWidth - 1)] + Glyphs.ELLIPSIS
            : noteString.PadRight(noteWidth));

        WriteTraffic(traffic);

        Ansi.Write(' ');
        Ansi.SetBgColor(Glyphs.DARK_COLOR);

        lastUpdate = Stopwatch.GetTimestamp();
    }

    private void DrawEtherItem(int index, int x, int y, int width) {
        if (etherTypeList.Count == 0) return;
        if (index < 0) return;
        if (index >= etherTypeList.Count) return;

        int adjustedY = y + index - etherTypeList.scrollOffset;
        if (adjustedY < y || adjustedY > Renderer.LastHeight) return;

        bool isSelected = index == etherTypeList.index;

        Count count = etherTypeList[index];

        ushort protocol       = etherTypeList.shadow.GetKeyByIndex(index);
        string protocolString = "0x" + protocol.ToString("X2").PadLeft(4, '0');

        int nameWidth = Math.Max(56, 0);
        int noteWidth = Math.Max(width - nameWidth - 34, 0);

        string nameString = GetEtherTypeName(protocol);

        Ansi.SetCursorPosition(1, adjustedY);

        if (isSelected) {
            Ansi.SetFgColor(etherTypeList.isFocused ? Glyphs.DARKGRAY_COLOR : Glyphs.LIGHT_COLOR);
            Ansi.SetBgColor(etherTypeList.isFocused ? Glyphs.FOCUS_COLOR : Glyphs.HIGHLIGHT_COLOR);
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

        WriteNumber(count.packets, 12, Glyphs.LIGHT_COLOR);
        WriteBytes(count.bytes, 12, Glyphs.LIGHT_COLOR);

        Ansi.Write(' ');
        Ansi.SetBgColor(Glyphs.DARK_COLOR);

        lastUpdate = Stopwatch.GetTimestamp();
    }

    private void DrawLayer4Item(int index, int x, int y, int width) {
        if (layer4ProtocolList.Count == 0) return;
        if (index < 0) return;
        if (index >= layer4ProtocolList.Count) return;

        int adjustedY = y + index - layer4ProtocolList.scrollOffset;
        if (adjustedY < y || adjustedY > Renderer.LastHeight) return;

        bool isSelected = index == layer4ProtocolList.index;

        Count count = layer4ProtocolList[index];

        byte protocolCode = layer4ProtocolList.shadow.GetKeyByIndex(index);

        int nameWidth = 56;
        int noteWidth = Math.Max(width - nameWidth - 34, 0);

        string nameString = transportProtocolNames[protocolCode];

        Ansi.SetCursorPosition(1, adjustedY);

        if (isSelected) {
            Ansi.SetFgColor(layer4ProtocolList.isFocused ? Glyphs.DARKGRAY_COLOR : Glyphs.LIGHT_COLOR);
            Ansi.SetBgColor(layer4ProtocolList.isFocused ? Glyphs.FOCUS_COLOR : Glyphs.HIGHLIGHT_COLOR);
        }
        else {
            Ansi.SetFgColor(Glyphs.LIGHT_COLOR);
            Ansi.SetBgColor(Glyphs.PANE_COLOR);
        }

        Ansi.Write(' ');
        Ansi.Write(protocolCode.ToString().PadRight(7));

        Ansi.SetFgColor(Glyphs.LIGHT_COLOR);
        Ansi.SetBgColor(isSelected ? Glyphs.HIGHLIGHT_COLOR : Glyphs.PANE_COLOR);

        Ansi.Write(' ');

        Ansi.Write(nameString.Length > nameWidth
            ? nameString[..(nameWidth - 1)] + Glyphs.ELLIPSIS
            : nameString.PadRight(nameWidth));

        Ansi.Write(new String(' ', noteWidth));

        WriteNumber(count.packets, 12, Glyphs.LIGHT_COLOR);
        WriteBytes(count.bytes, 12, Glyphs.LIGHT_COLOR);

        Ansi.Write(' ');

        Ansi.SetBgColor(Glyphs.DARK_COLOR);

        lastUpdate = Stopwatch.GetTimestamp();
    }

    private void DrawTcpStatItem(int index, int x, int y, int width) {
        if (tcpStatList.Count == 0) return;
        if (index < 0) return;
        if (index >= tcpStatList.Count) return;

        int adjustedY = y + index - tcpStatList.scrollOffset;
        if (adjustedY < y || adjustedY > Renderer.LastHeight) return;

        bool isSelected = index == tcpStatList.index;

        StreamCount count = tcpStatList[index];

        int nameWidth = width > 150 ? 82 : 48;
        int noteWidth = Math.Max(width - nameWidth - 64, 0);

        IPPair ips = sniffer.tcpStatCount.GetKeyByIndex(index);
        string nameString = ips.ToString();

        Ansi.Color fgColor = ips.a.IsIPv4Private() && ips.b.IsIPv4Private() ? Glyphs.WHITE_COLOR : Glyphs.LIGHT_COLOR;

        Ansi.SetCursorPosition(1, adjustedY);

        if (isSelected) {
            Ansi.SetFgColor(tcpStatList.isFocused ? Glyphs.DARKGRAY_COLOR : fgColor);
            Ansi.SetBgColor(tcpStatList.isFocused ? Glyphs.FOCUS_COLOR : Glyphs.HIGHLIGHT_COLOR);
        }
        else {
            Ansi.SetFgColor(fgColor);
            Ansi.SetBgColor(Glyphs.PANE_COLOR);
        }

        Ansi.Write(' ');
        Ansi.Write(nameString.Length > nameWidth
            ? nameString[..(nameWidth - 1)] + Glyphs.ELLIPSIS
            : nameString.PadRight(nameWidth));

        Ansi.SetFgColor(Glyphs.LIGHT_COLOR);
        Ansi.SetBgColor(isSelected ? Glyphs.HIGHLIGHT_COLOR : Glyphs.PANE_COLOR);

        //WriteNumber(count.totalSegments, 12, Glyphs.LIGHT_COLOR);
        WriteNumber(count.totalSegments, 12, Glyphs.LIGHT_COLOR);
        WriteBytes(count.totalBytes, 12, Glyphs.LIGHT_COLOR);

        if (count.total3wh == 0) {
            Ansi.SetFgColor(Glyphs.GRAY_COLOR);
            Ansi.Write("    -           -  ");
        }
        else {
            WriteTextWithPost((count.totalRtt / count.total3wh / 10_000).ToString(), 7, Glyphs.LIGHT_COLOR, "ms");
            WriteTextWithPost($"{count.minRtt / 10_000}-{count.maxRtt / 10_000}", 12, Glyphs.LIGHT_COLOR, "ms");
        }

        if (count.loss == 0) {
            Ansi.SetFgColor(Glyphs.GRAY_COLOR);
            Ansi.Write("         -");
        }
        else {
            WriteNumber(count.loss, 10, Glyphs.LIGHT_COLOR);
        }

        if (count.duplicate == 0) {
            Ansi.SetFgColor(Glyphs.GRAY_COLOR);
            Ansi.Write("         -");
        }
        else {
            WriteNumber(count.duplicate, 10, Glyphs.LIGHT_COLOR);
        }

        Ansi.Write(new String(' ', noteWidth));
    }

    private void DrawIssueItem(int index, int x, int y, int width) {
        if (issuesList.items.Count == 0) return;
        if (index < 0) return;
        if (index >= issuesList.items.Count) return;

        int adjustedY = y + index - issuesList.scrollOffset;
        if (adjustedY < y || adjustedY > Renderer.LastHeight) return;

        bool isSelected = index == issuesList.index;

    }

    private void DrawOverviewItem(int index, int x, int y, int width) {
        if (overviewList.items.Count == 0) return;
        if (index < 0) return;
        if (index >= overviewList.items.Count) return;

        int adjustedY = y + index - overviewList.scrollOffset;
        if (adjustedY < y || adjustedY > Renderer.LastHeight) return;

        bool isSelected = index == overviewList.index;

        string name = index switch {
            0 => " Total:             ",
            1 => " IPv4:              ",
            2 => " IPv6:              ",
            3 => " UDP:               ",
            4 => " TCP:               ",
            5 => " TCP loss:          ",
            6 => " TCP duplicate:     ",
            _ => String.Empty
        };

        Ansi.SetCursorPosition(1, adjustedY);

        if (isSelected) {
            Ansi.SetFgColor(overviewList.isFocused && !String.IsNullOrEmpty(name) ? Glyphs.DARKGRAY_COLOR : Glyphs.LIGHT_COLOR);
            Ansi.SetBgColor(overviewList.isFocused && !String.IsNullOrEmpty(name) ? Glyphs.FOCUS_COLOR : Glyphs.HIGHLIGHT_COLOR);
        }
        else {
            Ansi.SetFgColor(Glyphs.LIGHT_COLOR);
            Ansi.SetBgColor(Glyphs.PANE_COLOR);
        }

        Ansi.Write(String.IsNullOrEmpty(name) ? new String(' ', 20) : name);

        Ansi.SetFgColor(Glyphs.LIGHT_COLOR);
        Ansi.SetBgColor(isSelected ? Glyphs.HIGHLIGHT_COLOR : Glyphs.PANE_COLOR);

        switch (index) {
        case 0:
            WriteNumber(sniffer?.totalPackets ?? 0, 16, Glyphs.LIGHT_COLOR);
            WriteBytes(sniffer?.totalBytes ?? 0, 16, Glyphs.LIGHT_COLOR);
            Ansi.Write(new String(' ', 8));
            break;

        case 1: {
            if (sniffer is null) goto default;
            sniffer.etherTypeCount.TryGetValue((ushort)etherTypes.IPv4, out Count count);
            WriteNumber(count.packets, 16, Glyphs.LIGHT_COLOR);
            WriteBytes(count.bytes, 16, Glyphs.LIGHT_COLOR);
            if (count.packets > 0) {
                Ansi.Write($"   {(((double)count.bytes * 100 / sniffer.totalBytes)).ToString("0.0")}%".PadRight(8));
            }
            else {
                Ansi.Write(new String(' ', 8));
            }
            break;
        }
        
        case 2: {
            if (sniffer is null) goto default;
            sniffer.etherTypeCount.TryGetValue((ushort)etherTypes.IPv6, out Count count);
            WriteNumber(count.packets, 16, Glyphs.LIGHT_COLOR);
            WriteBytes(count.bytes, 16, Glyphs.LIGHT_COLOR);
            if (count.packets > 0) {
                Ansi.Write($"   {(((double)count.bytes * 100 / sniffer.totalBytes)).ToString("0.0")}%".PadRight(8));
            }
            else {
                Ansi.Write(new String(' ', 8));
            }
            break;
        }

        case 3: {
            if (sniffer is null) goto default;
            sniffer.transportCount.TryGetValue((byte)TransportProtocol.UDP, out Count count);
            WriteNumber(count.packets, 16, Glyphs.LIGHT_COLOR);
            WriteBytes(count.bytes, 16, Glyphs.LIGHT_COLOR);
            if (count.packets > 0) {
                Ansi.Write($"   {(((double)count.bytes * 100 / sniffer.totalBytes)).ToString("0.0")}%".PadRight(8));
            }
            else {
                Ansi.Write(new String(' ', 8));
            }
            break;
        }

        case 4: {
            if (sniffer is null) goto default;
            sniffer.transportCount.TryGetValue((byte)TransportProtocol.TCP, out Count count);
            WriteNumber(count.packets, 16, Glyphs.LIGHT_COLOR);
            WriteBytes(count.bytes, 16, Glyphs.LIGHT_COLOR);
            if (count.packets > 0) {
                Ansi.Write($"   {(((double)count.bytes * 100 / sniffer.totalBytes)).ToString("0.0")}%".PadRight(8));
            }
            else {
                Ansi.Write(new String(' ', 8));
            }
            break;
        }

        case 5: {
            if (sniffer is null) goto default;
            WriteNumber(sniffer?.packetLoss ?? 0, 16, Glyphs.LIGHT_COLOR);
            Ansi.Write(new String(' ', 24));
            break;
        }

        case 6: {
            if (sniffer is null) goto default;
            sniffer.transportCount.TryGetValue((byte)TransportProtocol.TCP, out Count tcpCount);
            WriteNumber(sniffer?.packetRetransmit ?? 0, 16, Glyphs.LIGHT_COLOR);
            WriteBytes(sniffer?.bytesRetransmit ?? 0, 16, Glyphs.LIGHT_COLOR);
            if (sniffer?.bytesRetransmit > 0) {
                Ansi.Write($"   {(((double)sniffer.bytesRetransmit * 100 / tcpCount.bytes)).ToString("0.0")}%".PadRight(8));
            }
            else {
                Ansi.Write(new String(' ', 8));
            }
            break;
        }

        default:
            Ansi.Write(new String(' ', 40));
            Ansi.Push();
            break;
        }

        Ansi.Write(new String(' ', width - 60));
    }

    private void WriteTraffic(TrafficData traffic) {
        WriteNumber(traffic.packetsTx, 12, TX_COLOR);
        WriteNumber(traffic.packetsRx, 12, RX_COLOR);
        WriteBytes(traffic.bytesTx, 12, TX_COLOR);
        WriteBytes(traffic.bytesRx, 12, RX_COLOR);

        long now = DateTime.UtcNow.Ticks;
        long delta = now - traffic.lastActivity;
        if (delta < 100_000_000) {
            byte v = (byte)(255 - delta * 223 / 100_000_000);
            Ansi.SetFgColor(v, v, 32);
            Ansi.Write($" {Glyphs.BULLET}");
        }
        else {
            Ansi.Write("  ");
        }
    }

    private void WriteNumber(long value, int padding, Ansi.Color color) {
        if (value == 0) {
            Ansi.SetFgColor(color);
            Ansi.Write("-".PadLeft(padding));
            return;
        }

        string text = value.ToString();

        Ansi.Write(new String(' ', Math.Max(padding - text.Length, 0)));

        for (int i = 0; i < text.Length; i++) {
            int groupIndex = (text.Length - i - 1) / 3;
            Ansi.Color groupColor = color + (groupIndex * 54);
            Ansi.SetFgColor(groupColor);
            Ansi.Write(text[i]);
        }
    }

    private void WriteTextWithPost(string text, int padding, Ansi.Color color, string post) {
        Ansi.Write(new String(' ', Math.Max(padding - text.Length - post.Length, 0)));
        Ansi.SetFgColor(color);
        Ansi.Write(text);
        Ansi.Write(post);
    }

    private void WriteBytes(long value, int padding, Ansi.Color color) {
        if (value == 0) {
            Ansi.SetFgColor(color);
            Ansi.Write("-   ".PadLeft(padding));
            return;
        }

        string text = SizeToString(value);

        Ansi.Write(new String(' ', Math.Max(padding - text.Length, 0)));

        if (text.Length > 6) {
            Ansi.SetFgColor(color + 54);
            Ansi.Write(text.Substring(0, text.Length - 6));

            Ansi.SetFgColor(color);
            Ansi.Write(text.Substring(text.Length - 6));
        }
        else {
            Ansi.SetFgColor(color);
            Ansi.Write(text);
        }
    }

    private void WritePacketsLabels() {
        int left = Renderer.LastWidth - 49;
        if (left < 48) return;

        Ansi.SetFgColor(Glyphs.INPUT_COLOR);
        Tui.Frame.WriteLabel("Tx Packets", left, 3, 12);
        Tui.Frame.WriteLabel("Rx Packets", left + 12, 3, 12);
        Tui.Frame.WriteLabel("  Tx Bytes", left + 24, 3, 12);
        Tui.Frame.WriteLabel("  Rx Bytes", left + 36, 3, 12);
    }

    private void WriteProtocolsLabels() {
        int left = Renderer.LastWidth - 23;
        if (left < 48) return;

        Ansi.SetFgColor(Glyphs.INPUT_COLOR);
        Tui.Frame.WriteLabel("   Packets", left, 3, 12);
        Tui.Frame.WriteLabel("     Bytes", left + 12, 3, 12);
    }

    private void WriteTcpStatLabels() {
        int left = (Renderer.LastWidth > 150 ? 82 : 48) + 3;
        
        Ansi.SetFgColor(Glyphs.INPUT_COLOR);
        Tui.Frame.WriteLabel("   Packets", left, 3, 12);
        Tui.Frame.WriteLabel("     Bytes", left + 12, 3, 12);

        Tui.Frame.WriteLabel("  Avg", left + 24, 3, 7);
        Tui.Frame.WriteLabel("   Min-max", left + 31, 3, 12);

        Tui.Frame.WriteLabel("    Loss", left + 43, 3, 10);
        Tui.Frame.WriteLabel(" Duplic.", left + 53, 3, 10);
        //Tui.Frame.WriteLabel("0-win", left + 57, 3, 7);
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

    private async Task UpdateLoop() {
        cancellationTokenSource = new CancellationTokenSource();
        cancellationToken = cancellationTokenSource.Token;
        Tokens.dictionary.TryAdd(cancellationTokenSource, cancellationToken);

        long lastCount = 0;

        while (!cancellationToken.IsCancellationRequested) {
            await Task.Delay(250, cancellationToken);
            long now = Stopwatch.GetTimestamp();

            if (lastCount == sniffer.totalPackets && now - lastUpdate < 30_000_000) continue;
            if (Renderer.ActiveDialog is not null) continue;
            if (Renderer.ActiveFrame != this) continue;

            currentList.Draw(false);
            toolbar.drawStatus();
            Ansi.Push();

            lastUpdate = now;
            lastCount = sniffer.totalPackets;
        }

        Tokens.dictionary.TryRemove(cancellationTokenSource, out _);
        cancellationTokenSource.Dispose();

        captureDevice?.StopCapture();
        captureDevice?.Close();
        //sniffer?.Dispose();

        toolbar.items[0].text = "Start";
    }

    private void StartDialog() {
        if (captureDevice is not null && captureDevice.Started) {
            StopDialog();
            return;
        }

        StartDialog dialog = new StartDialog();

        dialog.okButton.action = () => {
            captureDevice = dialog.devices[dialog.rangeSelectBox.index];

            dialog.Close();

            try {
                sniffer = new Revolt.Sniff.Sniffer(captureDevice);

                framesList.BindDictionary(sniffer.framesCount);
                packetList.BindDictionary(sniffer.packetCount);
                segmentList.BindDictionary(sniffer.segmentCount);
                datagramList.BindDictionary(sniffer.datagramCount);
                etherTypeList.BindDictionary(sniffer.etherTypeCount);
                layer4ProtocolList.BindDictionary(sniffer.transportCount);
                tcpStatList.BindDictionary(sniffer.tcpStatCount);

                sniffer.Start();

                Task.Run(UpdateLoop);
            }
            catch (SharpPcap.PcapException ex) {
                Tui.MessageDialog message = new Tui.MessageDialog() {
                    text = ex.Error.ToString()
                };
                message.Show();
                return;
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
            toolbar.items[0].text = "Start";

            sniffer.Stop();
            cancellationTokenSource?.Cancel();
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
        ILiveDevice[] sortedDevices = CaptureDeviceList.Instance
            .OrderByDescending(d => d.Description?.Contains("PCI", StringComparison.OrdinalIgnoreCase) == true ||d.Name?.Contains("PCI", StringComparison.OrdinalIgnoreCase) == true)
            .ToArray();

        devices = [];
        List<string> strings = [];

        for (int i = 0; i < sortedDevices.Length; i++) {
            if (sortedDevices[i].MacAddress is null) continue;
            devices.Add(sortedDevices[i]);
            string description = sortedDevices[i].Description ?? (string.Join(':', sortedDevices[i].MacAddress.GetAddressBytes().Select(b => b.ToString("X2"))) + " - " + sortedDevices[i].Name);
            strings.Add(description);
        }

        for (int i = 0; i < sortedDevices.Length; i++) {
            if (sortedDevices[i].MacAddress is not null) continue;
            devices.Add(sortedDevices[i]);
            strings.Add(sortedDevices[i].Description ?? sortedDevices[i].Name);
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

        Ansi.SetFgColor(Glyphs.DARKGRAY_COLOR);
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
        int width = Math.Min(Renderer.LastWidth, 56);
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