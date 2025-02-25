using System;
using System.Collections.Concurrent;

using static Revolt.Sniff.Sniffer;

namespace Revolt.Sniff;

public sealed partial class Sniffer {
    private const ushort FIN_MASK    = 0b00000000_00000001;
    private const ushort SYN_MASK    = 0b00000000_00000010;
    private const ushort RST_MASK    = 0b00000000_00000100;
    private const ushort PUS_MASK    = 0b00000000_00001000;
    private const ushort ACK_MASK    = 0b00000000_00010000;
    private const ushort URG_MASK    = 0b00000000_00100000;
    private const ushort ECH_MASK    = 0b00000000_01000000;
    private const ushort WIN_MASK    = 0b00000000_10000000;
    private const ushort ACC_MASK    = 0b00000001_00000000;
    private const ushort SYNACK_MASK = 0b00000000_00010010;

    private readonly ConcurrentDictionary<FourTuple, (ConcurrentDictionary<uint, byte>, ConcurrentDictionary<uint, byte>)> sequenceTrackers
        = new ConcurrentDictionary<FourTuple, (ConcurrentDictionary<uint, byte>, ConcurrentDictionary<uint, byte>)>();

    private readonly ConcurrentQueue<FourTuple> zeroWindowEvent = new ConcurrentQueue<FourTuple>();

    public long packetLoss = 0;
    public long outOfOrder = 0;
    public long packetRetransmit = 0, bytesRetransmit = 0;

    public void SegmentAnalysis(in Segment segment, ConcurrentQueue<Segment> stream) {
        IPPair pair = new IPPair(segment.fourTuple.sourceIP, segment.fourTuple.destinationIP);
        
        long rtt           = stream.Count == 3 ? Analyze3WH(in pair, stream) : -1;
        long segmentSize   = segment.payloadSize;
        uint nextSegmentNo = segment.sequenceNo + segment.payloadSize;

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
        CheckWindowSize(in segment, count);
        CheckProtocolViolation(in segment, count);
    }

    private long Analyze3WH(in IPPair ips, ConcurrentQueue<Segment> stream) {
        long timestampSyn = 0, timestampAck = 0;
        int index = 0;

        foreach (Segment segment in stream) {
            if (index == 0 && (segment.flags & SYN_MASK) != 0) {
                timestampSyn = segment.timestamp;
            }
            else if (index == 1 && (segment.flags & SYNACK_MASK) == SYNACK_MASK) {
                
            }
            else if (index == 2 && (segment.flags & ACK_MASK) != 0) {
                timestampAck = segment.timestamp;
            }
            else {
                return -1;
            }

            index++;
        }

        return timestampAck - timestampSyn;
    }

    private void AnalyzeSequenceNo(in Segment segment, ConcurrentQueue<Segment> stream, StreamCount count) {
        (ConcurrentDictionary<uint, byte>, ConcurrentDictionary<uint, byte>) sequenceTracker = sequenceTrackers.GetOrAdd(segment.fourTuple, _ => (new ConcurrentDictionary<uint, byte>(), new ConcurrentDictionary<uint, byte>()));

        int sourceHash = unchecked(segment.fourTuple.sourceIP.GetHashCode() + segment.fourTuple.sourcePort);
        int destinationHash = unchecked(segment.fourTuple.destinationIP.GetHashCode() + segment.fourTuple.destinationPort);

        ConcurrentDictionary<uint, byte> tracker = sourceHash < destinationHash ? sequenceTracker.Item1 : sequenceTracker.Item2;

        bool isFIN = (segment.flags & FIN_MASK) == FIN_MASK;
        bool isSYN = (segment.flags & SYN_MASK) == SYN_MASK;
        bool isACK = (segment.flags & ACK_MASK) == ACK_MASK;
        bool hasPhantomByte = isSYN || isFIN;

        uint size = segment.payloadSize;

        if (!isACK && !tracker.TryAdd(segment.sequenceNo, 0)) {
            Interlocked.Increment(ref count.duplicate);
            Interlocked.Increment(ref packetRetransmit);
            Interlocked.Add(ref bytesRetransmit, size);
        }

        if (hasPhantomByte && size == 0) size = 1;

        if (sourceHash < destinationHash) {
            if (segment.sequenceNo > count.nextSeqNoA && count.nextSeqNoA != 0) {
                Interlocked.Increment(ref count.loss);
                Interlocked.Increment(ref packetLoss);
            }
            else if (segment.sequenceNo < count.nextSeqNoA) {
                Interlocked.Increment(ref count.ooo);
                Interlocked.Increment(ref outOfOrder);
            }
            count.nextSeqNoA = unchecked(segment.sequenceNo + size);
        }
        else {
            if (segment.sequenceNo > count.nextSeqNoB && count.nextSeqNoB != 0) {
                Interlocked.Increment(ref count.loss);
                Interlocked.Increment(ref packetLoss);
            }
            else if (segment.sequenceNo < count.nextSeqNoB) {
                Interlocked.Increment(ref count.ooo);
                Interlocked.Increment(ref outOfOrder);
            }
            count.nextSeqNoB = unchecked(segment.sequenceNo + size);
        }
    }

    private void CheckWindowSize(in Segment segment, StreamCount count) {
        if ((segment.flags & RST_MASK) == RST_MASK) return;
        if (segment.window > 0) return;
        if (zeroWindowEvent.Contains(segment.fourTuple)) return;

        while (zeroWindowEvent.Count > 512 && zeroWindowEvent.TryDequeue(out _)) { }

        zeroWindowEvent.Enqueue(segment.fourTuple);

        if (segment.fourTuple.sourceIP.isIPv6) {
            issuesList.Add(new SniffIssuesItem(1, $"Zero window size: [{segment.fourTuple.sourceIP}]:{segment.fourTuple.sourcePort} -> [{segment.fourTuple.destinationIP}]:{segment.fourTuple.destinationPort}"));
        }
        else {
            issuesList.Add(new SniffIssuesItem(1, $"Zero window size: {segment.fourTuple.sourceIP}:{segment.fourTuple.sourcePort} -> {segment.fourTuple.destinationIP}:{segment.fourTuple.destinationPort}"));
        }
    }

    private void CheckProtocolViolation(in Segment segment, StreamCount count) {
        if (segment.payloadSize == 0) return;

        bool isSyn = (segment.flags & SYN_MASK) == SYN_MASK;
        if (isSyn) {
            if (segment.fourTuple.sourceIP.isIPv6) {
                issuesList.Add(new SniffIssuesItem(2, $"Unexpected payload in SYN packet: [{segment.fourTuple.sourceIP}]:{segment.fourTuple.sourcePort} -> [{segment.fourTuple.destinationIP}]:{segment.fourTuple.destinationPort}"));
            }
            else {
                issuesList.Add(new SniffIssuesItem(2, $"Unexpected payload in SYN packet: {segment.fourTuple.sourceIP}:{segment.fourTuple.sourcePort} -> {segment.fourTuple.destinationIP}:{segment.fourTuple.destinationPort}"));
            }
        }
    }

}