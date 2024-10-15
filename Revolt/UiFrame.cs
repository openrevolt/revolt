namespace Revolt;
public class UiFrame {

    public int[] rows;
    public int[] cols;
    public UiElement[] elements;
    public UiElement defaultElement;

    public UiFrame() {
        rows = [];
        cols = [];
        elements = [];
        defaultElement = null;
    }

}