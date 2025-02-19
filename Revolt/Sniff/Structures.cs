﻿namespace Revolt.Sniff;

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

    public readonly struct Segment(long timestamp, FourTuple fourTuple, uint sequenceNo, uint acknowledgmentNo, ushort flags, uint window, uint size) {
        public readonly long      timestamp        = timestamp;
        public readonly FourTuple fourTuple        = fourTuple;
        public readonly uint      sequenceNo       = sequenceNo;
        public readonly uint      acknowledgmentNo = acknowledgmentNo;
        public readonly ushort    flags            = flags;
        public readonly uint      window           = window;
        public readonly uint      size             = size;
    }

    public readonly struct SniffIssuesItem {

    }

    public sealed class TrafficData {
        public long bytesRx;
        public long bytesTx;
        public long packetsRx;
        public long packetsTx;
        public long lastActivity;
    }

    public sealed class Count {
        public long bytes;
        public long packets;
    }

    public sealed class StreamCount {
        public long total3wh;
        public long totalRtt;
        public long minRtt;
        public long maxRtt;

        public long totalSegments;
        public long totalBytes;

        public uint loss;
        public uint retransmission;
        public uint ooo;
        public uint checksumMismatch;
        public uint zeroWindowEvent;
    }
}
