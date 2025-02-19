namespace Revolt.Sniff;

public sealed partial class Sniffer {

    public readonly struct IPPair(IP a, IP b) {
        public readonly IP a = a;
        public readonly IP b = b;

        public static bool operator ==(IPPair left, IPPair right) =>
             left.a == right.a && left.b == right.b;

        public static bool operator !=(IPPair left, IPPair right) =>
            left.a != right.a || left.b != right.b;

        public override bool Equals(object obj) =>
            obj is IPPair other && BidirectionallyEquals(this, other);

        public bool Equals(IPPair pair) =>
            BidirectionallyEquals(this, pair);

        public static bool BidirectionallyEquals(IPPair left, IPPair right) =>
            left == right || left.a == right.b && left.b == right.a;

        public override int GetHashCode() {
            int hash = 17;

            if (a.ipv6 < b.ipv6 || a == b) {
                unchecked {
                    hash = hash * 31 + a.GetHashCode();
                    hash = hash * 31 + b.GetHashCode();
                }
            }
            else {
                unchecked {
                    hash = hash * 31 + b.GetHashCode();
                    hash = hash * 31 + a.GetHashCode();
                }
            }

            return hash;
        }

        public override string ToString() {
            if (a.IsIPv4Private() && !b.IsIPv4Private()) {
                return $"{a} - {b}";
            }

            if (!a.IsIPv4Private() && b.IsIPv4Private()) {
                return $"{b} - {a}";
            }

            return a.ipv6 < b.ipv6 ? $"{a} - {b}" : $"{b} - {a}";
        }

    }
}
