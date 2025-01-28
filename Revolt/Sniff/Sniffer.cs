using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using SharpPcap;

namespace Revolt.Sniff;

public sealed partial class Sniffer : IDisposable {
    public IndexedDictionary<Mac, TrafficData>       framesCount   = new IndexedDictionary<Mac, TrafficData>();
    public IndexedDictionary<IPAddress, TrafficData> packetCount   = new IndexedDictionary<IPAddress, TrafficData>();
    public IndexedDictionary<ushort, TrafficData>    segmentCount  = new IndexedDictionary<ushort, TrafficData>();
    public IndexedDictionary<ushort, TrafficData>    datagramCount = new IndexedDictionary<ushort, TrafficData>();

    public IndexedDictionary<ushort, Count> networkCount = new IndexedDictionary<ushort, Count>();

    public long[] transportBytes   = new long[256];
    public long[] transportPackets = new long[256];

    public long totalPackets=0, totalBytes=0;

    private ulong frameIndex = 0;
    private ConcurrentDictionary<ulong, Frame> frames = new ConcurrentDictionary<ulong, Frame>();

    private ICaptureDevice device;
    private PhysicalAddress deviceMac;

    //private bool captureOnlyWellKnownPort = true;
    private const ushort maxPort = 1024; //49152

    public Sniffer(ICaptureDevice device) {
        if (device.MacAddress is null) {
            throw new Exception("no mac address");
        }

        this.device = device;
        deviceMac = device.MacAddress;
    }

    public void Start() {
        device.OnPacketArrival += Device_OnPacketArrival;
        device.Open();
        device.StartCapture();
    }

    private void Device_OnPacketArrival(object sender, SharpPcap.PacketCapture e) {
        RawCapture rawPacket = e.GetPacket();
        byte[] buffer = rawPacket.Data;
        HandlePacket(buffer, rawPacket.Timeval.Date.Ticks);
    }

    public void Stop() {
        if (device is null) return;
        device!.StopCapture();
        device!.Close();
        device = null;
    }

    private void HandlePacket(byte[] buffer, long timestamp) {
        Mac    sourceMac            = Mac.Parse(buffer, 0);
        Mac    destinationMac       = Mac.Parse(buffer, 6);
        ushort networkProtocol      = (ushort)(buffer[12] << 8 | buffer[13]);

        ushort    l3Size            = default;
        byte      ttl               = default;
        byte      transportProtocol = default;
        byte      ihl               = default;
        IPAddress sourceIP          = null;
        IPAddress destinationIP     = null;

        ushort sourcePort           = default;
        ushort destinationPort      = default;

        switch ((NetworkProtocol)networkProtocol) {
        case NetworkProtocol.IPv4:
            (l3Size, ttl, transportProtocol, ihl, sourceIP, destinationIP) = HandleV4Packet(buffer, 14);
            Interlocked.Add(ref transportBytes[transportProtocol], buffer.Length);
            Interlocked.Increment(ref transportPackets[transportProtocol]);
            break;

        case NetworkProtocol.IPv6:
            (l3Size, transportProtocol, ttl, sourceIP, destinationIP) = HandleV6Packet(buffer, 14);
            ihl = 40;
            Interlocked.Add(ref transportBytes[transportProtocol], buffer.Length);
            Interlocked.Increment(ref transportPackets[transportProtocol]);
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

        if (sourceIP is not null && destinationIP is not null) {
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

            switch ((TransportProtocol)transportProtocol) {
            case TransportProtocol.TCP: {
                (sourcePort, destinationPort, _) = HandleSegment(buffer, 14 + ihl);

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

                break;
            }
            case TransportProtocol.UDP: {
                (sourcePort, destinationPort, _) = HandleDatagram(buffer, 14 + ihl);

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

                break;
            }
            }
        }

        Frame frame = new Frame() {
            timestamp         = timestamp,
            size              = l3Size,

            sourceMac         = sourceMac,
            destinationMac    = destinationMac,
            networkProtocol   = networkProtocol,

            sourceIP          = sourceIP,
            destinationIP     = destinationIP,
            ttl               = ttl,
            transportProtocol = transportProtocol,

            sourcePort        = sourcePort,
            destinationPort   = destinationPort,
        };

        frames.TryAdd(frameIndex, frame);
        Interlocked.Increment(ref frameIndex);

        Interlocked.Increment(ref totalPackets);
        Interlocked.Add(ref totalBytes, buffer.Length);

        networkCount.AddOrUpdate(
            networkProtocol,
            new Count() { bytes = buffer.Length, packets = 1 },
            (code, count) => {
                Interlocked.Add(ref count.bytes, buffer.Length);
                Interlocked.Increment(ref count.packets);
                return count;
            }
        );
    }

    private (ushort, byte, byte, byte, IPAddress, IPAddress) HandleV4Packet(byte[] buffer, int offset) {
        if (buffer.Length < 20) return (0, 0, 0, 0, default, default); //invalid traffic

        byte   ihl      = (byte)((buffer[offset] & 0x0F) << 2);
        ushort size     = (ushort)(buffer[offset+2] << 8 | buffer[offset+3]);
        byte   ttl      = buffer[offset+8];
        byte   protocol = buffer[offset+9];
        //ushort checksum = (ushort)(buffer[offset+10] << 8 | buffer[offset+11]);

        IPAddress source      = new IPAddress(buffer.AsSpan(offset+12, 4));
        IPAddress destination = new IPAddress(buffer.AsSpan(offset+16, 4));

        return (size, ttl, protocol, ihl, source, destination);
    }

    private (ushort, byte, byte, IPAddress, IPAddress) HandleV6Packet(byte[] buffer, int offset) {
        if (buffer.Length < 40) return (0, 0, 0, default, default); //invalid traffic

        ushort size     = (ushort)((buffer[offset+4] << 8 | buffer[offset+5]) + 40);
        byte   protocol = buffer[offset+6];
        byte   ttl      = buffer[offset+7];

        IPAddress source      = new IPAddress(buffer.AsSpan(offset+8, 16));
        IPAddress destination = new IPAddress(buffer.AsSpan(offset+24, 16));

        return (size, protocol, ttl, source, destination);
    }

    private (ushort, ushort, ushort) HandleSegment(byte[] buffer, int offset) {
        ushort source      = (ushort)(buffer[offset+0] << 8 | buffer[offset+1]);
        ushort destination = (ushort)(buffer[offset+2] << 8 | buffer[offset+3]);
        ushort size        = (ushort)(buffer[offset+14] << 8 | buffer[offset+15]);
        //ushort checksum    = (ushort)(buffer[offset+16] << 8 | buffer[offset+17]);
        return (source, destination, size);
    }

    private (ushort, ushort, ushort) HandleDatagram(byte[] buffer, int offset) {
        ushort source      = (ushort)(buffer[offset+0] << 8 | buffer[offset+1]);
        ushort destination = (ushort)(buffer[offset+2] << 8 | buffer[offset+3]);
        ushort size        = (ushort)(buffer[offset+4] << 8 | buffer[offset+5]);
        //ushort checksum    = (ushort)(buffer[offset+5] << 8 | buffer[offset+6]);
        return (source, destination, size);
    }

    public void Dispose() {
        frames.Clear();
        device?.Dispose();
        Stop();
    }
}