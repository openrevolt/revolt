using System.Collections.Generic;
using SharpPcap;
using Revolt.Sniff;
using Revolt.Tui;

namespace Revolt.Frames;

internal class SnifferFrame : Tui.Frame {

    public static SnifferFrame Instance { get; } = new SnifferFrame();

    public Tui.Tabs tabs;
    public Tui.ListBox<string> list;
    public Tui.Toolbar toolbar;

    private ICaptureDevice captureDevice;
    private Sniffer sniffer;

    private bool includeSelfTraffic = true;
    private bool includePublicIPs   = true;
    private bool analyzeL4          = true;

    public SnifferFrame() {
        tabs = new Tui.Tabs(this) {
            left  = 1,
            right = 1,
            top   = 0,
            items = [
                new Tui.Tabs.TabItem() { text="L3",        key="3" },
                new Tui.Tabs.TabItem() { text="L4",        key="4" },
                new Tui.Tabs.TabItem() { text="Frames",    key="F" },
                new Tui.Tabs.TabItem() { text="Packets",   key="P" },
                new Tui.Tabs.TabItem() { text="Segments",  key="S" },
                new Tui.Tabs.TabItem() { text="Datagrams", key="D" },
                new Tui.Tabs.TabItem() { text="Overview",  key="O" },
                new Tui.Tabs.TabItem() { text="Issues",    key="I" },
            ]
        };

        list = new Tui.ListBox<string>(this) {
            left            = 1,
            right           = 1,
            top             = 3,
            bottom          = 1,
            backgroundColor = Glyphs.PANE_COLOR
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

    public override void Draw(int width, int height) {
        base.Draw(width, height);

        Ansi.SetCursorPosition(2, height - 1);
        Ansi.SetBgColor(Glyphs.DARK_COLOR);
        Ansi.SetFgColor(Glyphs.PANE_COLOR);
        Ansi.Write(new String(Glyphs.UPPER_4_8TH_BLOCK, width - 2));
        Ansi.Push();
    }

    private void StartDialog() {
        if (captureDevice is not null && captureDevice.Started) {
            StopDialog();
            return;
        }

        StartDialog dialog = new StartDialog();

        dialog.okButton.action = () => {
            captureDevice      = dialog.devices[dialog.rangeSelectBox.index];
            includeSelfTraffic = dialog.includeSelfToggle.Value;
            includePublicIPs   = dialog.includePublicToggle.Value;
            analyzeL4          = dialog.l4Toggle.Value;

            dialog.Close();

            try {
                sniffer = new Revolt.Sniff.Sniffer(captureDevice) {
                    includeSelfTraffic = dialog.includeSelfToggle.Value,
                    includePublicIPs   = dialog.includePublicToggle.Value,
                    analyzeL4          = dialog.l4Toggle.Value,
                };

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
        dialog.includeSelfToggle.Value   = includeSelfTraffic;
        dialog.includePublicToggle.Value = includePublicIPs;
        dialog.l4Toggle.Value            = analyzeL4;
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
    public Tui.Toggle    includeSelfToggle;
    public Tui.Toggle    includePublicToggle;
    public Tui.Toggle    l4Toggle;

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

        includeSelfToggle   = new Tui.Toggle(this, "Include self traffic");
        includePublicToggle = new Tui.Toggle(this, "Include public IPs");
        l4Toggle            = new Tui.Toggle(this, "Analyze Layer-4 header");

        elements.Add(rangeSelectBox);
        elements.Add(includeSelfToggle);
        elements.Add(includePublicToggle);
        elements.Add(l4Toggle);

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


        Ansi.SetCursorPosition(left, top++);
        Ansi.Write(blank);

        includeSelfToggle.left = left;
        includeSelfToggle.top = top - 1;
        Ansi.SetCursorPosition(left, top++);
        Ansi.Write(blank);


        Ansi.SetCursorPosition(left, top++);
        Ansi.Write(blank);

        includePublicToggle.left = left;
        includePublicToggle.top = top - 1;
        Ansi.SetCursorPosition(left, top++);
        Ansi.Write(blank);


        Ansi.SetCursorPosition(left, top++);
        Ansi.Write(blank);

        l4Toggle.left = left;
        l4Toggle.top = top - 1;
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