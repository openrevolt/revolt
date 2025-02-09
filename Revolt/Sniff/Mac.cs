using System.Runtime.InteropServices;

namespace Revolt.Sniff;

public sealed partial class Sniffer {
    private static readonly char[]  macLookup = "0123456789ABCDEF".ToCharArray();

    [StructLayout(LayoutKind.Explicit)]
    public readonly struct Mac(byte[] buffer, int offset) {
        [FieldOffset(0)] public readonly ulong value;
        [FieldOffset(5)] public readonly byte a = buffer[offset];
        [FieldOffset(4)] public readonly byte b = buffer[offset + 1];
        [FieldOffset(3)] public readonly byte c = buffer[offset + 2];
        [FieldOffset(2)] public readonly byte d = buffer[offset + 3];
        [FieldOffset(1)] public readonly byte e = buffer[offset + 4];
        [FieldOffset(0)] public readonly byte f = buffer[offset + 5];

        public override string ToString() {
            Span<char> buffer = stackalloc char[17];
            buffer[0] = macLookup[(a >> 4) & 0xF];
            buffer[1] = macLookup[a & 0xF];
            buffer[2] = ':';
            buffer[3] = macLookup[(b >> 4) & 0xF];
            buffer[4] = macLookup[b & 0xF];
            buffer[5] = ':';
            buffer[6] = macLookup[(c >> 4) & 0xF];
            buffer[7] = macLookup[c & 0xF];
            buffer[8] = ':';
            buffer[9] = macLookup[(d >> 4) & 0xF];
            buffer[10] = macLookup[d & 0xF];
            buffer[11] = ':';
            buffer[12] = macLookup[(e >> 4) & 0xF];
            buffer[13] = macLookup[e & 0xF];
            buffer[14] = ':';
            buffer[15] = macLookup[(f >> 4) & 0xF];
            buffer[16] = macLookup[f & 0xF];
            return new string(buffer);
        }

        public bool IsBroadcast() =>
            value == 0xffffffffffff;

        public bool IsMulticast() =>
            (value & 0x010000000000) != 0x00;

        public bool IsUnicast() =>
            (value & 0x010000000000) == 0x00;

        public bool IsEthernetMulticast() =>
            (value & 0xffffff000000) == 0x01_80_c2_00_00_00;

        public bool IsPVv4Multicast() =>
            (value & 0xffffff000000) == 0x01_00_5e_00_00_00;

        public bool IsPVv6Multicast() =>
            (value & 0xffff00000000) == 0x33_33_00_00_00_00;


        public bool IsLocallyAdministered() =>
            (value & 0x020000000000) != 0x00;
    }
}
