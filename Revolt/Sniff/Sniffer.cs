using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net.NetworkInformation;
using SharpPcap;

namespace Revolt.Sniff;

public sealed partial class Sniffer : IDisposable {
    public long totalPackets = 0, totalBytes = 0;

    public IndexedDictionary<Mac, TrafficData>    framesCount    = new IndexedDictionary<Mac, TrafficData>();
    public IndexedDictionary<IP, TrafficData>     packetCount    = new IndexedDictionary<IP, TrafficData>();
    public IndexedDictionary<ushort, TrafficData> segmentCount   = new IndexedDictionary<ushort, TrafficData>();
    public IndexedDictionary<ushort, TrafficData> datagramCount  = new IndexedDictionary<ushort, TrafficData>();
    public IndexedDictionary<ushort, Count>       etherTypeCount = new IndexedDictionary<ushort, Count>();
    public IndexedDictionary<byte, Count>         transportCount = new IndexedDictionary<byte, Count>();
    public IndexedDictionary<IPPair, StreamCount> tcpStatCount   = new IndexedDictionary<IPPair, StreamCount>();

    private readonly Tui.ListBox<SniffIssuesItem> issuesList;

    public ConcurrentDictionary<FourTuple, ConcurrentQueue<Segment>> streams = new ConcurrentDictionary<FourTuple, ConcurrentQueue<Segment>>();

    private ConcurrentDictionary<IP, Mac> macTable = new ConcurrentDictionary<IP, Mac>();
    private ICaptureDevice device;
    private const ushort maxPort = 1024; //49152

    //private ulong frameIndex = 0;
    //private ConcurrentDictionary<ulong, Packet> frames = new ConcurrentDictionary<ulong, Packet>();

    public Sniffer(ICaptureDevice device, Tui.ListBox<SniffIssuesItem> issuesList) {
        this.device = device;
        this.issuesList = issuesList;

        etherTypeCount.TryAdd(0x0800, new Count() { packets = 0, bytes = 0 });
        etherTypeCount.TryAdd(0x86DD, new Count() { packets = 0, bytes = 0 });

        transportCount.TryAdd(6, new Count() { packets = 0, bytes = 0 });
        transportCount.TryAdd(17, new Count() { packets = 0, bytes = 0 });
    }

    public void Start() {
        device.OnPacketArrival += Device_OnPacketArrival;
        device.Open();
        device.StartCapture();
    }

    public void Stop() {
        if (device is null) return;
        device!.StopCapture();
        device!.Close();
        device = null;
    }

    private void Device_OnPacketArrival(object sender, SharpPcap.PacketCapture e) {
        RawCapture rawPacket = e.GetPacket();
        byte[] buffer = rawPacket.Data;
        HandleEthernetFrame(buffer, rawPacket.Timeval.Date.Ticks);
    }

    private void HandleEthernetFrame(byte[] buffer, long timestamp) {
        Mac       sourceMac       = new Mac(buffer, 0);
        Mac       destinationMac  = new Mac(buffer, 6);
        ushort    networkProtocol = (ushort)(buffer[12] << 8 | buffer[13]);

        switch ((EtherType)networkProtocol) {
        case EtherType.IPv4:
            HandleIPv4(buffer, timestamp, sourceMac, destinationMac);
            break;

        case EtherType.IPv6:
            HandleIPv6(buffer, timestamp, sourceMac, destinationMac);
            break;
        }

        framesCount.AddOrUpdate(
            sourceMac,
            new TrafficData() { bytesRx=0, bytesTx=buffer.Length, packetsRx=0, packetsTx=1, lastActivity=timestamp },
            (mac, traffic) => {
                Interlocked.Add(ref traffic.bytesTx, buffer.Length);
                Interlocked.Increment(ref traffic.packetsTx);
                traffic.lastActivity = timestamp;
                return traffic;
            }
        );

        framesCount.AddOrUpdate(
            destinationMac,
            new TrafficData() { bytesRx= buffer.Length, bytesTx=0, packetsRx=1, packetsTx=0, lastActivity = timestamp },
            (mac, traffic) => {
                Interlocked.Add(ref traffic.bytesRx, buffer.Length);
                Interlocked.Increment(ref traffic.packetsRx);
                traffic.lastActivity = timestamp;
                return traffic;
            }
        );

        /*Packet frame = new Packet(
            timestamp,
            l3Size,

            sourceMac,
            destinationMac,
            networkProtocol,

            sourceIP,
            destinationIP,
            ttl,
            transportProtocol
        );

        frames.TryAdd(frameIndex, frame);
        Interlocked.Increment(ref frameIndex);*/

        Interlocked.Increment(ref totalPackets);
        Interlocked.Add(ref totalBytes, buffer.Length);

        etherTypeCount.AddOrUpdate(
            networkProtocol,
            new Count() { bytes = buffer.Length, packets = 1 },
            (code, count) => {
                Interlocked.Add(ref count.bytes, buffer.Length);
                Interlocked.Increment(ref count.packets);
                return count;
            }
        );
    }

    private void HandleIPv4(byte[] buffer, long timestamp, Mac sourceMac, Mac destinationMac) {
        (ushort l3Size, byte ttl, byte transportProtocol, byte ihl, IP sourceIP, IP destinationIP) = ParseV4PacketHeader(buffer, 14);

        HandleTransportLayer(transportProtocol, buffer, timestamp, sourceIP, destinationIP, ihl);

        transportCount.AddOrUpdate(
            transportProtocol,
            new Count() { bytes = buffer.Length, packets = 1 },
            (code, count) => {
                Interlocked.Add(ref count.bytes, buffer.Length);
                Interlocked.Increment(ref count.packets);
                return count;
            }
        );

        packetCount.AddOrUpdate(
            sourceIP,
            new TrafficData() { bytesRx = 0, bytesTx = buffer.Length, packetsRx = 0, packetsTx = 1, lastActivity = timestamp },
            (ip, traffic) => {
                Interlocked.Add(ref traffic.bytesTx, buffer.Length);
                Interlocked.Increment(ref traffic.packetsTx);
                traffic.lastActivity = timestamp;
                return traffic;
            }
        );

        packetCount.AddOrUpdate(
            destinationIP,
            new TrafficData() { bytesRx = buffer.Length, bytesTx = 0, packetsRx = 1, packetsTx = 0, lastActivity = timestamp },
            (ip, traffic) => {
                Interlocked.Add(ref traffic.bytesRx, buffer.Length);
                Interlocked.Increment(ref traffic.packetsRx);
                traffic.lastActivity = timestamp;
                return traffic;
            }
        );

        if (!sourceMac.IsBroadcast() && !sourceMac.IsMulticast()) {
            if (macTable.TryGetValue(sourceIP, out Mac existingMac)) {
                if (!sourceMac.Equals(existingMac)) {
                    macTable.Remove(sourceIP, out _);
                    issuesList.Add(new SniffIssuesItem($"Duplicate IP: {sourceIP} used by {existingMac} and {sourceMac}"));
                }
            }
            macTable.TryAdd(sourceIP, sourceMac);
        }

        if (ttl == 0) {
            issuesList.Add(new SniffIssuesItem($"TTL expired for packet {sourceIP}"));
        }
    }

    private void HandleIPv6(byte[] buffer, long timestamp, Mac sourceMac, Mac destinationMac) {
        (ushort l3Size, byte transportProtocol, byte ttl, IP sourceIP, IP destinationIP) = ParseV6PacketHeader(buffer, 14);
        byte ihl = 40;

        HandleTransportLayer(transportProtocol, buffer, timestamp, sourceIP, destinationIP, ihl);

        transportCount.AddOrUpdate(
            transportProtocol,
            new Count() { bytes = buffer.Length, packets = 1 },
            (code, count) => {
                Interlocked.Add(ref count.bytes, buffer.Length);
                Interlocked.Increment(ref count.packets);
                return count;
            }
        );

        packetCount.AddOrUpdate(
            sourceIP,
            new TrafficData() { bytesRx = 0, bytesTx = buffer.Length, packetsRx = 0, packetsTx = 1, lastActivity = timestamp },
            (ip, traffic) => {
                Interlocked.Add(ref traffic.bytesTx, buffer.Length);
                Interlocked.Increment(ref traffic.packetsTx);
                traffic.lastActivity = timestamp;
                return traffic;
            }
        );

        packetCount.AddOrUpdate(
            destinationIP,
            new TrafficData() { bytesRx = buffer.Length, bytesTx = 0, packetsRx = 1, packetsTx = 0, lastActivity = timestamp },
            (ip, traffic) => {
                Interlocked.Add(ref traffic.bytesRx, buffer.Length);
                Interlocked.Increment(ref traffic.packetsRx);
                traffic.lastActivity = timestamp;
                return traffic;
            }
        );

        if (!sourceMac.IsBroadcast() && !sourceMac.IsMulticast()) {
            if (macTable.TryGetValue(sourceIP, out Mac existingMac)) {
                if (!sourceMac.Equals(existingMac)) {
                    macTable.Remove(sourceIP, out _);
                    issuesList.Add(new SniffIssuesItem($"Duplicate IP: {sourceIP} used by {existingMac} and {sourceMac}"));
                }
            }
            macTable.TryAdd(sourceIP, sourceMac);
        }

        if (ttl == 0) {
            issuesList.Add(new SniffIssuesItem($"TTL expired for packet {sourceIP}"));
        }
    }

    private void HandleTransportLayer(byte transportProtocol, byte[] buffer, long timestamp, IP sourceIP, IP destinationIP, byte ihl) {
        switch ((TransportProtocol)transportProtocol) {
        case TransportProtocol.TCP:
            HandleTCP(buffer, timestamp, sourceIP, destinationIP, ihl);
            break;

        case TransportProtocol.UDP:
            HandleUDP(buffer, timestamp, sourceIP, destinationIP, ihl);
            break;
        }
    }

    private void HandleTCP(byte[] buffer, long timestamp, IP sourceIP, IP destinationIP, byte ihl) {
        (ushort sourcePort, ushort destinationPort, uint sequenceNo, uint ackNo, ushort flags, ushort window, ushort checksum, uint size) = ParseSegmentHeader(buffer, 14 + ihl);

        if (sourcePort < maxPort) {
            segmentCount.AddOrUpdate(
                sourcePort,
                new TrafficData() { bytesRx = 0, bytesTx = buffer.Length, packetsRx = 0, packetsTx = 1, lastActivity = timestamp },
                (port, traffic) => {
                    Interlocked.Add(ref traffic.bytesTx, buffer.Length);
                    Interlocked.Increment(ref traffic.packetsTx);
                    traffic.lastActivity = timestamp;
                    return traffic;
                }
            );
        }

        if (destinationPort < maxPort) {
            segmentCount.AddOrUpdate(
                destinationPort,
                new TrafficData() { bytesRx = buffer.Length, bytesTx = 0, packetsRx = 1, packetsTx = 0, lastActivity = timestamp },
                (port, traffic) => {
                    Interlocked.Add(ref traffic.bytesRx, buffer.Length);
                    Interlocked.Increment(ref traffic.packetsRx);
                    traffic.lastActivity = timestamp;
                    return traffic;
                }
            );
        }

        FourTuple fourTuple = new FourTuple(sourceIP, destinationIP, sourcePort, destinationPort);
        Segment   segment   = new Segment(timestamp, fourTuple, sequenceNo, ackNo, flags, window, size);

        ConcurrentQueue<Segment> stream = streams.GetOrAdd(fourTuple, _ => new ConcurrentQueue<Segment>());
        stream.Enqueue(segment);

        IP target = new IP([192, 168, 169, 4]);

        if (true || (sourceIP == target || destinationIP == target) && (fourTuple.sourcePort == 4443 || fourTuple.destinationPort == 4443)) {
            SegmentAnalysis(in segment, stream);
        }

    }

    private void HandleUDP(byte[] buffer, long timestamp, IP sourceIP, IP destinationIP, byte ihl) {
        (ushort sourcePort, ushort destinationPort, _) = ParseDatagramHeader(buffer, 14 + ihl);

        if (sourcePort < maxPort) {
            datagramCount.AddOrUpdate(
                sourcePort,
                new TrafficData() { bytesRx = 0, bytesTx = buffer.Length, packetsRx = 0, packetsTx = 1, lastActivity = timestamp },
                (port, traffic) => {
                    Interlocked.Add(ref traffic.bytesTx, buffer.Length);
                    Interlocked.Increment(ref traffic.packetsTx);
                    traffic.lastActivity = timestamp;
                    return traffic;
                }
            );
        }

        if (destinationPort < maxPort) {
            datagramCount.AddOrUpdate(
                destinationPort,
                new TrafficData() { bytesRx = buffer.Length, bytesTx = 0, packetsRx = 1, packetsTx = 0, lastActivity = timestamp },
                (port, traffic) => {
                    Interlocked.Add(ref traffic.bytesRx, buffer.Length);
                    Interlocked.Increment(ref traffic.packetsRx);
                    traffic.lastActivity = timestamp;
                    return traffic;
                }
            );
        }
    }

    private (ushort, byte, byte, byte, IP, IP) ParseV4PacketHeader(byte[] buffer, int offset) {
        if (buffer.Length < 20) return (0, 0, 0, 0, default, default); //invalid traffic

        byte   ihl      = (byte)((buffer[offset] & 0x0F) << 2);
        ushort size     = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset + 2));
        byte   ttl      = buffer[offset + 8];
        byte   protocol = buffer[offset + 9];
        //ushort checksum = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset + 10));

        IP source      = new IP(buffer.AsSpan(offset + 12, 4));
        IP destination = new IP(buffer.AsSpan(offset + 16, 4));

        return (size, ttl, protocol, ihl, source, destination);
    }

    private (ushort, byte, byte, IP, IP) ParseV6PacketHeader(byte[] buffer, int offset) {
        if (buffer.Length < 40) return (0, 0, 0, default, default); //invalid traffic

        return (
            (ushort)(BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset + 4)) + 40), //size
            buffer[offset + 6],                    //protocol
            buffer[offset + 7],                    //ttl
            new IP(buffer.AsSpan(offset + 8, 16)), //source
            new IP(buffer.AsSpan(offset + 24, 16)) //destination
            );
    }

    private (ushort, ushort, ushort) ParseDatagramHeader(byte[] buffer, int offset) {
        return (
            BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset + 0)),  //source
            BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset + 2)),  //destination
            BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset + 4))   //size
            //BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset + 6)) //checksum
            );
    }

    private (ushort, ushort, uint, uint, ushort, ushort, ushort, uint) ParseSegmentHeader(byte[] buffer, int offset) {
        int headerSize = (buffer[offset + 12] >> 4) << 2;

        return (
            BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset + 0)),  //source
            BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset + 2)),  //destination
            BinaryPrimitives.ReadUInt32BigEndian(buffer.AsSpan(offset + 4)),  //sequenceNo
            BinaryPrimitives.ReadUInt32BigEndian(buffer.AsSpan(offset + 8)),  //acknowledgmentNo
            BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset + 12)), //flags
            BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset + 14)), //window
            BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset + 16)), //size
            (uint)(buffer.Length - headerSize - offset));
    }

    public void Dispose() {
        //frames.Clear();
        device?.Dispose();
        Stop();
    }
}