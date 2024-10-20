﻿namespace Revolt.Frames;

public sealed class PingFrame : Ui.Frame {
    public struct PingItem {
        public string  host;
        public short   status;
        public short[] history;
    }

    private bool status = true;

    public Ui.Toolbar toolbar;
    public Ui.ListBox<PingItem> list;

    public static readonly PingFrame singleton;
    static PingFrame() {
        singleton = new PingFrame();
    }

    public PingFrame() {
        toolbar = new Ui.Toolbar(this) { left = 1, right = 1 };

        list = new Ui.ListBox<PingItem>(this) {
            top = 3,
            drawItemHandler = DrawPingItem
        };

        toolbar.items = [
        new Ui.Toolbar.ToolbarItem() { text="Add",     action=Add },
        new Ui.Toolbar.ToolbarItem() { text="Clear",   action=Clear },
        new Ui.Toolbar.ToolbarItem() { text="Pause",   action=ToggleStatus },
        new Ui.Toolbar.ToolbarItem() { text="Options", action=Options },
        ];

        elements.Add(toolbar);
        elements.Add(list);

        defaultElement = toolbar;
        FocusNext();
    }

    public override void Draw(int width, int height) {
        base.Draw(width, height);
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
            Renderer.ActiveFrame = MainMenu.singleton;
            Renderer.Redraw();
            break;

        default:
            focusedElement.HandleKey(key);
            break;
        }

        return true;
    }

    private void DrawPingItem(int i, int x, int y, int width) {
        if (list.items is null || list.items.Count == 0) return;
        if (i >= list.items.Count) return;

        PingItem item = list.items[i];

        Ansi.SetCursorPosition(x, y + i);
        Ansi.SetFgColor(Data.FG_COLOR);
        Ansi.SetBgColor(Data.BG_COLOR);
        Console.Write(item.host);
    }

    private void Add() {
        Ui.InputDialog dialog = new Ui.InputDialog() {
            text = "Enter IP, domain or hostname:"
        };

        dialog.okButton.action = () => {
            AddItem(dialog.valueTextbox.Value);
            dialog.Close();
        };

        Renderer.Dialog = dialog;
        dialog.Draw();
    }

    private void AddItem(string host) {
        if (host is null) return;

        list.Add(new PingItem {
            host = host,
            status = 0,
            history = new short[50]
        });
    }

    private void Clear() {
        list.items.Clear();
        list.Draw();
    }

    private void ToggleStatus() {
        status = !status;

        if (status) {
            toolbar.items[2].text = "Pause";
        }
        else {
            toolbar.items[2].text = "Start";
        }

        toolbar.Draw();
    }

    private void Options() { }

}