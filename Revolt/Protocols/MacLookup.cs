using System.Collections.Frozen;

namespace Revolt.Protocols;

public static class MacLookup {

#if DEBUG
    public static readonly FrozenDictionary<byte, (byte, byte, string)> dictionary = new Dictionary<byte, (byte, byte, string)>(){}.ToFrozenDictionary();
#endif

}
