using System;
using System.Collections.Concurrent;

namespace Revolt.Sniff;

public sealed partial class Sniffer {
    public IndexedDictionary<IPPair, TcpStatCount> tcpStatCount = new IndexedDictionary<IPPair, TcpStatCount>();

    public void AnalyzeTCP() {
        foreach (ConcurrentQueue<Segment> stream in streams.Values) {
            if (stream.Count == 0) return;

            IPPair ips = new IPPair(stream.First().fourTuple.sourceIP, stream.First().fourTuple.destinationIP);

            Analyze3WH(stream, ips);
            AnalyzeSequenceNumbers(stream, ips);
        }
    }

    private void Analyze3WH(ConcurrentQueue<Segment> segments, IPPair ips) {
        if (segments.Count < 3) return;

        int index = 0;
        long timestampSyn = 0, timestampAck = 0;
        IP sourceIP = default;
        IP destinationIP = default;

        foreach (Segment segment in segments) {
            ushort flags = (ushort)(segment.flags & 0x0fff);

            if (index == 0 && flags == 0b00000000_00000010) { //SYN
                timestampSyn = segment.timestamp;
                sourceIP = segment.fourTuple.sourceIP;
                destinationIP = segment.fourTuple.destinationIP;
            }
            else if (index == 1 && flags == 0b00000000_00010010) { //SYN-ACK
                //
            }
            else if (index == 2 && flags == 0b00000000_00010000) { //ACK
                timestampAck = segment.timestamp;
            }
            else {
                return;
            }

            if (++index > 2) break;
        }

        if (timestampSyn == 0 || timestampAck == 0) return;

        long delta = timestampAck - timestampSyn;

        tcpStatCount.AddOrUpdate(
            ips,
            new TcpStatCount() { total3wh = 1, totalRtt = delta, minRtt = delta, maxRtt = delta },
            (ip, count) => {
                Interlocked.Increment(ref count.total3wh);
                Interlocked.Add(ref count.totalRtt, delta);
                Interlocked.Exchange(ref count.minRtt, Math.Min(count.minRtt, delta));
                Interlocked.Exchange(ref count.maxRtt, Math.Max(count.maxRtt, delta));
                return count;
            }
        );

    }

    private void AnalyzeSequenceNumbers(ConcurrentQueue<Segment> segments, IPPair ips) {

        foreach (Segment segment in segments) {

        }

        tcpStatCount.AddOrUpdate(
            ips,
            new TcpStatCount() { totalSegments = segments.Count },
            (ip, count) => {
                Interlocked.Add(ref count.totalSegments, segments.Count);
                return count;
            }
        );

    }

}