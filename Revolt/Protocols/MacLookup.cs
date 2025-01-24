using Revolt;

namespace Revolt.Protocols;

public static partial class MacLookup {

#if DEBUG
    private static readonly (byte, byte, string)[][] table = [];
#endif

    public static string Lookup(string mac) {
        if (table.Length == 0) return String.Empty;

        mac = mac.Replace("-", "").Replace(":", "").Replace(" ", "").Replace(".", "");
        if (!byte.TryParse(mac[0..2], System.Globalization.NumberStyles.HexNumber, null, out byte a)) return String.Empty;
        if (!byte.TryParse(mac[2..4], System.Globalization.NumberStyles.HexNumber, null, out byte b)) return String.Empty;
        if (!byte.TryParse(mac[4..6], System.Globalization.NumberStyles.HexNumber, null, out byte c)) return String.Empty;

        return Lookup(a, b, c);
    }

    public static string Lookup(Sniff.Sniffer.Mac mac) {
        if (table.Length == 0) return String.Empty;

        return Lookup(mac.a, mac.b, mac.c);
    }

    private static string Lookup(byte a, byte b, byte c) {

        (byte, byte, string)[] subArray = table[a];

        if (subArray == null || subArray.Length == 0) return string.Empty;

        int start = 0;
        int end = subArray.Length - 1;

        while (start <= end) {
            int mid = (start + end) / 2;
            (byte midB, byte midC, string vendor) = subArray[mid];

            if (midB == b && midC == c) return vendor;

            if (midB < b || (midB == b && midC < c)) {
                start = mid + 1;
            }
            else {
                end = mid - 1;
            }
        }

        //not found
        return string.Empty;
    }
}
