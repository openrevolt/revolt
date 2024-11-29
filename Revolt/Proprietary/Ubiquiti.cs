using Revolt.Frames;
using System.Net;
using System.Net.Sockets;

using System.Text;

namespace Revolt.Proprietary;

public class Ubiquiti {
    private static readonly IPAddress address       = IPAddress.Parse("233.89.188.1");
    private static readonly int       port          = 10001;
    private static readonly byte[]    broadcastData = [0x01, 0x00, 0x00, 0x00];

    public static IpDiscoveryFrame.DiscoverItem[] Discover() {
        List<IpDiscoveryFrame.DiscoverItem> list = [];

        using UdpClient client = new UdpClient() {
            EnableBroadcast = true
        };

        //try {
        IPEndPoint endPoint = new IPEndPoint(address, port);
        client.Send(broadcastData, broadcastData.Length, endPoint);
        client.Client.ReceiveTimeout = 2000;

        while (true) {
            try {
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] receivedData = client.Receive(ref remoteEndPoint);

                (IpDiscoveryFrame.DiscoverItem item, bool error) = Parse(receivedData);
                if (error) continue;
                list.Add(item);
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut) {
                break;
            }
        }
        /*}
        catch { }*/

        list.Sort((a, b) => String.Compare(a.mac, b.mac));

        return list.ToArray();
    }

    private static (IpDiscoveryFrame.DiscoverItem, bool) Parse(byte[] data) {
        if (data.Length < 40) return (default, true);

        byte type = data[0];

        return type switch {
            1 => ParseType1(data),
            _ => (default, true),
        };
    }

    private static (IpDiscoveryFrame.DiscoverItem, bool) ParseType1(byte[] data) {
        int index = 4;
        Dictionary<byte, (int, int)> attributers = [];

        while (index < data.Length) {
            byte type = data[index++];
            index++; //null
            byte size = data[index++];

            attributers.Add(type, (index, size));

            index += size;
        }

        if (attributers.Count == 0) return (default, true);

        IpDiscoveryFrame.DiscoverItem item = new IpDiscoveryFrame.DiscoverItem();

        string firmware = String.Empty;

        foreach (KeyValuePair<byte, (int, int)> pair in attributers) {
            byte type = pair.Key;
            (int offset, int size) = pair.Value;

            switch (type) {

            case 0x01: { //mac
                string mac = ExtractMac(data, offset, size);
                item.mac = mac;
                break;
            }

            case 0x02: { //mac and ip
                (string mac, string ip) = ExtractMacIpPair(data, offset, size);
                item.mac = mac;
                item.ip = ip;
                break;
            }

            case 0x03: { //firmware
                firmware = ExtractName(data, offset, size);
                break;
            }

            case 0x0B: { //name
                string name = ExtractName(data, offset, size);
                item.name = name;
                break;
            }

            case 0x0C: { //product
                string product = ExtractName(data, offset, size);
                item.other = product;
                break;
            }

            }
        }

        if (!String.IsNullOrEmpty(firmware)) {
            if (String.IsNullOrEmpty(item.other)) {
                item.other = firmware;
            }
            else {
                item.other += $" - {firmware}";
            }
        }

        return (item, false);
    }

    private static string ExtractMac(byte[] data, int offset, int size) =>
         $"{data[offset]:X2}-{data[offset+1]:X2}-{data[offset+2]:X2}-{data[offset+3]:X2}-{data[offset+4]:X2}-{data[offset+5]:X2}";
    

    private static (string, string) ExtractMacIpPair(byte[] data, int offset, int size) {
        string mac = $"{data[offset]:X2}-{data[offset+1]:X2}-{data[offset+2]:X2}-{data[offset+3]:X2}-{data[offset+4]:X2}-{data[offset+5]:X2}";
        string ip = $"{data[offset+6]}.{data[offset+7]}.{data[offset+8]}.{data[offset+9]}";
        return (mac, ip);
    }

    private static string ExtractName(byte[] data, int offset, int size) =>
        Encoding.UTF8.GetString(data, offset, size);



   /* private static (IpDiscoveryFrame.DiscoverItem, bool) ParseTypeA(byte[] data) {
        string mac, ip;
        int nameLength, nameIndex;
        int productLength = 0, productIndex = 0;

        mac = $"{data[16]:X2}-{data[17]:X2}-{data[18]:X2}-{data[19]:X2}-{data[20]:X2}-{data[21]:X2}";
        ip = $"{data[22]}.{data[23]}.{data[24]}.{data[25]}";

        nameLength = data[35];
        nameIndex = 36;
        for (int i = 26; i < data.Length; i++) {
            if (data[i] == 0x0B) {
                nameLength = data[i + 2];
                nameIndex = i + 3;
                break;
            }
        }

        if (nameIndex + nameLength < data.Length) {
            productLength = data[nameIndex + nameLength + 2];
            productIndex = nameIndex + nameLength + 3;
        }

        for (int i = 26; i < data.Length; i++) {
            if (data[i] == 0x0C) {
                productLength = data[i + 2];
                productIndex = i + 3;
                break;
            }
        }

        if (nameIndex + nameLength < data.Length) {
            productLength = data[nameIndex + nameLength + 2];
            productIndex = nameIndex + nameLength + 3;
        }

        string name    = System.Text.Encoding.UTF8.GetString(data, nameIndex, nameLength);

        string product = String.Empty;
        if (productIndex + productLength < data.Length) {
            product = System.Text.Encoding.UTF8.GetString(data, productIndex, productLength);
        }

        IpDiscoveryFrame.DiscoverItem item = new IpDiscoveryFrame.DiscoverItem {
            name  = name,
            ip    = ip,
            mac   = mac,
            other = product,
            bytes = data,
        };

        return (item, false);
    }

    private static (IpDiscoveryFrame.DiscoverItem, bool) ParseTypeB(byte[] data) {
        string mac, ip;
        int nameLength, nameIndex;
        int productLength = 0, productIndex = 0;

        mac = $"{data[7]:X2}-{data[8]:X2}-{data[9]:X2}-{data[10]:X2}-{data[11]:X2}-{data[12]:X2}";
        ip = $"{data[13]}.{data[14]}.{data[15]}.{data[16]}";
        nameLength = data[35];
        nameIndex = 36;

        if (nameIndex + nameLength < data.Length) {
            productLength = data[nameIndex + nameLength + 2];
            productIndex = nameIndex + nameLength + 3;
        }

        string name    = System.Text.Encoding.UTF8.GetString(data, nameIndex, nameLength);

        string product = String.Empty;
        if (productIndex + productLength < data.Length) {
            product = System.Text.Encoding.UTF8.GetString(data, productIndex, productLength);
        }

        IpDiscoveryFrame.DiscoverItem item = new IpDiscoveryFrame.DiscoverItem {
            name  = name,
            ip    = ip,
            mac   = mac,
            other = product,
            bytes = data,
        };

        return (item, false);
    }
   */
}
