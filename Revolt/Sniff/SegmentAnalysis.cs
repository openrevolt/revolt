using System;
using System.Collections.Concurrent;

namespace Revolt.Sniff;

public sealed partial class Sniffer {
    private const ushort SYN_MASK    = 0b00000000_00000010;
    private const ushort SYNACK_MASK = 0b00000000_00010010;
    private const ushort ACK_MASK    = 0b00000000_00010000;

    public void SegmentAnalysis(in Segment segment, ConcurrentQueue<Segment> stream) {
        IPPair pair = new IPPair(segment.fourTuple.sourceIP, segment.fourTuple.destinationIP);
        
        long segmentSize = segment.size;
        long rtt = stream.Count == 3 ? Analyze3WH(in pair, stream) : -1;

        if (rtt < 0) {
            streamsCount.AddOrUpdate(
                pair,

                new StreamCount() {
                    total3wh = 0,
                    totalSegments = 1,
                    totalBytes = segmentSize
                },

                (ip, count) => {
                    Interlocked.Increment(ref count.totalSegments);
                    Interlocked.Add(ref count.totalBytes, segmentSize);
                    return count;
                }
            );
        }
        else {
            streamsCount.AddOrUpdate(
                pair,

                new StreamCount() {
                    total3wh = 1,
                    totalRtt = rtt,
                    minRtt = rtt,
                    maxRtt = rtt,
                    totalSegments = 1,
                    totalBytes = segmentSize
                },

                (ip, count) => {
                    Interlocked.Increment(ref count.total3wh);
                    Interlocked.Add(ref count.totalRtt, rtt);
                    Interlocked.Exchange(ref count.minRtt, Math.Min(count.minRtt, rtt));
                    Interlocked.Exchange(ref count.maxRtt, Math.Max(count.maxRtt, rtt));
                    Interlocked.Increment(ref count.totalSegments);
                    Interlocked.Add(ref count.totalBytes, segmentSize);
                    return count;
                }
            );
        }
    }

    private long Analyze3WH(in IPPair ips, ConcurrentQueue<Segment> stream) {
        int index = 0;
        long timestampSyn = 0, timestampAck = 0;

        foreach (Segment segment in stream) {
            ushort flags = (ushort)(segment.flags & 0x0fff);

            if (index == 0 && (flags & SYN_MASK) == SYN_MASK) {
                timestampSyn = segment.timestamp;
            }
            else if (index == 1 && (flags & SYNACK_MASK) != SYNACK_MASK) {
                return -1;
            }
            else if (index == 2 && (flags & ACK_MASK) == ACK_MASK) {
                timestampAck = segment.timestamp;
            }
            else {
                return -1;
            }

            if (++index > 3) break;
        }

        long delta = timestampAck - timestampSyn;
        return delta;
    }

    private void AnalyzeSequenceNo(in IPPair ips, ConcurrentQueue<Segment> stream) {
        //TODO
    }

}