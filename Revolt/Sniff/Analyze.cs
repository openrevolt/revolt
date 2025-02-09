using System.Collections.Concurrent;

namespace Revolt.Sniff;

public sealed partial class Sniffer {

    public void Analyze() {
        foreach (ConcurrentQueue<Segment> stream in streams.Values) {
            AnalyzeStream(stream);
        }
    }

    private void AnalyzeStream(ConcurrentQueue<Segment> segments) {
        foreach (Segment segment in segments) {

        }
    }

}