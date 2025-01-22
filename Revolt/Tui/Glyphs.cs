﻿namespace Revolt.Tui;

public static class Glyphs {
    public static readonly byte[] LIGHT_COLOR     = [192, 192, 192];
    public static readonly byte[] DARK_COLOR      = [16, 16, 16];
    public static readonly byte[] FOCUS_COLOR     = [255, 192, 0];
    public static readonly byte[] HIGHLIGHT_COLOR = [48, 48, 48];
    public static readonly byte[] RED_COLOR       = [224, 48, 0];

    public static readonly byte[] PANE_COLOR         = [32, 32, 32];
    public static readonly byte[] TOOLBAR_COLOR      = [56, 56, 56];
    public static readonly byte[] CONTROL_COLOR      = [72, 72, 72];
    public static readonly byte[] INPUT_COLOR        = [96, 96, 96];
    public static readonly byte[] DIALOG_COLOR       = [160, 160, 160];

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

    public const char TOGGLE_BOX = '\u25A0';

    //public const char PING_CELL = '\x258C';
    public const char PING_CELL = '\x25A0';
}