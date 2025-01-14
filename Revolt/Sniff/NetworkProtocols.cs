using System.Collections.Frozen;

namespace Revolt.Sniff;

public sealed partial class Sniffer {

    private static readonly FrozenDictionary<ushort, string> networkProtocolNames = new Dictionary<ushort, string>() {
        {0x0800, "IPv4"},
        {0x0806, "ARP"},
        {0x0842, "WoL"},
        {0x22F3, "IETF TRILL"},
        {0x8035, "RARP"},
        {0x809B, "AppleTalk"},
        {0x86DD, "IPv6"},
        {0x8808, "Ethernet Flow Control"},
        {0x8809, "LACP"},
        {0x8847, "MPLS Unicast"},
        {0x8848, "MPLS Multicast"},
        {0x8863, "PPPoE Discovery"},
        {0x8864, "PPPoE Session"},
        {0x8870, "Jumbo Frames"},
        {0x887B, "HomePlug 1.0 MME"},
        {0x888E, "EAP over LAN"},
        {0x8892, "PROFINET"},
        {0x889A, "HyperSCSI"},
        {0x88A2, "ATA over Ethernet"},
        {0x88A4, "EtherCAT"},
        {0x88B5, "IEEE 802.11r"},
        {0x88B8, "AV Bridging"},
        {0x88CC, "LLDP"},
        {0x88E5, "MACSec"},
        {0x88F7, "PTP"},
        {0x8902, "IEEE 802.1ag"},
        {0x8906, "FCoE"},
        {0x890D, "IEEE 802.1AE MACSec"},
        {0x8915, "RoCE"},
        {0x891D, "TTEthernet"},
        {0x892F, "HSR"},
        {0x8940, "NSH"},
        {0x8941, "VXLAN with GPE"},
        {0x9000, "Loopback"},
        {0x9100, "VLAN (Q-in-Q)"},
    }.ToFrozenDictionary();

    public static string GetNetworkProtocolName(ushort etherType) =>
        networkProtocolNames.TryGetValue(etherType, out string name) ? name : "--";

}