using System;
using System.Collections.Concurrent;

namespace Revolt.Sniff;

public sealed partial class Sniffer {
    
    public void SegmentAnalysis(Segment segment, ConcurrentQueue<Segment> stream) {
        IPPair pair = new IPPair(segment.fourTuple.sourceIP, segment.fourTuple.destinationIP);

        AnalyzeSequenceNumbers(pair, stream);

        if (stream.Count == 3) {
            Analyze3WH(pair, stream);
        }
    }

    private void Analyze3WH(IPPair ips, ConcurrentQueue<Segment> stream) {
        int index = 0;
        long timestampSyn = 0, timestampAck = 0;
        IP sourceIP = default;
        IP destinationIP = default;

        foreach (Segment segment in stream) {
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

        streamsCount.AddOrUpdate(
            ips,
            new StreamCount() { total3wh = 1, totalRtt = delta, minRtt = delta, maxRtt = delta },
            (ip, count) => {
                Interlocked.Increment(ref count.total3wh);
                Interlocked.Add(ref count.totalRtt, delta);
                Interlocked.Exchange(ref count.minRtt, Math.Min(count.minRtt, delta));
                Interlocked.Exchange(ref count.maxRtt, Math.Max(count.maxRtt, delta));
                return count;
            }
        );
    }

    private void AnalyzeSequenceNumbers(IPPair pair, ConcurrentQueue<Segment> stream) {

        foreach (Segment segment in stream) {

        }

        streamsCount.AddOrUpdate(
            pair,
            new StreamCount() { totalSegments = stream.Count },
            (ip, count) => {
                Interlocked.Add(ref count.totalSegments, stream.Count);
                return count;
            }
        );

    }

}