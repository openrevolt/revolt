using System.Runtime.InteropServices;
using System.Text;

using static Revolt.Sniff.Sniffer;

namespace Revolt.Sniff;

public sealed partial class Sniffer {
    public readonly struct Packet(
        long timestamp, ushort size,
        Mac sourceMac, Mac destinationMac, ushort networkProtocol,
        IP sourceIP, IP destinationIP, byte ttl, byte transportProtocol) {

        public readonly long   timestamp         = timestamp;
        public readonly ushort size              = size;
        public readonly Mac    sourceMac         = sourceMac;
        public readonly Mac    destinationMac    = destinationMac;
        public readonly ushort networkProtocol   = networkProtocol;
        public readonly IP     sourceIP          = sourceIP;
        public readonly IP     destinationIP     = destinationIP;
        public readonly byte   ttl               = ttl;
        public readonly byte   transportProtocol = transportProtocol;
    }

    public readonly struct Segment (FourTuple fourTuple, uint initialSequence, uint seqNumber, uint ackNumber, uint window) {
        public readonly FourTuple fourTuple       = fourTuple;
        public readonly uint      initialSequence = initialSequence;
        public readonly uint      seqNumber       = seqNumber;
        public readonly uint      ackNumber       = ackNumber;
        public readonly uint      window          = window;
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
