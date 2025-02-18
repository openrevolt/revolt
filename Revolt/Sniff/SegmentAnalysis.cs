using System;
using System.Collections.Concurrent;

namespace Revolt.Sniff;

public sealed partial class Sniffer {

    public void SegmentAnalysis(in Segment segment, ConcurrentQueue<Segment> stream) {
        IPPair pair = new IPPair(segment.fourTuple.sourceIP, segment.fourTuple.destinationIP);
        
        long segmentSize = segment.size;

        if (stream.Count == 3) {
            long rtt = Analyze3WH(in pair, stream);

            streamsCount.AddOrUpdate(
                pair,

                new StreamCount() {
                    total3wh      = 1,
                    totalRtt      = rtt,
                    minRtt        = rtt,
                    maxRtt        = rtt,
                    totalSegments = 1,
                    totalBytes    = segmentSize
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
        else {
            streamsCount.AddOrUpdate(
                pair,

                new StreamCount() {
                    total3wh = 0,
                    totalSegments = 1,
                    totalBytes = segmentSize
                },

                (ip, count) => {
                    Interlocked.Increment(ref count.total3wh);
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

            if (index == 0 && flags == 0b00000000_00000010) { //SYN
                timestampSyn = segment.timestamp;
            }
            else if (index == 1 && flags != 0b00000000_00010010) { //SYN-ACK
                return -1;
            }
            else if (index == 2 && flags == 0b00000000_00010000) { //ACK
                timestampAck = segment.timestamp;
            }
            else {
                return -1;
            }

            if (++index > 2) break;
        }

        long delta = timestampAck - timestampSyn;
        return delta;
    }

}