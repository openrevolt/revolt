using System.Collections.Concurrent;

namespace Revolt;

public static class Tokens {
    public static ConcurrentDictionary<CancellationTokenSource, CancellationToken> dictionary = new ConcurrentDictionary<CancellationTokenSource, CancellationToken>();
}
