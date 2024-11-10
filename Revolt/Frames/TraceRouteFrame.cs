using System.Net.NetworkInformation;

namespace Revolt.Frames;

public sealed class TraceRouteFrame : Ui.Frame {
    public struct TraceItem {
        public string host;
        public bool status;
        public int rtt;
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

        //DrawProgressBar(width, 0, 1);

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

        int hostWidth = Math.Max(width - 14, 1);

        Ansi.Write(item.host.Length > hostWidth ? item.host[..(hostWidth - 1)] + Data.ELLIPSIS : item.host.PadRight(hostWidth));

        Ansi.Write(' ');

        if (item.rtt >= 0) {
            Ansi.Write($"{item.rtt}ms".PadLeft(8));
        }
        else {
            Ansi.Write(new String(' ', 8));
        }

        Ansi.SetBgColor(Data.BG_COLOR);
    }

    private static void DrawProgressBar(int width, int progress, int totalSteps) {
        Ansi.SetCursorPosition(0, 7);
        Ansi.SetBgColor(Data.BG_COLOR);
        Ansi.Write(new String(' ', (width - 30) / 2));

        int v = 30 * progress / totalSteps;

        Ansi.SetBgColor(Data.SELECT_COLOR);
        Ansi.Write(new String(' ', v));

        Ansi.SetBgColor(Data.SELECT_COLOR_LIGHT);
        Ansi.Write(new String(' ', 30 - v));

        Ansi.SetBgColor(Data.BG_COLOR);
        Ansi.Write(new String(' ', (width - 30) / 2));
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
                string value = textbox.Value.Trim();
                textbox.Value = String.Empty;
                if (String.IsNullOrEmpty(value)) break;
                list.Clear();
                textbox.history.Add(value);
                Trace(value);
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

    private void Clear() {
        list.Clear();
    }

    private void Trace(string target) {
        const int timeout = 1_000;
        const int maxHops = 30;

        (int left, int top, int width, _) = list.GetBounding();

        DrawProgressBar(Renderer.LastWidth, 0, maxHops);
        Ansi.Push();

        string lastAddress = String.Empty;
        using Ping ping = new Ping();
        for (int ttl = 1; ttl <= maxHops; ttl++) {
            DrawProgressBar(Renderer.LastWidth, ttl, maxHops);
            Ansi.Push();

            string host;
            bool status;
            int rtt;

            try {
                PingReply reply = ping.Send(target, timeout, Protocols.Icmp.ICMP_PAYLOAD, new PingOptions(ttl, true));
                if (reply is null) break;

                status = reply.Status == IPStatus.TtlExpired || reply.Status == IPStatus.Success;

                if (reply.Status == IPStatus.Success || reply.Status == IPStatus.TtlExpired) {
                    if (lastAddress == reply.Address.ToString()) {
                        break;
                    }
                    else {
                        lastAddress = reply.Address.ToString();
                    }

                    host = reply.Address?.ToString() ?? "unknown";
                    rtt = status ? (int)reply.RoundtripTime : -1;
                }
                else if (reply.Status == IPStatus.TimedOut) {
                    host = "timed out";
                    rtt = -1;
                }
                else {
                    break;
                }
            }
            catch (Exception) {
                host = "timed out";
                status = false;
                rtt = -1;
                break;
            }

            int lastIndex = list.index;

            list.Add(new TraceItem {
                host   = host,
                status = status,
                rtt    = rtt
            });

            if (lastIndex > -1) {
                list.drawItemHandler(lastIndex, left, top, width);
            }

            list.drawItemHandler(list.items.Count - 1, left, top, width);
        }

        DrawProgressBar(Renderer.LastWidth, maxHops, maxHops);
        Ansi.Push();

        textbox.Focus(true);
    }
}
