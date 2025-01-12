using System.Collections.Generic;

namespace Revolt.Frames;

internal class SnifferFrame : Tui.Frame {

    public static SnifferFrame Instance { get; } = new SnifferFrame();

    public Tui.Toolbar toolbar;

    public SnifferFrame() {
        toolbar = new Tui.Toolbar(this) {
            left = 0,
            right = 0,
            items = [
                new Tui.Toolbar.ToolbarItem() { text="Start", key="F2", action=Start},
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

        default:
            focusedElement.HandleKey(key);
            break;
        }

        return true;
    }

    public override void Draw(int width, int height) {
        base.Draw(width, height);
    }

    private void Start() { }
    private void Stop() { }

}