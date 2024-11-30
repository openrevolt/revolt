using System.Net;
using System.Net.Sockets;

namespace Revolt;

public static class Data {
    public static readonly byte[] FG_COLOR           = [192, 192, 192];
    public static readonly byte[] BG_COLOR           = [24, 24, 24];
    public static readonly byte[] TOOLBAR_COLOR      = [64, 64, 64];
    public static readonly byte[] CONTROL_COLOR      = [72, 72, 72];
    public static readonly byte[] INPUT_COLOR        = [96, 96, 96];
    public static readonly byte[] PANE_COLOR         = [160, 160, 160];
    public static readonly byte[] SELECT_COLOR       = [255, 192, 0];
    public static readonly byte[] SELECT_COLOR_LIGHT = [48, 48, 48];

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

    public const char LEFT_TRIANGLE  = '\u25C2';
    public const char RIGHT_TRIANGLE = '\u25B8';

    public const char TOGGLE_BOX = '\u25A0';
    
    public const char BIG_RIGHT_TRIANGLE = '\uE0B0';
    public const char BIG_LEFT_TRIANGLE = '\uE0B2';

    public const char PING_CELL = '\x258C';

    public static bool IsPrivate(this IPAddress address) {
        if (address.AddressFamily != AddressFamily.InterNetwork) return false;

        byte[] bytes = address.GetAddressBytes();
        return bytes[0] switch {
            10  => true,
            127 => true,
            172 => bytes[1] < 32 && bytes[1] >= 16,
            192 => bytes[1] == 168,
            _   => false,
        };
    }

    public static bool IsApipa(this IPAddress address) {
        if (address.AddressFamily != AddressFamily.InterNetwork) return false;
        byte[] bytes = address.GetAddressBytes();
        if (bytes[0] == 169 && bytes[1] == 254) return true;
        return false;
    }

    public static int SubnetMaskToCidr(IPAddress subnetMask) {
        byte[] bytes = subnetMask.GetAddressBytes();

        int cidr = 0;
        foreach (byte b in bytes) {
            cidr += CountBits(b);
        }

        return cidr;
    }
    private static int CountBits(byte b) {
        int count = 0;
        while (b != 0) {
            count += b & 1;
            b >>= 1;
        }
        return count;
    }
}