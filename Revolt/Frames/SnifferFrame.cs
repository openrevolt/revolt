namespace Revolt.Frames;

internal class SnifferFrame : Tui.Frame {

    public static SnifferFrame Instance { get; } = new SnifferFrame();

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
            MainMenu.Instance.Show();
            break;

        default:
            focusedElement.HandleKey(key);
            break;
        }

        return true;
    }

}