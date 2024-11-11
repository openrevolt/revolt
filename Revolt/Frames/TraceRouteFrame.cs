using System.Net.NetworkInformation;

namespace Revolt.Frames;

public sealed class TraceRouteFrame : Ui.Frame {
    public struct TraceItem {
        public bool status;
        public string host;
        public string domain;
        //public int rtt;
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
        Ansi.SetBgColor(Data.BG_COLOR);
        for (int y = top; y <= height; y++) {
            if (y > Console.WindowHeight) break;

            Ansi.SetCursorPosition(0, y);
            Ansi.Write(blank);
        }

        int padding = width < 64 ? 1 : 16;
        textbox.left = padding;
        textbox.right = padding;

        list.left = padding;
        list.right = padding;

        Ansi.SetFgColor(Data.FG_COLOR);
        Ansi.SetBgColor(Data.BG_COLOR);
        WriteLabel("Target:", padding, 4, width - padding);

        for (int i = 0; i < elements.Count; i++) {
            elements[i].Draw(false);
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
            Ansi.SetFgColor(list.isFocused ? [16, 16, 16] : Data.FG_COLOR);
            Ansi.SetBgColor(list.isFocused ? Data.SELECT_COLOR : Data.SELECT_COLOR_LIGHT);
        }
        else {
            Ansi.SetFgColor(Data.FG_COLOR);
            Ansi.SetBgColor(Data.BG_COLOR);
        }

        Ansi.SetCursorPosition(x, adjustedY);
        Ansi.Write((index + 1).ToString().PadLeft(3));

        Ansi.SetBgColor(Data.SELECT_COLOR_LIGHT);

        if (index == list.index) {
            Ansi.SetFgColor(list.isFocused ? Data.SELECT_COLOR : Data.SELECT_COLOR_LIGHT);
            Ansi.Write(Data.BIG_RIGHT_TRIANGLE);
        }
        else {
            Ansi.SetBgColor(Data.BG_COLOR);
            Ansi.Write(' ');
        }

        Ansi.SetFgColor(Data.FG_COLOR);
        Ansi.SetBgColor(index == list.index ? Data.SELECT_COLOR_LIGHT : Data.BG_COLOR);

        Ansi.Write(' ');

        int hostWidth   = 24;
        int domainWidth = Math.Max(width - hostWidth - 6, 1);

        Ansi.Write(item.host.Length > hostWidth ? item.host[..(hostWidth - 1)] + Data.ELLIPSIS : item.host.PadRight(hostWidth));

        Ansi.Write(' ');

        Ansi.SetFgColor(Data.INPUT_COLOR);
        Ansi.Write(item.domain.Length > domainWidth ? item.domain[..(domainWidth - 1)] + Data.ELLIPSIS : item.domain.PadRight(domainWidth));

        Ansi.SetBgColor(Data.BG_COLOR);
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

                SetStatus("Tracing route");
                Trace(value);
                SetStatus(null);
                
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
        (int _, int _, int width, int _) = list.GetBounding();
        int padding = width < 64 ? 1 : 16;

        Ansi.SetCursorPosition(padding + 1, 7);
        Ansi.SetFgColor(Data.FG_COLOR);
        Ansi.SetBgColor(Data.BG_COLOR);

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

    private void Trace(string target) {
        const int timeout = 1_000;
        const int maxHops = 30;

        (int left, int top, int width, _) = list.GetBounding();

        string lastAddress = String.Empty;
        using Ping ping = new Ping();
        for (int ttl = 1; ttl <= maxHops; ttl++) {

            bool status;
            string host;
            string domain;

            try {
                PingReply reply = ping.Send(target, timeout, Protocols.Icmp.ICMP_PAYLOAD, new PingOptions(ttl, true));
                if (reply is null) break;

                status = reply.Status == IPStatus.TtlExpired || reply.Status == IPStatus.Success;
                host   = reply.Address?.ToString() ?? "unknown";
                domain = String.Empty;

                if (reply.Status == IPStatus.Success || reply.Status == IPStatus.TtlExpired) {
                    try {
                        domain = System.Net.Dns.GetHostEntryAsync(host).GetAwaiter().GetResult().HostName;
                    }
                    catch { }

                    if (lastAddress == reply.Address.ToString()) {
                        break;
                    }
                    else {
                        lastAddress = reply.Address.ToString();
                    }
                }
                else if (reply.Status == IPStatus.TimedOut) {
                    host = "timed out";
                }
                else {
                    break;
                }
            }
            catch (Exception) {
                status = false;
                host   = "timed out";
                domain = String.Empty;
            }

            int lastIndex = list.index;

            list.Add(new TraceItem {
                status = status,
                host   = host,
                domain = domain,
            });

            if (lastIndex > -1) {
                list.drawItemHandler(lastIndex, left, top, width);
            }

            list.drawItemHandler(list.items.Count - 1, left, top, width);

            Ansi.Push();
        }
    }
}
