using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using Revolt.Tui;
using SharpPcap;
using static Revolt.Sniff.Sniffer;

namespace Revolt.Sniff;

public sealed partial class Sniffer : IDisposable {
    public long totalPackets = 0, totalBytes = 0;

    public IndexedDictionary<Mac, TrafficData>    framesCount    = new IndexedDictionary<Mac, TrafficData>();
    public IndexedDictionary<IP, TrafficData>     packetCount    = new IndexedDictionary<IP, TrafficData>();
    public IndexedDictionary<ushort, TrafficData> segmentCount   = new IndexedDictionary<ushort, TrafficData>();
    public IndexedDictionary<ushort, TrafficData> datagramCount  = new IndexedDictionary<ushort, TrafficData>();
    public IndexedDictionary<ushort, Count>       etherTypeCount = new IndexedDictionary<ushort, Count>();
    public IndexedDictionary<byte, Count>         transportCount = new IndexedDictionary<byte, Count>();

    //private ulong frameIndex = 0;
    //private ConcurrentDictionary<ulong, Packet> frames = new ConcurrentDictionary<ulong, Packet>();

    private ulong segmentIndex = 0;
    private ConcurrentDictionary<ulong, Segment> segments = new ConcurrentDictionary<ulong, Segment>();

    private ICaptureDevice device;
    private PhysicalAddress deviceMac;

    private const ushort maxPort = 1024; //49152

    public Sniffer(ICaptureDevice device) {
        if (device.MacAddress is null) {
            throw new Exception("no mac address");
        }

        this.device = device;
        deviceMac = device.MacAddress;

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
        Mac       sourceMac         = new Mac(buffer, 0);
        Mac       destinationMac    = new Mac(buffer, 6);
        ushort    networkProtocol   = (ushort)(buffer[12] << 8 | buffer[13]);

        switch ((etherTypes)networkProtocol) {
        case etherTypes.IPv4:
            HandleIPv4(buffer, timestamp);
            break;

        case etherTypes.IPv6:
            HandleIPv6(buffer, timestamp);
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

    private void HandleIPv4(byte[] buffer, long timestamp) {
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
    }

    private void HandleIPv6(byte[] buffer, long timestamp) {
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
        (ushort sourcePort, ushort destinationPort, _) = ParseSegmentHeader(buffer, 14 + ihl);

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

        Segment segment = new Segment() {
            initialSequence = 0,
            seqNumber       = 0,
            ackNumber       = 0,
            window          = 0
        };

        segments.TryAdd(segmentIndex, segment);
        Interlocked.Increment(ref segmentIndex);
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
        ushort size     = (ushort)(buffer[offset+2] << 8 | buffer[offset+3]);
        byte   ttl      = buffer[offset+8];
        byte   protocol = buffer[offset+9];
        //ushort checksum = (ushort)(buffer[offset+10] << 8 | buffer[offset+11]);

        IP source      = new IP(buffer.AsSpan(offset+12, 4));
        IP destination = new IP(buffer.AsSpan(offset+16, 4));

        return (size, ttl, protocol, ihl, source, destination);
    }

    private (ushort, byte, byte, IP, IP) ParseV6PacketHeader(byte[] buffer, int offset) {
        if (buffer.Length < 40) return (0, 0, 0, default, default); //invalid traffic

        ushort size     = (ushort)((buffer[offset+4] << 8 | buffer[offset+5]) + 40);
        byte   protocol = buffer[offset+6];
        byte   ttl      = buffer[offset+7];

        IP source      = new IP(buffer.AsSpan(offset+8, 16));
        IP destination = new IP(buffer.AsSpan(offset+24, 16));

        return (size, protocol, ttl, source, destination);
    }

    private (ushort, ushort, ushort) ParseSegmentHeader(byte[] buffer, int offset) {
        ushort source      = (ushort)(buffer[offset+0] << 8 | buffer[offset+1]);
        ushort destination = (ushort)(buffer[offset+2] << 8 | buffer[offset+3]);
        ushort size        = (ushort)(buffer[offset+14] << 8 | buffer[offset+15]);
        //ushort checksum    = (ushort)(buffer[offset+16] << 8 | buffer[offset+17]);
        return (source, destination, size);
    }

    private (ushort, ushort, ushort) ParseDatagramHeader(byte[] buffer, int offset) {
        ushort source      = (ushort)(buffer[offset+0] << 8 | buffer[offset+1]);
        ushort destination = (ushort)(buffer[offset+2] << 8 | buffer[offset+3]);
        ushort size        = (ushort)(buffer[offset+4] << 8 | buffer[offset+5]);
        //ushort checksum    = (ushort)(buffer[offset+5] << 8 | buffer[offset+6]);
        return (source, destination, size);
    }

    public void Dispose() {
        //frames.Clear();
        device?.Dispose();
        Stop();
    }
}