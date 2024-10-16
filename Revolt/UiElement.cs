namespace Revolt;

public abstract class UiElement {
    public int[] position;

    public virtual void Draw() {

    }

    public abstract void Focus();

    public abstract void Blur();
}