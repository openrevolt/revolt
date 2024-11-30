using System.Net.NetworkInformation;

namespace Revolt.Frames;

public sealed class TraceRouteFrame : Ui.Frame {
    public struct TraceItem {
        public string host;
        public string domain;
        public string rtt;
    }

    public static TraceRouteFrame Instance { get; } = new TraceRouteFrame();

    public Ui.Toolbar toolbar;
    public Ui.Textbox textbox;
    public Ui.ListBox<TraceItem> list;

    public TraceRouteFrame() {
        toolbar = new Ui.Toolbar(this) {
            left  = 1,
            right = 1,
            items = [
                new Ui.Toolbar.ToolbarItem() { text="Clear", action=Clear },
            ]
        };

        textbox = new Ui.Textbox(this) {
            top   = 4,
            left  = 16,
            right = 16,
        };

        list = new Ui.ListBox<TraceItem>(this) {
            left   = 16,
            right  = 16,
            top    = 8,
            bottom = 1,
            drawItemHandler = DrawTraceItem
        };

        textbox.enableHistory = true;
        textbox.history = [];

        elements.Add(toolbar);
        elements.Add(textbox);
        elements.Add(list);

        defaultElement = textbox;
        FocusNext();
    }

    public override void Show(bool draw = true) {
        Ansi.HideCursor();
        base.Show(draw);
    }

    public override void Draw(int width, int height) {
        string blank = new string(' ', width);

        int top = 0;
        Ansi.SetBgColor(Data.DARK_COLOR);
        for (int i = top; i <= height; i++) {
            if (i > Console.WindowHeight) break;
            Ansi.SetCursorPosition(0, i);
            Ansi.Write(blank);
        }

        int padding = width < 96 ? 1 : 12;
        textbox.left = padding;
        textbox.right = padding;

        list.left = padding;
        list.right = padding;

        Ansi.SetFgColor(Data.LIGHT_COLOR);
        Ansi.SetBgColor(Data.DARK_COLOR);
        WriteLabel("Target:", padding, 4, width - padding);

        for (int i = 0; i < elements.Count; i++) {
            elements[i].Draw(false);
        }

        if (textbox.isFocused) {
            textbox.Focus(false);
        }

        Ansi.Push();
    }

    private void DrawTraceItem(int index, int x, int y, int width) {
        if (list.items is null || list.items.Count == 0) return;
        if (index >= list.items.Count) return;

        int adjustedY = y + index - list.scrollOffset * list.itemHeight;
        if (adjustedY < y || adjustedY >= Renderer.LastHeight) return;

        TraceItem item = list.items[index];

        Ansi.SetCursorPosition(2, adjustedY);

        if (index == list.index) {
            Ansi.SetFgColor(list.isFocused ? [16, 16, 16] : Data.LIGHT_COLOR);
            Ansi.SetBgColor(list.isFocused ? Data.SELECT_COLOR : Data.SELECT_COLOR_LIGHT);
        }
        else {
            Ansi.SetFgColor(Data.LIGHT_COLOR);
            Ansi.SetBgColor(Data.DARK_COLOR);
        }

        Ansi.SetCursorPosition(x, adjustedY);
        Ansi.Write((index + 1).ToString().PadLeft(3));

        Ansi.SetBgColor(Data.SELECT_COLOR_LIGHT);

        if (index == list.index) {
            Ansi.SetFgColor(list.isFocused ? Data.SELECT_COLOR : Data.SELECT_COLOR_LIGHT);
            Ansi.Write(Data.BIG_RIGHT_TRIANGLE);
        }
        else {
            Ansi.SetBgColor(Data.DARK_COLOR);
            Ansi.Write(' ');
        }

        Ansi.SetFgColor(Data.LIGHT_COLOR);
        Ansi.SetBgColor(index == list.index ? Data.SELECT_COLOR_LIGHT : Data.DARK_COLOR);

        Ansi.Write(' ');

        int hostWidth   = Math.Max(24, width / 3);
        int rttWidth    = 10;
        int domainWidth = Math.Max(width - hostWidth - rttWidth - 10, 1);

        Ansi.Write(item.host.Length > hostWidth ? item.host[..(hostWidth - 1)] + Data.ELLIPSIS : item.host.PadRight(hostWidth));

        Ansi.Write(' ');

        Ansi.Write(item.rtt.PadLeft(rttWidth));

        Ansi.Write(new String(' ', 4));

        Ansi.SetFgColor(Data.INPUT_COLOR);
        Ansi.Write(item.domain.Length > domainWidth ? item.domain[..(domainWidth - 1)] + Data.ELLIPSIS : item.domain.PadRight(domainWidth));

        Ansi.SetBgColor(Data.DARK_COLOR);
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

        case ConsoleKey.Enter:
            if (focusedElement == textbox) {
                //textbox.Value = String.Empty;
                string value = textbox.Value.Trim();
                if (String.IsNullOrEmpty(value)) break;

                textbox.Blur(true);
                list.Clear();
                textbox.history.Add(value);
                TraceAsync(value).GetAwaiter().GetResult();
                textbox.Focus(true);
            }
            else {
                focusedElement?.HandleKey(key);
            }
            break;

        default:
            focusedElement?.HandleKey(key);
            break;
        }

        return true;
    }

    private void SetStatus(string status) {
        (int left, int _, int width, int _) = list.GetBounding();

        Ansi.SetCursorPosition(left, 7);
        Ansi.SetFgColor(Data.SELECT_COLOR);
        Ansi.SetBgColor(Data.DARK_COLOR);

        if (status is null) {
            Ansi.Write(new String(' ', width - 1));
        }
        else {
            Ansi.SetRapidBlinkOn();
            Ansi.Write(status);
            Ansi.SetBlinkOff();
        }

        Ansi.Push();
    }

    private void Clear() {
        list.Clear();
    }

    private async Task TraceAsync(string target) {
        SetStatus("Tracing route...");

        string lastHop = String.Empty;
        string targetIpString;

        try {
            System.Net.IPAddress[] targetIpAddress = System.Net.Dns.GetHostAddresses(target);
            if (targetIpAddress.Length == 0) return;

            targetIpString = targetIpAddress[0].ToString();
        }
        catch {
            list.Draw(true);
            SetStatus(null);
            return;
        }

        const int timeout = 1_000;
        const int maxHops = 30;
        int batch = OperatingSystem.IsWindows() ? 8 : 1;

        (int left, int top, int width, _) = list.GetBounding();

        try {
            for (int i = 0; i < maxHops; i += batch) {
                int currentBatchSize = Math.Min(batch, maxHops - i);
                Task<string[]>[] batchTasks = new Task<string[]>[currentBatchSize];
                for (int ttl = 0; ttl < currentBatchSize; ttl++) {
                    batchTasks[ttl] = TraceHost(targetIpString, timeout, i + ttl + 1);
                }

                string[][] batchResults = await Task.WhenAll(batchTasks);

                foreach (string[] result in batchResults) {
                    string hop    = result[0];
                    string domain = result[1];
                    string rtt    = result[2];

                    if (hop == lastHop && !lastHop.Equals("timed out") && lastHop.Equals("unknown")) return;

                    list.Add(new TraceItem {
                        host   = hop,
                        domain = domain,
                        rtt    = rtt,
                    });

                    list.drawItemHandler(list.items.Count - 1, left, top, width);

                    if (hop == targetIpString) return;

                    lastHop = hop;
                }

                list.Draw(true);
            }
        }
        finally {
            list.Draw(true);
            SetStatus(null);
        }
    }

    private static async Task<string[]> TraceHost(string target, int timeout, int ttl) {
        using Ping ping = new Ping();

        string host, domain, rtt;
        domain = String.Empty;
        rtt = String.Empty;

        try {
            PingReply reply = await ping.SendPingAsync(target, timeout, Protocols.Icmp.ICMP_PAYLOAD, new PingOptions(ttl, true));
            host = reply.Address?.ToString() ?? "unknown";

            if (reply.Status == IPStatus.TtlExpired || reply.Status == IPStatus.Success) {
                try {
                    domain = (await System.Net.Dns.GetHostEntryAsync(host)).HostName;
                }
                catch { }

                reply = await ping.SendPingAsync(host, timeout, Protocols.Icmp.ICMP_PAYLOAD, new PingOptions(ttl, true));
                rtt = $"{reply.RoundtripTime}ms";
            }
            else if (reply.Status == IPStatus.TimedOut) {
                host = "--";
                rtt  = "timed out";
            }
        }
        catch (Exception) {
            host = "--";
            rtt  = "timed out";
        }

        return [host, domain, rtt];
    }
}
