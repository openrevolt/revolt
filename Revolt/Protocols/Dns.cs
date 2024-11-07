namespace Revolt.Protocols;

public static class Dns {
    public enum TransportMethod : byte {
        auto  = 0,
        udp   = 1,
        tcp   = 2,
        tls   = 5,
        https = 6,
        quic  = 7,
    }

    public enum RecordType : byte {
        A     = 1,
        NS    = 2,
        CNAME = 5,
        SOA   = 6,
        PTR   = 12,
        MX    = 15,
        TXT   = 16,
        AAAA  = 28,
        SRV   = 33,
        NSEC  = 47,
        ANY   = 255
    }

    public enum Class : byte {
        IN = 1, //Internet
        CS = 2, //CSNET -Obsolete
        CH = 3, //Chaos -Obsolete
        HS = 4  //Hesiod
    }

    public static readonly string[] types = [
        "A",
        "AAAA",
        "NS",
        "CNAME",
        "SOA",
        "PTR",
        "MX",
        "TXT",
        "SRV",
        "ANY"
    ];

    public static readonly string[] typeFullNames = [
        "IPv4 Address",
        "IPv6 Address",
        "Name Server",
        "Canonical Name",
        "Start Of Author.",
        "Pointer",
        "Mail Exchange",
        "Text",
        "Service",
        "All types known"
    ];
    public static readonly byte[][] typesColors = [
        [236, 91, 19],
        [236, 200, 19],
        [164, 236, 19],
        [19, 236, 91],
        [43, 173, 238],
        [81, 109, 251],
        [137, 81, 251],
        [205, 43, 238],
        [236, 19, 164],
        [255, 255, 255]
    ];

    public static void Resolve(
        String name,
        RecordType type = RecordType.A,
        int timeout = 2000,
        TransportMethod transport = TransportMethod.auto,
        bool isStandard = false,
        bool isInverse = false,
        bool showServerStatus = false,
        bool isTruncated = false,
        bool isRecursive = false
        ) {
        
    }
}