using System;
using System.Collections.Concurrent;

using static Revolt.Sniff.Sniffer;

namespace Revolt.Sniff;

public sealed partial class Sniffer {
    private const ushort SYN_MASK    = 0b00000000_00000010;
    private const ushort ACK_MASK    = 0b00000000_00010000;
    private const ushort SYNACK_MASK = 0b00000000_00010010;

    private readonly ConcurrentDictionary<FourTuple, (HashSet<uint>, HashSet<uint>)> sequenceTrackers = new ConcurrentDictionary<FourTuple, (HashSet<uint>, HashSet<uint>)>();

    public void SegmentAnalysis(in Segment segment, ConcurrentQueue<Segment> stream) {
        IPPair pair = new IPPair(segment.fourTuple.sourceIP, segment.fourTuple.destinationIP);
        
        long rtt           = stream.Count == 3 ? Analyze3WH(in pair, stream) : -1;
        long segmentSize   = segment.size;
        uint nextSegmentNo = segment.sequenceNo + segment.size;

        StreamCount count = tcpStatCount.AddOrUpdate(
            pair,

            new StreamCount {
                totalSegments = 1,
                totalBytes    = segmentSize,
                total3wh      = rtt > 0 ? 1 : 0,
                totalRtt      = rtt > 0 ? rtt : 0,
                minRtt        = rtt > 0 ? rtt : 0,
                maxRtt        = rtt > 0 ? rtt : 0,
            },

            (_, count) => {
                Interlocked.Increment(ref count.totalSegments);
                Interlocked.Add(ref count.totalBytes, segmentSize);

                if (rtt > 0) {
                    Interlocked.Increment(ref count.total3wh);
                    Interlocked.Add(ref count.totalRtt, rtt);
                    Interlocked.Exchange(ref count.minRtt, Math.Min(count.minRtt, rtt));
                    Interlocked.Exchange(ref count.maxRtt, Math.Max(count.maxRtt, rtt));
                }
                return count;
            }
        );

        AnalyzeSequenceNo(in segment, stream, count);
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

    private void AnalyzeSequenceNo(in Segment lastSegment, ConcurrentQueue<Segment> stream, StreamCount count) {
        if (!sequenceTrackers.TryGetValue(lastSegment.fourTuple, out (HashSet<uint>, HashSet<uint>) sequenceTracker)) {
            sequenceTracker = (new HashSet<uint>(), new HashSet<uint>());
            sequenceTrackers[lastSegment.fourTuple] = sequenceTracker;
        }

        //bool isPhantomByte = false;

        int sourceHash = unchecked(lastSegment.fourTuple.sourceIP.GetHashCode() + lastSegment.fourTuple.sourcePort);
        int destinationHash = unchecked(lastSegment.fourTuple.destinationIP.GetHashCode() + lastSegment.fourTuple.destinationPort);

        HashSet<uint> tracker = sourceHash < destinationHash ? sequenceTracker.Item1 : sequenceTracker.Item2;

        if (tracker.Contains(lastSegment.sequenceNo)) {
            Interlocked.Increment(ref count.retransmission);
        }
        else {
            tracker.Add(lastSegment.sequenceNo);
        }

        if (sourceHash < destinationHash) {
            if (lastSegment.sequenceNo > count.nextSeqNoA) {
                Interlocked.Increment(ref count.loss);
            }
            count.nextSeqNoA = unchecked(lastSegment.sequenceNo + lastSegment.size);
        }
        else {
            if (lastSegment.sequenceNo > count.nextSeqNoB) {
                Interlocked.Increment(ref count.loss);
            }
            count.nextSeqNoB = unchecked(lastSegment.sequenceNo + lastSegment.size);
        }

    }

}