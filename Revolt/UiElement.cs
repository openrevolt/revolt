namespace Revolt;

public abstract class UiElement(UiFrame parentFrame) {
    public float left = 0, right = 0, top = 0, bottom = 0;
    public readonly UiFrame parentFrame = parentFrame;

    protected bool isFocused = false;

    protected (int, int, int, int) GetBounding() {
        int left   = this.left < 1 ? (int)(Renderer.LastWidth * this.left) : (int)this.left;
        int right  = this.right < 1 ? (int)(Renderer.LastWidth * this.right) : (int)this.right;
        int top    = this.top < 1 ? (int)(Renderer.LastHeight * this.top) : (int)this.top;
        int bottom = this.bottom < 1 ? (int)(Renderer.LastHeight * this.bottom) : (int)this.bottom;
        int width  = Renderer.LastWidth - left - right;
        int height = Renderer.LastHeight - top - bottom;
        return (left + 1, top + 1, width, height);
    }

    public abstract void Draw();

    public abstract void HandleKey(ConsoleKeyInfo key);

    public virtual void Focus() {
        isFocused = true;
        Draw();
    }

    public virtual void Blur() {
        isFocused = false;
        Draw();
    }

}