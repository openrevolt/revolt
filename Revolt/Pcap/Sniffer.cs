using System.Net.Sockets;
using System.Net;

namespace Revolt.Pcap;

public sealed class Sniffer {

    static void Start(IPAddress address) {
        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP);

        socket.Bind(new IPEndPoint(address, 0));
        socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, true);
        byte[] inBytes = new byte[4] { 1, 0, 0, 0 };
        byte[] outBytes = new byte[4];
        socket.IOControl(IOControlCode.ReceiveAll, inBytes, outBytes);

        byte[] buffer = new byte[4096];

        while (true) {
            int bytesRead = socket.Receive(buffer);
            if (bytesRead == 0) continue;
            ParsePacket(buffer, bytesRead);
        }
    }

    static void ParsePacket(byte[] buffer, int length) {
        int ipHeaderLength = (buffer[0] & 0x0F) * 4;
        byte protocol = buffer[9];
        const int sourceIpStartIndex = 12;
        const int destIpStartIndex = 16;

        IPAddress sourceIp = new IPAddress(BitConverter.ToUInt32(buffer, sourceIpStartIndex));
        IPAddress destIp = new IPAddress(BitConverter.ToUInt32(buffer, destIpStartIndex));

        Console.WriteLine($"IP Packet: {sourceIp} -> {destIp}, Protocol: {protocol}");

        if (protocol == 6) {
            int tcpHeaderStartIndex = ipHeaderLength;
            ushort sourcePort = BitConverter.ToUInt16(new byte[] { buffer[tcpHeaderStartIndex + 1], buffer[tcpHeaderStartIndex] }, 0);
            ushort destPort = BitConverter.ToUInt16(new byte[] { buffer[tcpHeaderStartIndex + 3], buffer[tcpHeaderStartIndex + 2] }, 0);

            Console.WriteLine($"TCP Packet: {sourceIp}:{sourcePort} -> {destIp}:{destPort}");
        }
        else if (protocol == 17) {
            int udpHeaderStartIndex = ipHeaderLength;
            ushort sourcePort = BitConverter.ToUInt16(new byte[] { buffer[udpHeaderStartIndex + 1], buffer[udpHeaderStartIndex] }, 0);
            ushort destPort = BitConverter.ToUInt16(new byte[] { buffer[udpHeaderStartIndex + 3], buffer[udpHeaderStartIndex + 2] }, 0);

            Console.WriteLine($"UDP Packet: {sourceIp}:{sourcePort} -> {destIp}:{destPort}");
        }

        Console.WriteLine();
    }

}