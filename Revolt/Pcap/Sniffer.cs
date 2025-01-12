using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Revolt.Pcap;

public sealed partial class Sniffer {

    public struct Packet {
        public long      timestamp;
        public bool      isIPv6;
        public ushort    size;
        public byte      ttl;
        public byte      protocol;
        
        public IPAddress source;
        public IPAddress destination;

        public ushort sourcePort;
        public ushort destinationPort;
    }

    public bool analyzeL3 = true;
    public bool analyzeL4 = true;

    public List<Packet> packets = new List<Packet>();
    public long totalBytesRx = 0;
    public long totalPacketsRx = 0;

    public CancellationTokenSource cancellationTokenSource;
    public CancellationToken cancellationToken;

    public void Start(IPAddress address) {
        AddressFamily ipFamily = address.AddressFamily;
        SocketOptionLevel socketLevel = ipFamily == AddressFamily.InterNetwork ? SocketOptionLevel.IP : SocketOptionLevel.IPv6;

        using Socket socket = new Socket(ipFamily, SocketType.Raw, ProtocolType.IP);

        socket.Bind(new IPEndPoint(address, 0));
        socket.SetSocketOption(socketLevel, SocketOptionName.HeaderIncluded, true);

        byte[] buffer = new byte[65535];

        cancellationTokenSource = new CancellationTokenSource();
        cancellationToken = cancellationTokenSource.Token;

        if (ipFamily == AddressFamily.InterNetwork) {
            while (!cancellationToken.IsCancellationRequested) {
                int bytesRead = socket.Receive(buffer);
                HandleV4Packet(buffer, bytesRead);

                //Interlocked.Add(ref totalBytesRx, bytesRead);
                //Interlocked.Increment(ref totalPacketsRx);
                
                totalBytesRx += bytesRead;
                totalPacketsRx++;
            }
        }
        else if (ipFamily == AddressFamily.InterNetworkV6) {
            while (!cancellationToken.IsCancellationRequested) {
                int bytesRead = socket.Receive(buffer);
                HandleV6Packet(buffer, bytesRead);

                //Interlocked.Add(ref totalBytesRx, bytesRead);
                //Interlocked.Increment(ref totalPacketsRx);

                totalBytesRx += bytesRead;
                totalPacketsRx++;
            }
        }
        else {
            throw new NotSupportedException("Unsupported network protocol");
        }
    }

    public void Stop() {
        cancellationTokenSource?.Cancel();
    }

    private void HandleV4Packet(byte[] buffer, int length) {
        if (length < 20) return; //invalid traffic

        ushort size     = (ushort)(buffer[2] << 8 | buffer[3]);
        byte   ttl      = buffer[4];
        byte   protocol = buffer[5];
        //ushort checksum = (ushort)(buffer[6] << 8 | buffer[7]);

        //if (size != length) return; //size mismatch

        Packet packet = new Packet() {
            timestamp   = Stopwatch.GetTimestamp(),
            isIPv6      = false,
            size        = size,
            ttl         = ttl,
            protocol    = protocol,
            source      = new IPAddress(buffer.AsSpan(12, 4)),
            destination = new IPAddress(buffer.AsSpan(16, 4))
        };

        packets.Add(packet);

        if (analyzeL4) {
            byte ihl = (byte)(buffer[0] & 0x0F << 2);
            HandleL4(buffer, ihl);
        }
    }

    private void HandleV6Packet(byte[] buffer, int length) {
        if (length < 40) return; //invalid traffic

        ushort size     = (ushort)((buffer[4] << 8 | buffer[5]) + 40);
        byte   protocol = buffer[6];
        byte   ttl      = buffer[7];

        //if (size != length) return; //size mismatch

        Packet packet = new Packet() {
            timestamp   = Stopwatch.GetTimestamp(),
            isIPv6      = true,
            size        = size,
            ttl         = ttl,
            protocol    = protocol,
            source      = new IPAddress(buffer.AsSpan(8, 16)),
            destination = new IPAddress(buffer.AsSpan(24, 16))
        };

        packets.Add(packet);

        if (analyzeL4) {
            HandleL4(buffer, 40);
        }
    }

    private void HandleL4(byte[] buffer, int transportIndex) {
        
    }

}