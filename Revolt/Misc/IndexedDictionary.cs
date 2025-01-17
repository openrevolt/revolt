using System.Collections.Concurrent;
using System.Collections.Generic;

namespace System.Collections.Generic;

public class IndexedDictionary<TKey, TValue> where TValue : class {
    private readonly ConcurrentDictionary<TKey, TValue> _dictionary = new ConcurrentDictionary<TKey, TValue>();
    private readonly List<TValue> _list = new List<TValue>();
    private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

    public int Count => _dictionary.Count;

    public TValue this[int index] {
        get {
            _lock.EnterReadLock();
            try {
                return _list[index];
            }
            finally {
                _lock.ExitReadLock();
            }
        }
    }

    public bool TryAdd(TKey key, TValue value) {
        if (!_dictionary.TryAdd(key, value)) return false;

        _lock.EnterWriteLock();
        try {
            _list.Add(value);
        }
        finally {
            _lock.ExitWriteLock();
        }

        return true;
    }

    public TValue AddOrUpdate(TKey key, TValue value, Func<TKey, TValue, TValue> updateValueFactory) {
        TValue existingValue;

        if (_dictionary.TryGetValue(key, out existingValue)) {
            var updatedValue = updateValueFactory(key, existingValue);
            _dictionary[key] = updatedValue;
            return updatedValue;
        }
        else {
            if (_dictionary.TryAdd(key, value)) {
                _lock.EnterWriteLock();
                try {
                    _list.Add(value);
                }
                finally {
                    _lock.ExitWriteLock();
                }
            }
            return value;
        }
    }

    public TValue GetByKey(TKey key) {
        if (!_dictionary.TryGetValue(key, out TValue value))
            throw new KeyNotFoundException($"Key '{key}' not found.");

        return value;
    }

    public void Clear() {
        _dictionary.Clear();

        _lock.EnterWriteLock();
        try {
            _list.Clear();
        }
        finally {
            _lock.ExitWriteLock();
        }
    }

    public bool ContainsKey(TKey key)
        => _dictionary.ContainsKey(key);

    public List<TValue> ToList() {
        _lock.EnterReadLock();
        try {
            return new List<TValue>(_list);
        }
        finally {
            _lock.ExitReadLock();
        }
    }
}
