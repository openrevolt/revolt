namespace Revolt.Sniff;

public sealed partial class Sniffer {

    public readonly struct IPPair(IP sourceIP, IP destinationIP) {
        public readonly IP     sourceIP        = sourceIP;
        public readonly IP     destinationIP   = destinationIP;

        public static bool operator ==(IPPair left, IPPair right) =>
             left.sourceIP == right.sourceIP &&
             left.destinationIP == right.destinationIP;

        public static bool operator !=(IPPair left, IPPair right) =>
            left.sourceIP != right.sourceIP ||
            left.destinationIP != right.destinationIP;

        public override bool Equals(object obj) =>
            obj is IPPair other && BidirectionallyEquals(this, other);

        public bool Equals(IPPair pair) =>
            BidirectionallyEquals(this, pair);

        public static bool BidirectionallyEquals(IPPair left, IPPair right) =>
            left == right ||
            left.sourceIP == right.destinationIP &&
            left.destinationIP == right.sourceIP;

        public override int GetHashCode() {
            int hash = 17;

            if (sourceIP.ipv6 < destinationIP.ipv6 || sourceIP == destinationIP) {
                unchecked {
                    hash = hash * 31 + sourceIP.GetHashCode();
                    hash = hash * 31 + destinationIP.GetHashCode();
                }
            }
            else {
                unchecked {
                    hash = hash * 31 + destinationIP.GetHashCode();
                    hash = hash * 31 + sourceIP.GetHashCode();
                }
            }

            return hash;
        }
    }
}
