//IANA protocol numbers

namespace Revolt.Sniff;

public sealed partial class Sniffer {
    public enum TransportProtocol : byte {
        HOPOPT          = 0,
        ICMP            = 1,
        IGMP            = 2,
        GGP             = 3,
        IP_in_IP        = 4,
        ST              = 5,
        TCP             = 6,
        CBT             = 7,
        EGP             = 8,
        IGP             = 9,
        BBN_RCC_MON     = 10,
        NVP_II          = 11,
        PUP             = 12,
        ARGUS           = 13,
        EMCON           = 14,
        XNET            = 15,
        CHAOS           = 16,
        UDP             = 17,
        MUX             = 18,
        DCN_MEAS        = 19,
        HMP             = 20,
        PRM             = 21,
        XNS_IDP         = 22,
        TRUNK_1         = 23,
        TRUNK_2         = 24,
        LEAF_1          = 25,
        LEAF_2          = 26,
        RDP             = 27,
        IRTP            = 28,
        ISO_TP4         = 29,
        NETBLT          = 30,
        MFE_NSP         = 31,
        MERIT_INP       = 32,
        DCCP            = 33,
        _3PC            = 34,
        IDPR            = 35,
        XTP             = 36,
        DDP             = 37,
        IDPR_CMTP       = 38,
        TP_pp           = 39,
        IL              = 40,
        IPv6            = 41,
        SDRP            = 42,
        IPv6_Route      = 43,
        IPv6_Frag       = 44,
        IDRP            = 45,
        RSVP            = 46,
        GRE             = 47,
        DSR             = 48,
        BNA             = 49,
        ESP             = 50,
        AH              = 51,
        I_NLSP          = 52,
        SwIPe           = 53,
        NARP            = 54,
        MOBILE          = 55,
        TLSP            = 56,
        SKIP            = 57,
        ICMPv6          = 58,
        IPv6_NoNxt      = 59,
        IPv6_Opts       = 60,
        CFTP            = 62,
        SAT_EXPAK       = 64,
        KRYPTOLAN       = 65,
        RVD             = 66,
        IPPC            = 67,
        SAT_MON         = 69,
        VISA            = 70,
        IPCU            = 71,
        CPNX            = 72,
        CPHB            = 73,
        WSN             = 74,
        PVP             = 75,
        BR_SAT_MON      = 76,
        SUN_ND          = 77,
        WB_MON          = 78,
        WB_EXPAK        = 79,
        ISO_IP          = 80,
        VMTP            = 81,
        SECURE_VMTP     = 82,
        VINES           = 83,
        TTP             = 84,
        NSFNET_IGP      = 85,
        DGP             = 86,
        TCF             = 87,
        EIGRP           = 88,
        OSPF            = 89,
        Sprite_RPC      = 90,
        LARP            = 91,
        MTP             = 92,
        AX_25           = 93,
        OS              = 94,
        MICP            = 95,
        SCC_SP          = 96,
        ETHERIP         = 97,
        ENCAP           = 98,
        GMTP            = 100,
        IFMP            = 101,
        PNNI            = 102,
        PIM             = 103,
        ARIS            = 104,
        SCPS            = 105,
        QNX             = 106,
        A_N             = 107,
        IPComp          = 108,
        SNP             = 109,
        Compaq_Peer     = 110,
        IPX_in_IP       = 111,
        VRRP            = 112,
        PGM             = 113,
        L2TP            = 115,
        DDX             = 116,
        IATP            = 117,
        STP             = 118,
        SRP             = 119,
        UTI             = 120,
        SMP             = 121,
        SM              = 122,
        PTP             = 123,
        IS_IS           = 124,
        FIRE            = 125,
        CRTP            = 126,
        CRUDP           = 127,
        SSCOPMCE        = 128,
        IPLT            = 129,
        SPS             = 130,
        PIPE            = 131,
        SCTP            = 132,
        FC              = 133,
        Mobility_Header = 135,
        UDPLite         = 136,
        MPLS_in_IP      = 137,
        manet           = 138,
        HIP             = 139,
        Shim6           = 140,
        WESP            = 141,
        ROHC            = 142,
        Ethernet        = 143,
        AGGFRAG         = 144,
        NSH             = 145
    }

    public static readonly string[] transportProtocolNames = [
        "HOPOPT",          //0
        "ICMP",            //1
        "IGMP",            //2
        "GGP",             //3
        "IP-in-IP ",       //4
        "ST",              //5
        "TCP",             //6
        "CBT",             //7
        "EGP",             //8
        "IGP",             //9
        "BBN-RCC-MON",     //10
        "NVP-II",          //11
        "PUP",             //12
        "ARGUS",           //13
        "EMCON",           //14
        "XNET",            //15
        "CHAOS",           //16
        "UDP",             //17
        "MUX",             //18
        "DCN-MEAS",        //19
        "HMP",             //20
        "PRM",             //21
        "XNS-IDP",         //22
        "TRUNK-1",         //23
        "TRUNK-2",         //24
        "LEAF-1",          //25
        "LEAF-2",          //26
        "RDP",             //27
        "IRTP",            //28
        "ISO-TP4",         //29
        "NETBLT",          //30
        "MFE-NSP",         //31
        "MERIT-INP",       //32
        "DCCP",            //33
        "3PC",             //34
        "IDPR",            //35
        "XTP",             //36
        "DDP",             //37
        "IDPR-CMTP",       //38
        "TP++",            //39
        "IL",              //40
        "IPv6",            //41
        "SDRP",            //42
        "IPv6-Route",      //43
        "IPv6-Frag",       //44
        "IDRP",            //45
        "RSVP",            //46
        "GRE",             //47
        "DSR",             //48
        "BNA",             //49
        "ESP",             //50
        "AH",              //51
        "I-NLSP",          //52
        "SwIPe",           //53
        "NARP",            //54
        "MOBILE",          //55
        "TLSP",            //56
        "SKIP",            //57
        "ICMPv6",          //58
        "IPv6-NoNxt",      //59
        "IPv6-Opts",       //60
        String.Empty,      //61
        "CFTP",            //62
        String.Empty,      //63
        "SAT-EXPAK",       //64
        "KRYPTOLAN",       //65
        "RVD",             //66
        "IPPC",            //67
        String.Empty,      //68
        "SAT-MON ",        //69
        "VISA",            //70
        "IPCU",            //71
        "CPNX",            //72
        "CPHB",            //73
        "WSN",             //74
        "PVP",             //75
        "BR-SAT-MON",      //76
        "SUN-ND",          //77
        "WB-MON",          //78
        "WB-EXPAK",        //79
        "ISO-IP",          //80
        "VMTP",            //81
        "SECURE-VMTP",     //82
        "VINES",           //83
        "TTP",             //84
        "NSFNET-IGP",      //85
        "DGP",             //86
        "TCF",             //87
        "EIGRP",           //88
        "OSPF",            //89
        "Sprite-RPC",      //90
        "LARP",            //91
        "MTP",             //92
        "AX.25",           //93
        "OS",              //94
        "MICP",            //95
        "SCC-SP",          //96
        "ETHERIP",         //97
        "ENCAP",           //98
        String.Empty,      //99
        "GMTP",            //100
        "IFMP",            //101
        "PNNI",            //102
        "PIM",             //103
        "ARIS",            //104
        "SCPS",            //105
        "QNX",             //106
        "A/N",             //107
        "IPComp",          //108
        "SNP",             //109
        "Compaq-Peer",     //110
        "IPX-in-IP",       //111
        "VRRP",            //112
        "PGM",             //113
        String.Empty,      //114
        "L2TP",            //115
        "DDX",             //116
        "IATP",            //117
        "STP",             //118
        "SRP",             //119
        "UTI",             //120
        "SMP",             //121
        "SM",              //122
        "PTP",             //123
        "IS-IS",           //124
        "FIRE",            //125
        "CRTP",            //126
        "CRUDP",           //127
        "SSCOPMCE",        //128
        "IPLT",            //129
        "SPS",             //130
        "PIPE",            //131
        "SCTP",            //132
        "FC",              //133
        String.Empty,      //134
        "Mobility Header", //135
        "UDPLite",         //136
        "MPLS-in-IP",      //137
        "manet",           //138
        "HIP",             //139
        "Shim6",           //140
        "WESP",            //141
        "ROHC",            //142
        "Ethernet",        //143
        "AGGFRAG",         //144
        "NSH",             //145
        String.Empty,      //146
        String.Empty,      //147
        String.Empty,      //148
        String.Empty,      //149
        String.Empty,      //150
        String.Empty,      //151
        String.Empty,      //152
        String.Empty,      //153
        String.Empty,      //154
        String.Empty,      //155
        String.Empty,      //156
        String.Empty,      //157
        String.Empty,      //158
        String.Empty,      //159
        String.Empty,      //160
        String.Empty,      //161
        String.Empty,      //162
        String.Empty,      //163
        String.Empty,      //164
        String.Empty,      //165
        String.Empty,      //166
        String.Empty,      //167
        String.Empty,      //168
        String.Empty,      //169
        String.Empty,      //170
        String.Empty,      //171
        String.Empty,      //172
        String.Empty,      //173
        String.Empty,      //174
        String.Empty,      //175
        String.Empty,      //176
        String.Empty,      //177
        String.Empty,      //178
        String.Empty,      //179
        String.Empty,      //180
        String.Empty,      //181
        String.Empty,      //182
        String.Empty,      //183
        String.Empty,      //184
        String.Empty,      //185
        String.Empty,      //186
        String.Empty,      //187
        String.Empty,      //188
        String.Empty,      //189
        String.Empty,      //190
        String.Empty,      //191
        String.Empty,      //192
        String.Empty,      //193
        String.Empty,      //194
        String.Empty,      //195
        String.Empty,      //196
        String.Empty,      //197
        String.Empty,      //198
        String.Empty,      //199
        String.Empty,      //200
        String.Empty,      //201
        String.Empty,      //202
        String.Empty,      //203
        String.Empty,      //204
        String.Empty,      //205
        String.Empty,      //206
        String.Empty,      //207
        String.Empty,      //208
        String.Empty,      //209
        String.Empty,      //210
        String.Empty,      //211
        String.Empty,      //212
        String.Empty,      //213
        String.Empty,      //214
        String.Empty,      //215
        String.Empty,      //216
        String.Empty,      //217
        String.Empty,      //218
        String.Empty,      //219
        String.Empty,      //220
        String.Empty,      //221
        String.Empty,      //222
        String.Empty,      //223
        String.Empty,      //224
        String.Empty,      //225
        String.Empty,      //226
        String.Empty,      //227
        String.Empty,      //228
        String.Empty,      //229
        String.Empty,      //230
        String.Empty,      //231
        String.Empty,      //232
        String.Empty,      //233
        String.Empty,      //234
        String.Empty,      //235
        String.Empty,      //236
        String.Empty,      //237
        String.Empty,      //238
        String.Empty,      //239
        String.Empty,      //240
        String.Empty,      //241
        String.Empty,      //242
        String.Empty,      //243
        String.Empty,      //244
        String.Empty,      //245
        String.Empty,      //246
        String.Empty,      //247
        String.Empty,      //248
        String.Empty,      //249
        String.Empty,      //250
        String.Empty,      //251
        String.Empty,      //252
        String.Empty,      //253
        String.Empty,      //254
        String.Empty       //255
    ];
}