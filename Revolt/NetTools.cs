using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Revolt;
public static class NetTools {

    public static IPAddress[] GetDirectIpAddresses() {
        NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
        List<IPAddress> list = [];
        foreach (NetworkInterface adapter in adapters) {
            UnicastIPAddressInformationCollection addresses = adapter.GetIPProperties().UnicastAddresses;
            if (addresses.Count == 0) continue;
            foreach (UnicastIPAddressInformation address in addresses) {
                list.Add(address.Address);
            }
        }
        return list.ToArray();
    }

    public static (IPAddress, IPAddress, IPAddress)[] GetDirectNetworks() {
        List<(IPAddress, IPAddress, IPAddress)> filtered = [];

        NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
        foreach (NetworkInterface nic in interfaces) {
            UnicastIPAddressInformationCollection unicast = nic.GetIPProperties().UnicastAddresses;

            if (unicast.Count == 0) continue;

            IPAddress localIpV4 = null;
            IPAddress subnetMask = null;
            IPAddress localIpV6 = null;

            foreach (UnicastIPAddressInformation address in unicast) {
                if (address.Address.AddressFamily == AddressFamily.InterNetwork) {
                    localIpV4 = address.Address;
                    subnetMask = address.IPv4Mask;
                }
                else if (address.Address.AddressFamily == AddressFamily.InterNetworkV6) {
                    localIpV6 = address.Address;
                }
            }

            if (localIpV4 is null || IPAddress.IsLoopback(localIpV4) || localIpV4.IsApipa()) continue;

            filtered.Add((localIpV4, subnetMask, localIpV6));
        }

        return filtered.ToArray();
    }

    public static uint ToUInt32(this IPAddress address) {
        byte[] bytes = address.GetAddressBytes();

        if (BitConverter.IsLittleEndian) {
            Array.Reverse(bytes);
        }

        return  BitConverter.ToUInt32(bytes, 0);
    }

    public static IPAddress GetLocalDnsAddress(bool forceIPv4 = false) {
        NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

        foreach (NetworkInterface networkInterface in networkInterfaces) {
            if (networkInterface.OperationalStatus == OperationalStatus.Up) {
                IPInterfaceProperties ipProperties = networkInterface.GetIPProperties();
                IPAddressCollection dnsAddresses = ipProperties.DnsAddresses;

                foreach (IPAddress dnsAddress in dnsAddresses) {
                    if (forceIPv4 && dnsAddress.AddressFamily != AddressFamily.InterNetwork) { continue; }
                    return dnsAddress;
                }
            }
        }

        return new IPAddress(0);
    }

    public static bool IsPrivate(this IPAddress address) {
        if (address.AddressFamily != AddressFamily.InterNetwork) return false;

        byte[] bytes = address.GetAddressBytes();
        return bytes[0] switch
        {
            10 => true,
            127 => true,
            172 => bytes[1] < 32 && bytes[1] >= 16,
            192 => bytes[1] == 168,
            _ => false,
        };
    }

    public static bool IsApipa(this IPAddress address) {
        if (address.AddressFamily != AddressFamily.InterNetwork) return false;
        byte[] bytes = address.GetAddressBytes();
        if (bytes[0] == 169 && bytes[1] == 254) return true;
        return false;
    }

    public static int SubnetMaskToCidr(IPAddress subnetMask) {
        byte[] bytes = subnetMask.GetAddressBytes();

        int cidr = 0;
        foreach (byte b in bytes) {
            cidr += CountBits(b);
        }

        return cidr;
    }
    private static int CountBits(byte b) {
        int count = 0;
        while (b != 0) {
            count += b & 1;
            b >>= 1;
        }
        return count;
    }

}
