using System.Net;
using System.Net.Sockets;
using System.Text;
using Revolt.Frames;

namespace Revolt.Proprietary;

public class Mndp {
    private const           int       timeout     = 1000;
    private static readonly int       port        = 5678;
    private static readonly IPAddress multicastAddress = IPAddress.Parse("239.255.255.255");
    private static readonly byte[]    requestData = [0x00, 0x00, 0x00, 0x00];

    public static List<IpDiscoveryFrame.DiscoverItem> Discover(IPAddress localIpV4, IPAddress localIpV6) {
        List<IpDiscoveryFrame.DiscoverItem> list = [];

        IPEndPoint localEndPointV4 = new IPEndPoint(localIpV4, 0);

        using UdpClient client = new UdpClient(localEndPointV4) {
            EnableBroadcast = true
        };

        IPEndPoint broadcastEndpoint   = new(IPAddress.Broadcast, port);
        IPEndPoint multicastEndpoint   = new(multicastAddress, port);

        try {
            client.Send(requestData, requestData.Length, broadcastEndpoint);
            client.Send(requestData, requestData.Length, multicastEndpoint);
            client.Client.ReceiveTimeout = timeout;

            while (true) {
                try {
                    IPEndPoint receivedEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    byte[] receivedData = client.Receive(ref receivedEndPoint);

                    (IpDiscoveryFrame.DiscoverItem item, bool error) = Parse(receivedData);
                    if (!error) {
                        list.Add(item);
                    }
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut) {
                    break;
                }
            }
        }
        catch { }

        if (localIpV6 is not null && localIpV6 != IPAddress.IPv6None) {
            IPEndPoint localEndPointV6 = new IPEndPoint(localIpV6, 0);

            using UdpClient clientV6 = new UdpClient(localEndPointV6) {
                EnableBroadcast = true
            };

            IPEndPoint multicastEndpointV6 = new(IPAddress.Parse("ff02::1"), port);

            try {
                clientV6.Send(requestData, requestData.Length, multicastEndpointV6);
                clientV6.Client.ReceiveTimeout = timeout;

                while (true) {
                    try {
                        IPEndPoint receivedEndPoint = new IPEndPoint(IPAddress.IPv6Any, 0);
                        byte[] receivedData = clientV6.Receive(ref receivedEndPoint);

                        (IpDiscoveryFrame.DiscoverItem item, bool error) = Parse(receivedData);
                        if (!error) {
                            list.Add(item);
                        }
                    }
                    catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut) {
                        break;
                    }
                }
            }
            catch { }
        }

        return list;
    }

    private static (IpDiscoveryFrame.DiscoverItem, bool) Parse(byte[] data) {
        int index = 4;
        Dictionary<short, (int, int)> attributers = [];

        while (index < data.Length) {
            if (index + 4 > data.Length) return (default, true);

            short type = (short)(data[index++] << 8 | data[index++]);
            short size = (short)(data[index++] << 8 | data[index++]);

            if (index + size > data.Length) return (default, true);

            attributers.Add(type, (index, size));
            index += size;
        }

        if (attributers.Count == 0) return (default, true);

        IpDiscoveryFrame.DiscoverItem item = new IpDiscoveryFrame.DiscoverItem();

        string softwareId    = String.Empty;
        string board         = String.Empty;
        string interfaceName = String.Empty;

        foreach (KeyValuePair<short, (int, int)> pair in attributers) {
            short type = pair.Key;
            (int offset, int size) = pair.Value;

            switch (type) {

            case 0x01: {
                string mac = ExtractMac(data, offset);
                item.mac = mac;
                break;
            }

            case 0x05: {
                string name = ExtractString(data, offset, size);
                item.name = name;
                break;
            }

            case 0x07: {
                string version = ExtractString(data, offset, size);
                break;
            }

            /*case 0x08: {
                string platform = ExtractString(data, offset, size);
                break;
            }*/

            /*case 0x0A: {
                uptime = ExtractString(data, offset, size);
                break;
            }*/

            case 0x0B: {
                softwareId = ExtractString(data, offset, size);
                break;
            }

            case 0x0C: {
                board = ExtractString(data, offset, size);
                break;
            }

            case 0x10: {
                interfaceName = ExtractString(data, offset, size);
                break;
            }

            }
        }

        if (String.IsNullOrEmpty(softwareId)) {
            item.other = String.IsNullOrEmpty(item.other) ? softwareId : $", {softwareId}";
        }

        if (String.IsNullOrEmpty(board)) {
            item.other = String.IsNullOrEmpty(item.other) ? board : $", {board}";
        }

        /*if (String.IsNullOrEmpty(uptime)) {
            item.other = String.IsNullOrEmpty(item.other) ? uptime : $", {uptime}";
        }*/

        if (String.IsNullOrEmpty(interfaceName)) {
            item.other = String.IsNullOrEmpty(item.other) ? interfaceName : $", {interfaceName}";
        }

        return (item, false);
    }

    private static string ExtractMac(byte[] data, int offset) =>
     $"{data[offset]:X2}-{data[offset + 1]:X2}-{data[offset + 2]:X2}-{data[offset + 3]:X2}-{data[offset + 4]:X2}-{data[offset + 5]:X2}";

    private static string ExtractString(byte[] data, int offset, int size) =>
        Encoding.UTF8.GetString(data, offset, size);
}
