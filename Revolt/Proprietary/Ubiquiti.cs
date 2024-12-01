using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Revolt.Frames;

namespace Revolt.Proprietary;

public class Ubiquiti {
    private const           int       timeout          = 2000;
    private const           int       port             = 10001;
    private static readonly IPAddress multicastAddress = IPAddress.Parse("233.89.188.1");
    private static readonly byte[]    requestData      = [0x01, 0x00, 0x00, 0x00];

    public static List<IpDiscoveryFrame.DiscoverItem> Discover(IPAddress localIpV4) {
        List<IpDiscoveryFrame.DiscoverItem> list = [];

        IPEndPoint localEndPointV4 = new IPEndPoint(localIpV4, 0);
        using UdpClient client = new UdpClient(localEndPointV4) {
            EnableBroadcast = true
        };

        SendAndReceive(client, list);

        SendAndReceive(client, list);

        list.Sort((a, b) => String.Compare(a.mac, b.mac));

        return list;
    }

    private static void SendAndReceive(UdpClient client, List<IpDiscoveryFrame.DiscoverItem> list) {
        IPEndPoint remoteEndPointA = new IPEndPoint(multicastAddress, port);
        IPEndPoint remoteEndPointB = new IPEndPoint(IPAddress.Broadcast, port);

        try {
            client.Send(requestData, requestData.Length, remoteEndPointA);
            client.Send(requestData, requestData.Length, remoteEndPointB);
            client.Client.ReceiveTimeout = timeout;

            while (true) {
                try {
                    IPEndPoint receivedEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    byte[] receivedData = client.Receive(ref receivedEndPoint);

                    (IpDiscoveryFrame.DiscoverItem item, bool error) = Parse(receivedData);
                    if (error) continue;

                    if (list.FindIndex(o => o.mac == item.mac) > -1) continue;

                    list.Add(item);

                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut) {
                    break;
                }
            }
        }
        catch { }
    }

    private static (IpDiscoveryFrame.DiscoverItem, bool) Parse(byte[] data) {
        if (data.Length < 4) return (default, true);

        byte type = data[0];

        /*return type switch {
            1 => ParseType1(data),
            _ => (default, true),
        };*/

        return ParseType1(data);
    }

    private static (IpDiscoveryFrame.DiscoverItem, bool) ParseType1(byte[] data) {
        int index = 4;
        Dictionary<byte, (int, int)> attributers = [];

        while (index < data.Length) {
            if (index + 3 > data.Length) return (default, true);

            byte type = data[index++];
            index++; //null
            byte size = data[index++];

            if (index + size > data.Length) return (default, true);

            attributers.Add(type, (index, size));

            index += size;
        }

        if (attributers.Count == 0) return (default, true);

        IpDiscoveryFrame.DiscoverItem item = new IpDiscoveryFrame.DiscoverItem();

        foreach (KeyValuePair<byte, (int, int)> pair in attributers) {
            byte type = pair.Key;
            (int offset, int size) = pair.Value;

            switch (type) {

            case 0x01: {
                string mac = ExtractMac(data, offset);
                item.mac = mac;
                break;
            }

            case 0x02: {
                (string mac, string ip) = ExtractMacIpPair(data, offset);
                item.mac = mac;
                item.ip = ip;
                break;
            }

            /*case 0x03: {
                string firmware = ExtractString(data, offset, size);
                break;
            }*/

            case 0x0B: {
                string name = ExtractString(data, offset, size);
                item.name = name;
                break;
            }

            case 0x0C: {
                string product = ExtractString(data, offset, size);
                item.other = product;
                break;
            }

            }
        }

        return (item, false);
    }

    private static string ExtractMac(byte[] data, int offset) =>
         $"{data[offset]:X2}-{data[offset+1]:X2}-{data[offset+2]:X2}-{data[offset+3]:X2}-{data[offset+4]:X2}-{data[offset+5]:X2}";

    private static (string, string) ExtractMacIpPair(byte[] data, int offset) {
        string mac = $"{data[offset]:X2}-{data[offset+1]:X2}-{data[offset+2]:X2}-{data[offset+3]:X2}-{data[offset+4]:X2}-{data[offset+5]:X2}";
        string ip = $"{data[offset+6]}.{data[offset+7]}.{data[offset+8]}.{data[offset+9]}";
        return (mac, ip);
    }

    private static string ExtractString(byte[] data, int offset, int size) =>
        Encoding.UTF8.GetString(data, offset, size);

}
