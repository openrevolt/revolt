﻿namespace Revolt.Sniff;

public sealed partial class Sniffer {

    public readonly struct FourTuple(IP sourceIP, IP destinationIP, ushort sourcePort, ushort destinationPort) {
        public readonly IP     sourceIP        = sourceIP;
        public readonly IP     destinationIP   = destinationIP;
        public readonly ushort sourcePort      = sourcePort;
        public readonly ushort destinationPort = destinationPort;

        public static bool operator ==(FourTuple left, FourTuple right) =>
             left.sourcePort == right.sourcePort &&
             left.destinationPort == right.destinationPort &&
             left.sourceIP == right.sourceIP &&
             left.destinationIP == right.destinationIP;

        public static bool operator !=(FourTuple left, FourTuple right) =>
            left.sourcePort != right.sourcePort ||
            left.destinationPort != right.destinationPort ||
            left.sourceIP != right.sourceIP ||
            left.destinationIP != right.destinationIP;

        public override bool Equals(object obj) =>
            obj is FourTuple other && BidirectionallyEquals(this, other);

        public bool Equals(FourTuple fourtuple) =>
            BidirectionallyEquals(this, fourtuple);

        public static bool BidirectionallyEquals(FourTuple left, FourTuple right) =>
            left == right ||
            left.sourcePort == right.destinationPort &&
            left.destinationPort == right.sourcePort &&
            left.sourceIP == right.destinationIP &&
            left.destinationIP == right.sourceIP;

        public override int GetHashCode() {
            int hash = 17;

            if (sourceIP.ipv6 < destinationIP.ipv6 || sourceIP == destinationIP && sourcePort < destinationPort) {
                unchecked {
                    hash = hash * 31 + sourceIP.GetHashCode();
                    hash = hash * 31 + destinationIP.GetHashCode();
                    hash = hash * 31 + sourcePort;
                    hash = hash * 31 + destinationPort;
                }
            }
            else {
                unchecked {
                    hash = hash * 31 + destinationIP.GetHashCode();
                    hash = hash * 31 + sourceIP.GetHashCode();
                    hash = hash * 31 + destinationPort;
                    hash = hash * 31 + sourcePort;
                }
            }

            return hash;
        }
    }
}
