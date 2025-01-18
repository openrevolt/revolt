namespace System.Collections.Concurrent;

public class IndexedDictionary<TKey, TValue>
    where TKey : notnull
    where TValue : class {

    private readonly ConcurrentDictionary<TKey, TValue> _dictionary = new ConcurrentDictionary<TKey, TValue>();
    private readonly ConcurrentDictionary<long, TValue> _indexMapping = new ConcurrentDictionary<long, TValue>();
    private long _count;

    public int Count =>
        (int)Interlocked.Read(ref _count);

    public TValue this[int index] {
        get {
            return _indexMapping[index];
        }
    }

    public bool TryAdd(TKey key, TValue value) {
        if (!_dictionary.TryAdd(key, value)) return false;
        long newIndex = Interlocked.Increment(ref _count) - 1;
        _indexMapping.TryAdd(newIndex, value);
        return true;
    }

    public TValue AddOrUpdate(TKey key, TValue value, Func<TKey, TValue, TValue> updateValueFactory) {
        if (_dictionary.TryGetValue(key, out TValue existingValue)) {
            TValue updatedValue = updateValueFactory(key, existingValue);
            _dictionary[key] = updatedValue;
            return updatedValue;
        }

        TryAdd(key, value);
        return value;
    }

    public TValue GetByKey(TKey key) {
        if (!_dictionary.TryGetValue(key, out TValue value)) {
            throw new KeyNotFoundException($"Key '{key}' not found.");
        }

        return value;
    }

    public bool ContainsKey(TKey key)
        => _dictionary.ContainsKey(key);

    public void Clear() {
        _dictionary.Clear();
        _indexMapping.Clear();
    }

    public List<TValue> ToList =>
        _indexMapping.Values.ToList();

}
