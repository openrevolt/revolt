using System.Net.Sockets;
using System.Net;
using System.Net.Security;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using System.Collections.Frozen;

namespace Revolt.Protocols;

public static class Dns {
    public enum TransportMethod : byte {
        Auto  = 0,
        UDP   = 1,
        TCP   = 2,
        TLS   = 3,
        HTTPS = 4,
        QUIC  = 5,
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

    public struct Answer {
        public RecordType type;
        public int ttl;
        public ushort length;
        public byte[] name;
        public string answerString;
        public bool isAuthoritative;
        public bool isAdditional;
        public byte error;
    }

    public static readonly FrozenDictionary<int, string> errorMessages = new Dictionary<int, string>() {
        { 0, "no error" },
        { 1, "query format error" },
        { 2, "server failure" },
        { 3, "no such name" },
        { 4, "function not implemented" },
        { 5, "refused" },
        { 6, "name should not exist" },
        { 7, "RRset should not exist" },
        { 8, "server not authoritative for the zone" },
        { 9, "name not in zone" },
        { 254, "invalid response" }
    }.ToFrozenDictionary();

    public static readonly string[] typeStrings = ["A", "AAAA", "NS", "CNAME", "SOA", "PTR", "MX", "TXT", "SRV", "ANY"];

    public static readonly string[] typeFullNames = [
        "IPv4 Address",
        "IPv6 Address",
        "Name Server",
        "Canonical Name",
        "Start Of Authority",
        "Pointer",
        "Mail Exchange",
        "Text",
        "Service",
        "All types known"
    ];

    public static readonly RecordType[] types = [
        RecordType.A,
        RecordType.AAAA,
        RecordType.NS,
        RecordType.CNAME,
        RecordType.SOA,
        RecordType.PTR,
        RecordType.MX,
        RecordType.TXT,
        RecordType.SRV,
        RecordType.ANY
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

    public static Answer[] Resolve(string name, string server, RecordType type, int timeout, TransportMethod transport, bool isStandard, bool isInverse, bool showServerStatus, bool isTruncated, bool isRecursive) {
        if (transport == TransportMethod.HTTPS) {
            ResolveOverHttps(name, server, type);
        }
        else if (transport == TransportMethod.QUIC) {
            ResolveOverQuic(name, server, type).GetAwaiter().GetResult();
        }
        
        byte[] query = ConstructQuery([name], type, isStandard, isInverse, showServerStatus, isTruncated, isRecursive);

        byte[] response = transport switch {
            TransportMethod.UDP   => ResolveOverUdp(query, server, timeout),
            TransportMethod.TCP   => ResolveOverTcp(query, server, timeout),
            TransportMethod.TLS   => ResolveOverTls(query, server, timeout),
            TransportMethod.Auto  => query.Length < 4096 ? ResolveOverUdp(query, server, timeout) : ResolveOverTcp(query, server, timeout),
            _                     => query.Length < 4096 ? ResolveOverUdp(query, server, timeout) : ResolveOverTcp(query, server, timeout)
        };

        if (response is null) {
            return [new Answer() {
                error = 2
            }];
        }

        Answer[] answers = ParseAnswers(response, out _, out _, out _);
        return answers;
    }

    private static byte[] ResolveOverUdp(byte[] query, string server, int timeout) {
        if (!IPAddress.TryParse(server, out IPAddress serverIp)) {
            serverIp = GetLocalDnsAddress(true);
        }

        try {
            IPEndPoint remoteEndPoint = new IPEndPoint(serverIp, 53);
            using Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);

            socket.Connect(remoteEndPoint);
            socket.ReceiveTimeout = timeout;
            socket.Send(query);

            byte[] response = new byte[4096];
            int receivedLength = socket.Receive(response);
            Array.Resize(ref response, receivedLength);

            socket.Close();

            return response;
        }
        catch {
            return null;
        }
    }

    private static byte[] ResolveOverTcp(byte[] query, string server, int timeout) {
        if (!IPAddress.TryParse(server, out IPAddress serverIp)) {
            serverIp = GetLocalDnsAddress(true);
        }

        try {
            IPEndPoint remoteEndPoint = new IPEndPoint(serverIp, 53);

            using Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(remoteEndPoint);
            socket.ReceiveTimeout = timeout;

            byte[] lengthBytes = BitConverter.GetBytes((short)query.Length);
            if (BitConverter.IsLittleEndian) Array.Reverse(lengthBytes);

            byte[] message = new byte[lengthBytes.Length + query.Length];
            Buffer.BlockCopy(lengthBytes, 0, message, 0, lengthBytes.Length);
            Buffer.BlockCopy(query, 0, message, lengthBytes.Length, query.Length);
            socket.Send(message);

            byte[] responseLengthBytes = new byte[2];
            socket.Receive(responseLengthBytes, 2, SocketFlags.None);
            if (BitConverter.IsLittleEndian) Array.Reverse(responseLengthBytes);

            short responseLength = BitConverter.ToInt16(responseLengthBytes, 0);
            byte[] response = new byte[responseLength];
            socket.Receive(response, responseLength, SocketFlags.None);

            socket.Close();

            return response;
        }
        catch {
            return null;
        }
    }

    private static byte[] ResolveOverTls(byte[] query, string server, int timeout) {
        if (!IPAddress.TryParse(server, out IPAddress serverIp)) {
            serverIp = GetLocalDnsAddress(true);
        }

        try {
            IPEndPoint remoteEndPoint = new IPEndPoint(serverIp, 853);

            using Socket socket = new Socket(remoteEndPoint.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(remoteEndPoint);

            using Stream stream = new NetworkStream(socket, ownsSocket: true);

            using SslStream secureStream = new SslStream(
                stream,
                false,
                (sender, certificate, chain, errors) => errors == SslPolicyErrors.None,
                null,
                EncryptionPolicy.RequireEncryption
            );

            secureStream.ReadTimeout = timeout;
            secureStream.AuthenticateAsClient(serverIp.ToString());

            secureStream.Write([(byte)(query.Length >> 8), (byte)query.Length]); //length
            secureStream.Flush();

            secureStream.Write(query);
            secureStream.Flush();

            byte[] responseLengthBytes = new byte[2];
            secureStream.ReadExactly(responseLengthBytes, 0, 2);
            if (BitConverter.IsLittleEndian) Array.Reverse(responseLengthBytes);

            short responseLength = BitConverter.ToInt16(responseLengthBytes, 0);
            byte[] response = new byte[responseLength];
            secureStream.ReadExactly(response, 0, responseLength);

            secureStream.Close();
            socket.Close();

            return response;
        }
        catch {
            return null;

        }
    }

    private static Answer[] ResolveOverHttps(string name, string server, RecordType type) {
        try {
            string url = $"https://{server}/dns-query?name={name}&type={type}";
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Accept", "application/dns-json");

            HttpResponseMessage responseMessage = client.GetAsync(url).GetAwaiter().GetResult();
            responseMessage.EnsureSuccessStatusCode();

            string data = responseMessage.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            return ParseJsonAnswer(data);
        }
        catch {
            return null;
        }
    }

    private static async Task<Answer[]> ResolveOverQuic(string name, string server, RecordType type) {
        string url = $"https://{server}/dns-query?name={name}&type={type}";

        using HttpClient client = new HttpClient {
            DefaultRequestVersion = HttpVersion.Version30,
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact
        };

        /*try*/ {
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string body = await response.Content.ReadAsStringAsync();

            return ParseJsonAnswer(body);
        }
        /*catch {
            return null;
        }*/
    }

    private static byte[] ConstructQuery(string[] domainNames, RecordType type, bool isStandard, bool isInverse, bool isServerStatus, bool isTruncated, bool isRecursive) {
        ushort questions = (ushort)domainNames.Length;

        int len = 12;
        string[][] labels = new string[domainNames.Length][];

        for (int i = 0; i < labels.Length; i++) {
            labels[i] = domainNames[i].Split('.');

            for (int j = 0; j < labels[i].Length; j++) {
                len += labels[i][j].Length + 1;
            }
            len += 5; //1[null] + 2[type] + 2[class]
        }

        byte[] query = new byte[len];

        //transaction id
        Random rand = new Random();
        query[0] = (byte)rand.Next(0, 255);
        query[1] = (byte)rand.Next(0, 255);

        //DNS query flags
        if (isStandard) query[2] |= 0b10000000;
        if (isInverse) query[2] |= 0b01000000;
        if (isServerStatus) query[2] |= 0b00100000;
        if (isTruncated) query[2] |= 0b00000010;
        if (isRecursive) query[2] |= 0b00000001;

        //questions
        query[4] = (byte)(questions << 8);
        query[5] = (byte)questions;

        //answer RRs
        ushort answers = 0;
        query[6] = (byte)(answers << 8);
        query[7] = (byte)answers;

        //authority RRs
        ushort authority = 0;
        query[8] = (byte)(authority << 8);
        query[9] = (byte)authority;

        //additional RRs
        ushort additional = 0;
        query[10] = (byte)(additional << 8);
        query[11] = (byte)additional;

        short index = 12;

        for (int i = 0; i < labels.Length; i++) {
            for (int j = 0; j < labels[i].Length; j++) {
                query[index++] = (byte)labels[i][j].Length;
                for (int k = 0; k < labels[i][j].Length; k++) {
                    query[index++] = (byte)labels[i][j][k];
                }
            }

            query[index++] = 0x00; //null termination

            query[index++] = 0x00; //type
            query[index++] = (byte)type;

            query[index++] = 0x00; //class
            query[index++] = (byte)Class.IN;
        }

        return query;
    }

    private static Answer[] ParseAnswers(byte[] response, out ushort answerCount, out ushort authorityCount, out ushort additionalCount) {
        if (response.Length < 12) {
            answerCount = 0;
            authorityCount = 0;
            additionalCount = 0;
            return [new Answer { error = 254 }];
        }

        //ushort transactionId = BitConverter.ToUInt16(response, 0);
        //ushort query = (ushort)((response[2] << 8) | response[3]);
        ushort questionCount = (ushort)((response[4] << 8) | response[5]);
        answerCount = (ushort)((response[6] << 8) | response[7]);
        authorityCount = (ushort)((response[8] << 8) | response[9]);
        additionalCount = (ushort)((response[10] << 8) | response[11]);

        //bool isResponse = (response[2] & 0b10000000) == 0b10000000;

        byte error = (byte)(response[3] & 0b00001111);
        if (error > 0) {
            return [new Answer { error = error }];
        }

        int index = 12;

        for (int i = 0; i < questionCount; i++) { //skip questions
            while (index < response.Length) {
                byte len = response[index++];
                if (len == 0) break;
                index += len;
            }
            index += 4; //skip type and class
        }

        Answer[] result = new Answer[answerCount + authorityCount + additionalCount];

        if (result.Length == 0) {
            return result;
        }

        int count = 0;
        while (index < response.Length) {
            Answer ans = new Answer();

            index += 2; //skip name

            ans.type = (RecordType)((response[index] << 8) | response[index + 1]);
            index += 2;

            index += 2; //skip class

            ans.ttl = (response[index] << 24) | (response[index + 1] << 16) | (response[index + 2] << 8) | response[index + 3];
            index += 4;

            ans.length = (ushort)((response[index] << 8) | response[index + 1]);
            index += 2;

            if (ans.type == RecordType.MX) {
                ans.length -= 2;
                index += 2; //skip preference
            }

            if (ans.length > response.Length - index) {
                ans.error = 254;
                break;
            }
            ans.name = new byte[ans.length];
            Array.Copy(response, index, ans.name, 0, ans.length);
            index += ans.length;

            switch (ans.type) {
            case RecordType.A:
                ans.answerString = String.Join(".", ans.name);
                break;

            case RecordType.NS:
            case RecordType.CNAME:
            case RecordType.SOA:
            case RecordType.PTR:
            case RecordType.MX:
            case RecordType.TXT:
                ans.answerString = ExtractLabel(ans.name, 0, response, out _);
                break;

            case RecordType.AAAA:
                if (ans.name.Length != 16) break;
                StringBuilder ipv6Builder = new StringBuilder();
                for (int j = 0; j < 16; j += 2) {
                    if (j > 0) ipv6Builder.Append(':');
                    ushort word = (ushort)((ans.name[j] << 8) | ans.name[j + 1]);
                    ipv6Builder.Append(word.ToString("x4"));
                }
                ans.answerString = ipv6Builder.ToString();
                break;

            case RecordType.SRV:
                ans.answerString = String.Join(".", ans.name);
                break;
            }

            if (count > answerCount + authorityCount) {
                ans.isAdditional = true;
            }
            else if (count > answerCount) {
                ans.isAuthoritative = true;
            }

            result[count++] = ans;
        }

        return result;
    }

    private static Answer[] ParseJsonAnswer(string json) {
        try {
            JsonDocument jsonDocument = JsonDocument.Parse(json);

            JsonElement root = jsonDocument.RootElement;
            if (!root.TryGetProperty("Answer", out JsonElement answersArray)) {
                return [new Answer { error = 1 }];
            }

            List<Answer> answers = [];

            foreach (JsonElement answerElement in answersArray.EnumerateArray()) {
                int ttl = answerElement.TryGetProperty("TTL", out JsonElement ttlElement) ? ttlElement.GetInt32() : 0;
                int type = answerElement.TryGetProperty("type", out JsonElement typeInt) ? typeInt.GetInt32() : 0;
                string answerString = answerElement.TryGetProperty("data", out JsonElement dataElement) ? dataElement.GetString() : string.Empty;

                Answer answer = new Answer {
                    ttl = ttl,
                    type = (RecordType)type,
                    answerString = answerString
                };

                if (!string.IsNullOrEmpty(answer.answerString)) {
                    answer.name = Encoding.UTF8.GetBytes(answer.answerString);
                }

                answers.Add(answer);
            }

            return answers.ToArray();
        }
        catch (JsonException) {
            // Return an error Answer if parsing fails
            return [new Answer { error = 255 }];
        }
    }

    private static string ExtractLabel(byte[] labels, int offset, byte[] response, out bool isNullTerminated) {
        if (labels.Length - offset < 2) {
            isNullTerminated = false;
            return String.Empty;
        }

        StringBuilder builder = new StringBuilder();

        int index = offset;
        while (index < labels.Length) {

            if (index > 0 && (labels[index] & 0xC0) != 0xc0) builder.Append('.');

            switch (labels[index]) {
            case 0x00: //null terminated
                string domainName = builder.ToString();
                if (domainName[^1] == '.') domainName = domainName[..^1];
                isNullTerminated = true;
                return domainName;

            case 0xc0: //pointer
                builder.Append(ExtractLabel(response, labels[index + 1], response, out bool nt));
                if (nt) {
                    isNullTerminated = true;
                    return builder.ToString();
                }
                index++;
                break;

            default:
                int labelLength = labels[index];
                for (int i = index + 1; i < index + labels[index] + 1; i++) {
                    if (i >= labels.Length) break;
                    if (labels[i] == 0) break;
                    builder.Append(Convert.ToChar(labels[i]));
                }
                index += labelLength;

                break;
            }

            index++;
        }

        isNullTerminated = false;
        return builder.ToString();
    }

    private static IPAddress GetLocalDnsAddress(bool forceIPv4 = false) {
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
}