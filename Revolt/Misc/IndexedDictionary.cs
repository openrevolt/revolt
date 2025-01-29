namespace System.Collections.Concurrent;

public class IndexedDictionary<TKey, TValue>
    where TKey : notnull
    where TValue : class {

    private readonly ConcurrentDictionary<TKey, TValue> _dictionary  = new ConcurrentDictionary<TKey, TValue>();
    private readonly ConcurrentDictionary<int, TValue> _indexMapping = new ConcurrentDictionary<int, TValue>();
    private readonly ConcurrentDictionary<int, TKey> _indexToKey     = new ConcurrentDictionary<int, TKey>();
    private int _count;

    public int Count =>
        Volatile.Read(ref _count);

    public TValue this[int index] {
        get {
            return _indexMapping[index];
        }
    }

    public bool TryAdd(TKey key, TValue value) {
        if (!_dictionary.TryAdd(key, value)) return false;

        int newIndex = Interlocked.Increment(ref _count) - 1;
        if (!_indexMapping.TryAdd(newIndex, value) || !_indexToKey.TryAdd(newIndex, key)) {
            _dictionary.TryRemove(key, out _);
            Interlocked.Decrement(ref _count);
            return false;
        }
        return true;
    }

    public TValue AddOrUpdate(TKey key, TValue value, Func<TKey, TValue, TValue> updateValueFactory) {
        if (_dictionary.TryGetValue(key, out TValue existingValue)) {
            return _dictionary[key] = updateValueFactory(key, existingValue);
        }

        TryAdd(key, value);
        return value;
    }

    public TKey GetKeyByIndex(int index) =>
        _indexToKey.TryGetValue(index, out TKey key) ? key : default;

    public bool ContainsKey(TKey key) =>
        _dictionary.ContainsKey(key);

    public void Clear() {
        _dictionary.Clear();
        _indexMapping.Clear();
        _indexToKey.Clear();
        Interlocked.Exchange(ref _count, 0);
    }

}
