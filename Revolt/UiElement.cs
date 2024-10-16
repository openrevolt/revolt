namespace Revolt;

public abstract class UiElement {
    public float left = 0, right = 0, top = 0, bottom = 0;

    public abstract void Draw();

    public abstract void HandleKey(ConsoleKeyInfo key);

    public abstract void Focus();

    public abstract void Blur();

}