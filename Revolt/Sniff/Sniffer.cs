using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using SharpPcap;

namespace Revolt.Sniff;

public sealed partial class Sniffer : IDisposable {

    //[StructLayout(LayoutKind.Auto)]
    public struct Frame {
        public long            timestamp;
        public ushort          size;

        public PhysicalAddress sourceMac;
        public PhysicalAddress destinationMac;
        public byte            networkProtocol;

        public IPAddress       sourceIP;
        public IPAddress       destinationIP;
        public byte            ttl;
        public byte            transportProtocol;

        public ushort          sourcePort;
        public ushort          destinationPort;
    }

    public bool analyzeL2 = true;
    public bool analyzeL3 = true;
    public bool analyzeL4 = true;

    public List<Frame> capture = new List<Frame>();
    public long bytesRx, bytesTx = 0;
    public long packetsRx, packetsTx = 0;

    private ICaptureDevice device;
    private PhysicalAddress deviceMac;

    public Sniffer(ICaptureDevice device) {
        if (device.MacAddress is null) {
            throw new Exception("no mac address");
        }

        this.device = device;
        deviceMac = device.MacAddress;
    }

    public void Start() {
        device.OnPacketArrival += Device_onPacketArrival;
        device.Open();
        device.StartCapture();
    }

    private void Device_onPacketArrival(object sender, SharpPcap.PacketCapture e) {
        RawCapture rawPacket = e.GetPacket();
        byte[] data = rawPacket.Data;
        //HandleFrames(data, data.Length);
    }

    public void Stop() {
        if (device is null) return;
        device!.StopCapture();
        device!.Close();
        device = null;
    }

    private void HandleFrames(byte[] buffer, int length) {
        Interlocked.Add(ref bytesRx, length);
        Interlocked.Increment(ref packetsRx);

        //TODO:
    }

    private void HandleV4Packet(byte[] buffer, int length) {
        if (length < 20) return; //invalid traffic

        ushort size     = (ushort)(buffer[2] << 8 | buffer[3]);
        byte   ttl      = buffer[8];
        byte   protocol = buffer[9];
        //ushort checksum = (ushort)(buffer[10] << 8 | buffer[11]);

        //if (size != length) return; //size mismatch

        Frame packet = new Frame() {
            timestamp         = Stopwatch.GetTimestamp(),
            networkProtocol   = 0,
            size              = size,
            ttl               = ttl,
            transportProtocol = protocol,
            sourceIP          = new IPAddress(buffer.AsSpan(12, 4)),
            destinationIP     = new IPAddress(buffer.AsSpan(16, 4))
        };

        capture.Add(packet);

        if (analyzeL4) {
            byte ihl = (byte)(buffer[0] & 0x0F << 2);
            HandleLayer4(buffer, ihl);
        }
    }

    private void HandleV6Packet(byte[] buffer, int length) {
        if (length < 40) return; //invalid traffic

        ushort size     = (ushort)((buffer[4] << 8 | buffer[5]) + 40);
        byte   protocol = buffer[6];
        byte   ttl      = buffer[7];

        //if (size != length) return; //size mismatch

        Frame packet = new Frame() {
            timestamp         = Stopwatch.GetTimestamp(),
            networkProtocol   = 0,
            size              = size,
            ttl               = ttl,
            transportProtocol = protocol,
            sourceIP          = new IPAddress(buffer.AsSpan(8, 16)),
            destinationIP     = new IPAddress(buffer.AsSpan(24, 16))
        };

        capture.Add(packet);

        if (analyzeL4) {
            HandleLayer4(buffer, 40);
        }
    }

    private void HandleLayer4(byte[] buffer, int offset) {
        //TODO: handle layer 4 header
    }

    public void Dispose() {
        Stop();
        capture.Clear();
        device?.Dispose();
    }
}