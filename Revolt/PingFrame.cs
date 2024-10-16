namespace Revolt;
public sealed class PingFrame : UiFrame {
    private const char PING_CELL = '\x258C';

    private bool status = true;

    public UiToolbar toolbar;
    public UiList list;

    public PingFrame() {
        toolbar = new UiToolbar() { left = 1, right = 1 };
        list = new UiList();

        toolbar.items = [
            new UiToolbar.ToolbarItem() { text="Add",     action=Add },
            new UiToolbar.ToolbarItem() { text="Clear",   action=Clear },
            new UiToolbar.ToolbarItem() { text="Pause",   action=ToggleStatus },
            new UiToolbar.ToolbarItem() { text="Options", action=Options },
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
            Renderer.activeFrame = Renderer.mainMenuFrame;
            Renderer.Redraw();
            break;

        default:
            focusedElement.HandleKey(key);
            break;
        }

        return true;
    }

    private void Add() { }
    
    private void Clear() { }

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