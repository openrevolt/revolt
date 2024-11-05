using System.Net.NetworkInformation;

namespace Revolt.Protocols;

public sealed class PingItem : IDisposable {
    public string  host;
    public short   status;
    public short[] history;
    public Ping    ping;

    public void Dispose() {
        ping?.Dispose();
        history = null;
        GC.SuppressFinalize(this);
    }
}

public static class Icmp {
    public const short TIMEDOUT        = -1;
    public const short UNREACHABLE     = -2;
    public const short INVALID_ADDRESS = -3;
    public const short GENERAL_FAILURE = -4;
    public const short ERROR           = -5;
    public const short UNKNOWN         = -8;
    public const short UNDEFINED       = -9;

    public static readonly byte[] ICMP_PAYLOAD = "---- revolt ----"u8.ToArray();

    public static async Task<short[]> PingArrayAsync(List<PingItem> list, int timeout) {
        List<Task<short>> tasks = [];
        for (int i = 0; i < list.Count; i++) tasks.Add(PingAsync(list[i], timeout));
        short[] result = await Task.WhenAll(tasks);
        return result;
    }

    private static async Task<short> PingAsync(PingItem host, int timeout) {
        try {
            PingReply reply = await host.ping.SendPingAsync(host.host, timeout, ICMP_PAYLOAD);

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
            return INVALID_ADDRESS;
        }
        catch (PingException) {
            return ERROR;
        }
        catch (Exception) {
            return UNKNOWN;
        }
    }
}