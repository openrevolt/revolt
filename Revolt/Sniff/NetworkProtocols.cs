﻿using System.Collections.Frozen;
using System.Numerics;

namespace Revolt.Sniff;

public sealed partial class Sniffer {

    public enum NetworkProtocol : ushort {
        none   = 0,
        IPv4   = 0x0800,
        ARP    = 0x0806,
        WoL    = 0x0842,
        Tagged = 0x8100,
        IPv6   = 0x86DD,
        LLDP   = 0x88CC
    }

    private static readonly FrozenDictionary<ushort, string> networkProtocolNames = new Dictionary<ushort, string>() {
        { 0x0600, "XEROX NS IDP" },
        { 0x0800, "IPv4 - Internet Protocol v4" },
        { 0x0801, "X.75 Internet" },
        { 0x0802, "NBS Internet" },
        { 0x0803, "ECMA Internet" },
        { 0x0804, "Chaosnet" },
        { 0x0805, "X.25 Level 3" },
        { 0x0806, "ARP - Address Resolution Protocol" },
        { 0x0807, "XNS Compatability" },
        { 0x0808, "Frame Relay ARP (802.3)" },
        { 0x081C, "Symbolics Private" },
        { 0x0A00, "Xerox PUP (802.3)" },
        { 0x0A01, "PUP Addr Trans" },
        { 0x0BAD, "Banyan VINES" },
        { 0x0BAE, "VINES Loopback" },
        { 0x0BAF, "VINES Echo" },
        { 0x8100, "VLAN Tag" }, //!
        { 0x0842, "WoL - Wake on LAN" },
        { 0x22EA, "Stream Reservation Protocol" },
        { 0x22F0, "AVTP -  Audio Video Transport Protocol" },
        { 0x22F3, "IETF TRILL" },
        { 0x22F4, "L2-IS-IS" },
        { 0x6002, "DEC MOP RC" },
        { 0x6003, "DECnet Phase IV, DNA Routing" },
        { 0x6004, "DEC LAT" },
        { 0x6558, "Trans Ether Bridging" },
        { 0x6559, "Raw Frame Relay" },
        { 0x0003, "Cronus VLN" },
        { 0x0004, "Cronus Direct" },
        { 0x8035, "RARP - Reverse Address Resolution Protocol " },
        { 0x809B, "AppleTalk/EtherTalk" },
        { 0x86DD, "IPv6 - Internet Protocol v6" },
        { 0x876B, "TCP/IP Compression" },
        { 0x876C, "IP Autonomous Systems" },
        { 0x876D, "Secure Data" },
        { 0x8808, "EPON -  Ethernet Passive Optical Network" },
        { 0x8809, "LACP - Link Aggregation Control Protocol" },
        { 0x880B, "PPP - Point to Point Protocol" },
        { 0x880C, "GSMP - General Switch Management Protocol" },
        { 0x8822, "Ethernet NIC hardware and software testing" },
        { 0x8847, "MPLS Unicast" },
        { 0x8848, "MPLS Multicast" },
        { 0x8861, "MCAP - Multicast Channel Allocation Protocol" },
        { 0x8863, "PPPoE Discovery" },
        { 0x8864, "PPPoE Session" },
        { 0x8870, "Jumbo Frames" },
        { 0x887B, "HomePlug 1.0 MME" },
        { 0x888E, "EAP over LAN (802.1X)" },
        { 0x8892, "PROFINET" },
        { 0x889A, "HyperSCSI" },
        { 0x88A2, "ATA over Ethernet" },
        { 0x88A4, "EtherCAT" },
        { 0x88A8, "S-Tag (802.1Q)" },
        { 0x88B5, "IEEE (802.11r)" },
        { 0x88B8, "AV Bridging" },
        { 0x88CC, "LLDP - Link Layer Discovery Protocol (802.1AB)" },
        { 0x88E5, "Media Access Control Security (802.1AE)" },
        { 0x88F5, "MVRP - Multiple VLAN Registration Protocol (802.1Q)" },
        { 0x88F6, "MMRP - Multiple MAC Registration Protocol (802.1Q)" },
        { 0x88F7, "PTP - Precision Time Protocol" },
        { 0x8902, "CFM - Connectivity Fault Management (802.1ag)" },
        { 0x8906, "FCoE" },
        { 0x890D, "MACSec (802.1AE)" },
        { 0x8915, "RoCE" },
        { 0x891D, "TTEthernet" },
        { 0x892F, "HSR - High-availability Seamless Redundancy" },
        { 0x8940, "NSH (802.1Qbg)" },
        { 0x8941, "VXLAN with GPE" },
        { 0x894F, "NSH - Network Services Headers" },
        { 0x9000, "Loopback" },
        { 0x9100, "VLAN (Q-in-Q)" },
        { 0x9A22, "Multi-Topology" },
        { 0xA0ED, "LoWPAN encapsulation" },
    }.ToFrozenDictionary();

    public static string GetNetworkProtocolName(ushort etherType) =>
        networkProtocolNames.TryGetValue(etherType, out string name) ? name : "-";

}