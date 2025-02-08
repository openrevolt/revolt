namespace Revolt.Tui;

public static class Glyphs {
    public static readonly Ansi.Color DARK_COLOR      = new Ansi.Color(0, 0, 0);
    public static readonly Ansi.Color DARKGRAY_COLOR  = new Ansi.Color(16, 16, 16);
    public static readonly Ansi.Color DIMGRAY_COLOR   = new Ansi.Color(64, 64, 64);
    public static readonly Ansi.Color GRAY_COLOR      = new Ansi.Color(128, 128, 128);
    public static readonly Ansi.Color LIGHT_COLOR     = new Ansi.Color(192, 192, 192);
    public static readonly Ansi.Color WHITE_COLOR     = new Ansi.Color(255, 255, 255);
    public static readonly Ansi.Color FOCUS_COLOR     = new Ansi.Color(255, 192, 0);
    public static readonly Ansi.Color HIGHLIGHT_COLOR = new Ansi.Color(48, 48, 48);
    public static readonly Ansi.Color RED_COLOR       = new Ansi.Color(224, 48, 0);

    public static readonly Ansi.Color PANE_COLOR      = new Ansi.Color(32, 32, 32);
    public static readonly Ansi.Color TOOLBAR_COLOR   = new Ansi.Color(56, 56, 56);
    public static readonly Ansi.Color CONTROL_COLOR   = new Ansi.Color(72, 72, 72);
    public static readonly Ansi.Color INPUT_COLOR     = new Ansi.Color(96, 96, 96);
    public static readonly Ansi.Color DIALOG_COLOR    = new Ansi.Color(160, 160, 160);

    public static ushort BRAILLE_BASE = 0x2800;

    public const char ELLIPSIS = '\u2026';

    public const char UPPER_1_8TH_BLOCK = '\u2594';
    public const char UPPER_4_8TH_BLOCK = '\u2580';

    public const char LOWER_1_8TH_BLOCK = '\u2581';
    public const char LOWER_2_8TH_BLOCK = '\u2582';
    public const char LOWER_3_8TH_BLOCK = '\u2583';
    public const char LOWER_4_8TH_BLOCK = '\u2584';
    public const char LOWER_5_8TH_BLOCK = '\u2585';
    public const char LOWER_6_8TH_BLOCK = '\u2586';
    public const char LOWER_7_8TH_BLOCK = '\u2587';
    public const char LOWER_8_8TH_BLOCK = '\u2588';

    public const char ARROW_UP = '\u2191';
    public const char ARROW_DOWN = '\u2193';

    public const char LEFT_TRIANGLE  = '\u25C2';
    public const char RIGHT_TRIANGLE = '\u25B8';
    
    public const char BULLET = '\u25CF';

    public const char TOGGLE_BOX = '\u25A0';

    public const char PING_CELL = '\x25A0';
}