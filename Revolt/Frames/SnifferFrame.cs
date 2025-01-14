using System.Collections.Generic;
using System.Net;
using Revolt.Proprietary;
using Revolt.Protocols;
using System.Threading;
using SharpPcap;
using Revolt.Sniff;

namespace Revolt.Frames;

internal class SnifferFrame : Tui.Frame {

    public static SnifferFrame Instance { get; } = new SnifferFrame();

    public Tui.Toolbar toolbar;

    private ICaptureDevice captureDevice;
    private Sniffer sniffer;
    private bool analyzeL2 = true;
    private bool analyzeL3 = true;
    private bool analyzeL4 = true;

    public SnifferFrame() {
        toolbar = new Tui.Toolbar(this) {
            left = 0,
            right = 0,
            items = [
                new Tui.Toolbar.ToolbarItem() { text="Start",  key="F2", action=Start},
                //new Tui.Toolbar.ToolbarItem() { text="Filter", key="F4", action=Start},
            ],
        };

        elements.Add(toolbar);

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

        case ConsoleKey.F2:
            Start();
            break;

        case ConsoleKey.F4:
            //TODO: implement
            break;

        default:
            focusedElement.HandleKey(key);
            break;
        }

        return true;
    }

    public override void Draw(int width, int height) {
        base.Draw(width, height);
    }

    private void Start() {
        if (captureDevice is not null && captureDevice.Started) {
            Stop();
            return;
        }

        StartDialog dialog = new StartDialog();

        dialog.okButton.action = () => {
            captureDevice = dialog.devices[dialog.rangeSelectBox.index];
            analyzeL2 = dialog.l2Toggle.Value;
            analyzeL3 = dialog.l3Toggle.Value;
            analyzeL4 = dialog.l4Toggle.Value;

            dialog.Close();

            try {
                sniffer = new Revolt.Sniff.Sniffer(captureDevice) {
                    analyzeL2 = dialog.l2Toggle.Value,
                    analyzeL3 = dialog.l3Toggle.Value,
                    analyzeL4 = dialog.l4Toggle.Value,
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

        dialog.l2Toggle.Value = analyzeL2;
        dialog.l3Toggle.Value = analyzeL3;
        dialog.l4Toggle.Value = analyzeL4;
    }

    private void Stop() {
        Tui.ConfirmDialog dialog = new Tui.ConfirmDialog() {
            text = "Are you sure you want to stop?"
        };

        dialog.okButton.action = () => {
            captureDevice?.StopCapture();
            captureDevice?.Close();
            sniffer?.Dispose();

            toolbar.items[0].text = "Start";

            dialog.Close();
        };

        dialog.Show();
    }

}

file sealed class StartDialog : Tui.DialogBox {
    public Tui.SelectBox rangeSelectBox;
    public Tui.Toggle    l2Toggle;
    public Tui.Toggle    l3Toggle;
    public Tui.Toggle    l4Toggle;

    public List<ILiveDevice> devices;

    public StartDialog() {
        CaptureDeviceList captureDevices = CaptureDeviceList.Instance;

        devices = new List<ILiveDevice>();
        List<string> strings = new List<string>();

        for (int i = 0; i < captureDevices.Count; i++) {
            if (captureDevices[i].MacAddress is null) continue;
            devices.Add(captureDevices[i]);
            strings.Add(captureDevices[i].Description);
        }

        okButton.text = "  Start  ";

        rangeSelectBox = new Tui.SelectBox(this) {
            options = strings.ToArray()
        };

        l2Toggle = new Tui.Toggle(this, "Analyze frames header");
        l3Toggle = new Tui.Toggle(this, "Analyze packets header");
        l4Toggle = new Tui.Toggle(this, "Analyze segments header");

        elements.Add(rangeSelectBox);

        elements.Add(l2Toggle);
        elements.Add(l3Toggle);
        elements.Add(l4Toggle);

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

        WriteLabel("NIC:", left, ++top, width);
        rangeSelectBox.left = left + 6;
        rangeSelectBox.right = Renderer.LastWidth - width - left + 2;
        rangeSelectBox.top = top++ - 1;

        Ansi.SetCursorPosition(left, top++);
        Ansi.Write(blank);

        l2Toggle.left = left;
        l2Toggle.top = top - 1;
        Ansi.SetCursorPosition(left, top++);
        Ansi.Write(blank);

        l3Toggle.left = left;
        l3Toggle.top = top - 1;
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