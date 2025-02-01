using System.Net;
using System.Runtime.InteropServices;

namespace Revolt.Sniff;

public sealed partial class Sniffer {

    private static readonly char[]  macLookup = "0123456789ABCDEF".ToCharArray();

    [StructLayout(LayoutKind.Explicit)]
    public struct Mac {
        [FieldOffset(0)] public ulong value;
        [FieldOffset(5)] public byte a;
        [FieldOffset(4)] public byte b;
        [FieldOffset(3)] public byte c;
        [FieldOffset(2)] public byte d;
        [FieldOffset(1)] public byte e;
        [FieldOffset(0)] public byte f;

        public static Mac Parse(byte[] buffer, int offset) {
            Mac mac = default;
            mac.value =
                ((ulong)buffer[offset]     << 40) |
                ((ulong)buffer[offset + 1] << 32) |
                ((ulong)buffer[offset + 2] << 24) |
                ((ulong)buffer[offset + 3] << 16) |
                ((ulong)buffer[offset + 4] << 8) |
                 (ulong)buffer[offset + 5];

            return mac;
        }

        public string ToFormattedString() {
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

        public bool IsEthernetMulticast() =>
            (value & 0xffffff000000) == 0x01_80_c2_00_00_00;

        public bool IsPVv4Multicast() =>
            (value & 0xffffff000000) == 0x01_00_5e_00_00_00;

        public bool IsPVv6Multicast() =>
            (value & 0xffff00000000) == 0x33_33_00_00_00_00;

        public bool IsUnicast() =>
            (value & 0x010000000000) == 0x00;

        public bool IsLocallyAdministered() =>
            (value & 0x020000000000) != 0x00;
    }

    public readonly struct Frame {
        public long      timestamp { init; get; }
        public ushort    size { init; get; }

        public Mac       sourceMac { init; get; }
        public Mac       destinationMac { init; get; }
        public ushort    networkProtocol { init; get; }

        public IPAddress sourceIP { init; get; }
        public IPAddress destinationIP { init; get; }
        public byte      ttl { init; get; }
        public byte      transportProtocol { init; get; }

        public ushort    sourcePort { init; get; }
        public ushort    destinationPort { init; get; }
        public uint      initialSequence { init; get;  }
        public uint      seqNumber { init; get; }
        public uint      ackNumber { init; get; }
        public uint      window { init; get; }
    }

    public readonly struct SniffOverviewItem {

    }

    public readonly struct SniffIssuesItem {

    }

    public class TrafficData {
        public long bytesRx;
        public long bytesTx;
        public long packetsRx;
        public long packetsTx;
        public long lastActivity;
    }

    public class Count {
        public long bytes;
        public long packets;
    }
}
