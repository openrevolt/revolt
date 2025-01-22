using System.Net;
using System.Runtime.InteropServices;

namespace Revolt.Sniff;

public sealed partial class Sniffer {

    [StructLayout(LayoutKind.Explicit)]
    public struct Mac {
        [FieldOffset(0)] public ulong value;
        [FieldOffset(0)] public byte a;
        [FieldOffset(1)] public byte b;
        [FieldOffset(2)] public byte c;
        [FieldOffset(3)] public byte d;
        [FieldOffset(4)] public byte e;
        [FieldOffset(5)] public byte f;

        public static Mac Parse(byte[] buffer, int offset) {
            Mac mac = default;
            unsafe {
                fixed (byte* p = &buffer[offset]) {
                    Buffer.MemoryCopy(p, &mac, 6, 6);
                }
            }
            return mac;
        }
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

    public class TrafficData {
        public long bytesRx;
        public long bytesTx;
        public long packetsRx;
        public long packetsTx;
    }

}

public static class MacExtension {
    private static readonly char[]  macLookup = "0123456789ABCDEF".ToCharArray();
    public static string ToFormattedString(this Sniff.Sniffer.Mac mac) {
        Span<char> buffer = stackalloc char[17];

        buffer[0] = macLookup[(mac.a >> 4) & 0xF];
        buffer[1] = macLookup[mac.a & 0xF];
        buffer[2] = ':';
        buffer[3] = macLookup[(mac.b >> 4) & 0xF];
        buffer[4] = macLookup[mac.b & 0xF];
        buffer[5] = ':';
        buffer[6] = macLookup[(mac.c >> 4) & 0xF];
        buffer[7] = macLookup[mac.c & 0xF];
        buffer[8] = ':';
        buffer[9] = macLookup[(mac.d >> 4) & 0xF];
        buffer[10] = macLookup[mac.d & 0xF];
        buffer[11] = ':';
        buffer[12] = macLookup[(mac.e >> 4) & 0xF];
        buffer[13] = macLookup[mac.e & 0xF];
        buffer[14] = ':';
        buffer[15] = macLookup[(mac.f >> 4) & 0xF];
        buffer[16] = macLookup[mac.f & 0xF];

        return new string(buffer);
    }
}