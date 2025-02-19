using System;
using System.Collections.Concurrent;

using static Revolt.Sniff.Sniffer;

namespace Revolt.Sniff;

public sealed partial class Sniffer {
    private const ushort SYN_MASK    = 0b00000000_00000010;
    private const ushort ACK_MASK    = 0b00000000_00010000;
    private const ushort SYNACK_MASK = 0b00000000_00010010;

    public void SegmentAnalysis(in Segment segment, ConcurrentQueue<Segment> stream) {
        IPPair pair = new IPPair(segment.fourTuple.sourceIP, segment.fourTuple.destinationIP);
        
        long rtt = stream.Count == 3 ? Analyze3WH(in pair, stream) : -1;
        long segmentSize = segment.size;

        StreamCount count = tcpStatCount.AddOrUpdate(
            pair,

            new StreamCount {
                total3wh      = rtt > 0 ? 1 : 0,
                totalRtt      = rtt > 0 ? rtt : 0,
                minRtt        = rtt > 0 ? rtt : 0,
                maxRtt        = rtt > 0 ? rtt : 0,
                totalSegments = 1,
                totalBytes    = segmentSize
            },

            (_, count) => {
                if (rtt > 0) {
                    Interlocked.Increment(ref count.total3wh);
                    Interlocked.Add(ref count.totalRtt, rtt);
                    Interlocked.Exchange(ref count.minRtt, Math.Min(count.minRtt, rtt));
                    Interlocked.Exchange(ref count.maxRtt, Math.Max(count.maxRtt, rtt));
                }
                Interlocked.Increment(ref count.totalSegments);
                Interlocked.Add(ref count.totalBytes, segmentSize);
                return count;
            }
        );

        AnalyzeSequenceNo(segment.fourTuple, stream, count);
    }

    private long Analyze3WH(in IPPair ips, ConcurrentQueue<Segment> stream) {
        long timestampSyn = 0, timestampAck = 0;
        int index = 0;

        foreach (Segment segment in stream) {
            if (index == 0 && (segment.flags & SYN_MASK) == SYN_MASK) {
                timestampSyn = segment.timestamp;
            }
            else if (index == 1 && (segment.flags & SYNACK_MASK) == SYNACK_MASK) {
                
            }
            else if (index == 2 && (segment.flags & ACK_MASK) == ACK_MASK) {
                timestampAck = segment.timestamp;
            }
            else {
                return -1;
            }

            index++;
        }

        return timestampAck - timestampSyn;
    }

    private void AnalyzeSequenceNo(in FourTuple fourTuple, ConcurrentQueue<Segment> stream, StreamCount count) {

    }

}