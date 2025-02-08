using System.Runtime.InteropServices;
using System.Text;
using static Revolt.Sniff.Sniffer;

namespace Revolt.Sniff;

public sealed partial class Sniffer {

    private static readonly char[]  macLookup = "0123456789ABCDEF".ToCharArray();

    [StructLayout(LayoutKind.Explicit)]
    public readonly struct Mac {
        [FieldOffset(0)] public readonly ulong value;
        [FieldOffset(5)] public readonly byte a;
        [FieldOffset(4)] public readonly byte b;
        [FieldOffset(3)] public readonly byte c;
        [FieldOffset(2)] public readonly byte d;
        [FieldOffset(1)] public readonly byte e;
        [FieldOffset(0)] public readonly byte f;

        public Mac(byte[] buffer, int offset) {
            a = buffer[offset];
            b = buffer[offset + 1];
            c = buffer[offset + 2];
            d = buffer[offset + 3];
            e = buffer[offset + 4];
            f = buffer[offset + 5];
        }

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

    [StructLayout(LayoutKind.Explicit)]
    public readonly struct IP {
        [FieldOffset(0)]  public readonly uint    ipv4;
        [FieldOffset(0)]  public readonly UInt128 ipv6;
        [FieldOffset(16)] public readonly bool    isIPv6;

#if BIGENDIAN
        [FieldOffset(0)] private readonly ulong upper;
        [FieldOffset(8)] private readonly ulong lower;
#else
        [FieldOffset(0)] private readonly ulong lower;
        [FieldOffset(8)] private readonly ulong upper;
#endif

        public IP(ReadOnlySpan<byte> bytes) {
            if (bytes.Length == 4) {
                ipv4 = (uint)((bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3]);
                isIPv6 = false;
            }
            else if (bytes.Length == 16) {
                ulong high = (ulong)bytes[0] << 56 | (ulong)bytes[1] << 48 | (ulong)bytes[2] << 40 | (ulong)bytes[3] << 32 |
                     (ulong)bytes[4] << 24 | (ulong)bytes[5] << 16 | (ulong)bytes[6] << 8  | (ulong)bytes[7];

                ulong low  = (ulong)bytes[8] << 56  | (ulong)bytes[9] << 48  | (ulong)bytes[10] << 40 | (ulong)bytes[11] << 32 |
                     (ulong)bytes[12] << 24 | (ulong)bytes[13] << 16 | (ulong)bytes[14] << 8  | (ulong)bytes[15];

                ipv6 = new UInt128(high, low);
                isIPv6 = true;
            }
            else {
                throw new ArgumentException("Invalid IP address length.");
            }
        }

        public static bool operator ==(IP left, IP right) {
            if (left.isIPv6 != right.isIPv6) return false;
            return left.isIPv6 ? left.ipv6 == right.ipv6 : left.ipv4 == right.ipv4;
        }

        public static bool operator !=(IP left, IP right) {
            if (left.isIPv6 != right.isIPv6) return true;
            return left.isIPv6 ? left.ipv6 != right.ipv6 : left.ipv4 != right.ipv4;
        }

        public override bool Equals(object obj) =>
            obj is IP other && this == other;

        public override int GetHashCode() {
            unchecked {
                return isIPv6 ? ipv6.GetHashCode() : (int)ipv4;
            }
        }

        public override string ToString() =>
            isIPv6? IPv6ToString() : IPv4ToString();

        private string IPv4ToString() {
            Span<char> buffer = stackalloc char[15];
            int pos = 0;

            (ipv4 >> 24 & 0xFF).TryFormat(buffer[pos..], out int written);
            pos += written;
            buffer[pos++] = '.';

            (ipv4 >> 16 & 0xFF).TryFormat(buffer[pos..], out written);
            pos += written;
            buffer[pos++] = '.';

            (ipv4 >> 8 & 0xFF).TryFormat(buffer[pos..], out written);
            pos += written;
            buffer[pos++] = '.';

            (ipv4 & 0xFF).TryFormat(buffer[pos..], out written);
            pos += written;

            return new string(buffer[..pos]);
        }

        private string IPv6ToString() {
            Span<ushort> segments = stackalloc ushort[8];
            ulong high = upper, low = lower;

            for (int i = 0; i < 4; i++) {
                segments[i] = (ushort)(high >> (48 - i * 16));
                segments[i + 4] = (ushort)(low >> (48 - i * 16));
            }

            int bestStart = -1, bestLength = 0, currentStart = -1, currentLength = 0;

            for (int i = 0; i < 8; i++) {
                if (segments[i] == 0) {
                    if (currentStart == -1) {
                        currentStart = i;
                    }
                    currentLength++;
                }
                else {
                    if (currentLength > bestLength) {
                        bestStart = currentStart;
                        bestLength = currentLength;
                    }
                    currentStart = -1;
                    currentLength = 0;
                }
            }
            if (currentLength > bestLength) {
                bestStart = currentStart;
                bestLength = currentLength;
            }

            Span<char> buffer = stackalloc char[39];
            int index = 0;

            for (int i = 0; i < 8; i++) {
                if (i == bestStart) {
                    buffer[index++] = ':';
                    i += bestLength - 1;
                    continue;
                }

                if (index > 0) buffer[index++] = ':';
                segments[i].TryFormat(buffer[index..], out int written, format: "x");
                index += written;
            }

            return new string(buffer[..index]);
        }

        public bool IsBroadcast() {
            if (isIPv6) return false;
            return ipv4 == 0xffffffff;
        }

        public bool IsMulticast() {
            if (isIPv6) {
                return (upper & 0xff00000000000000) == 0xff00000000000000;
            }
            return (ipv4 & 0xf0000000) == 0xe0000000;
        }

        public bool IsLoopback() {
            if (isIPv6) return ipv6 == 1 || IsIPv4MappedIPv6() && (ipv4 & 0xff000000) == 0x7f000000;
            return (ipv4 & 0xff000000) == 0x7f000000;
        }

        public bool IsApipa() {
            if (isIPv6) return false;
            return (ipv4 & 0xffff0000) == 0xa9fe0000;
        }

        public bool IsIPv6LinkLocal() {
            if (!isIPv6) return false;
            return (upper & 0xffff000000000000) == 0xfe80000000000000;
        }

        public bool IsIPv6Teredo() {
            if (!isIPv6) return false;
            return (upper & 0xffffffff00000000) == (0x2001000000000000UL); ;
        }

        public bool IsIPv6UniqueLocal() {
            if (!isIPv6) return false;
            return (upper & 0xfe00000000000000) == 0xfc00000000000000;
        }

        public bool IsIPv6SiteLocal() {
            if (!isIPv6) return false;
            return (upper & 0xffc0000000000000) == 0xfec0000000000000;
        }

        public bool IsIPv4MappedIPv6() {
            return upper == 0x0000000000000000FFFF000000000000;
        }

        public bool IsIPv4Private() {
            if (isIPv6) return false;
            return (ipv4 & 0xff000000) == 0x0a000000 || (ipv4 & 0xfff00000) == 0xac100000 || (ipv4 & 0xffff0000) == 0xc0a80000;
        }
    }

    public readonly struct Packet(
        long timestamp, ushort size,
        Mac sourceMac, Mac destinationMac, ushort networkProtocol,
        IP sourceIP, IP destinationIP, byte ttl, byte transportProtocol) {

        public long   Timestamp { get; } = timestamp;
        public ushort Size { get; } = size;
        public Mac    SourceMac { get; } = sourceMac;
        public Mac    DestinationMac { get; } = destinationMac;
        public ushort NetworkProtocol { get; } = networkProtocol;
        public IP     SourceIP { get; } = sourceIP;
        public IP     DestinationIP { get; } = destinationIP;
        public byte   Ttl { get; } = ttl;
        public byte   TransportProtocol { get; } = transportProtocol;
    }

    public readonly struct Segment {
        public uint initialSequence { init; get; }
        public uint seqNumber { init; get; }
        public uint ackNumber { init; get; }
        public uint window { init; get; }
        //public ushort SourcePort { get; }
        //public ushort DestinationPort { get; }
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
