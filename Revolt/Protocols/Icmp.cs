using System.Net.NetworkInformation;

namespace Revolt.Protocols;
public static class Icmp {
    public const short TIMEDOUT        = -1;
    public const short UNREACHABLE     = -2;
    public const short INVALID_ADDREDD = -3;
    public const short GENERAL_FAILURE = -4;
    public const short ERROR           = -5;
    public const short UNKNOWN         = -9;
    public const short UNDEFINED       = -10;

    public static readonly byte[] ICMP_PAYLOAD = "---- revolt ----"u8.ToArray();

    public static async Task<short[]> PingArrayAsync(string[] name, int timeout) {
        List<Task<short>> tasks = [];
        for (int i = 0; i < name.Length; i++) tasks.Add(PingAsync(name[i], timeout));
        short[] result = await Task.WhenAll(tasks);
        return result;
    }

    private static async Task<short> PingAsync(string hostname, int timeout) {
        using Ping p = new Ping();

        try {
            PingReply reply = await p.SendPingAsync(hostname, timeout, ICMP_PAYLOAD);

            return (int)reply.Status switch {
                (int)IPStatus.DestinationUnreachable or
                (int)IPStatus.DestinationHostUnreachable or
                (int)IPStatus.DestinationNetworkUnreachable => UNREACHABLE,

                (int)IPStatus.Success  => (short)reply.RoundtripTime,
                (int)IPStatus.TimedOut => TIMEDOUT,
                11050                  => GENERAL_FAILURE,
                _                      => UNKNOWN,
            };
        }
        catch (ArgumentException) {
            return INVALID_ADDREDD;
        }
        catch (PingException) {
            return ERROR;
        }
        catch (Exception) {
            return UNKNOWN;
        }
    }
}