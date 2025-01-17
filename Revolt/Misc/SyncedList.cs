namespace System.Collections.Generic;

public sealed class SyncedList<T> : IList<T> where T : notnull {
    private readonly List<T> _list = new();

    private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

    public int Count {
        get {
            _lock.EnterReadLock();
            try {
                return _list.Count;
            }
            finally {
                _lock.ExitReadLock();
            }
        }
    }

    public bool IsReadOnly => false;

    public T this[int index] {
        get {
            _lock.EnterReadLock();
            try {
                return _list[index];
            }
            finally {
                _lock.ExitReadLock();
            }
        }
        set {
            _lock.EnterWriteLock();
            try {
                _list[index] = value;
            }
            finally {
                _lock.ExitWriteLock();
            }
        }
    }

    public void Add(T item) {
        _lock.EnterWriteLock();
        try {
            _list.Add(item);
        }
        finally {
            _lock.ExitWriteLock();
        }
    }

    public bool Remove(T item) {
        _lock.EnterWriteLock();
        try {
            return _list.Remove(item);
        }
        finally {
            _lock.ExitWriteLock();
        }
    }

    public void RemoveAt(int index) {
        _lock.EnterWriteLock();
        try {
            _list.RemoveAt(index);
        }
        finally {
            _lock.ExitWriteLock();
        }
    }

    public void Clear() {
        _lock.EnterWriteLock();
        try {
            _list.Clear();
        }
        finally {
            _lock.ExitWriteLock();
        }
    }

    public bool Contains(T item) {
        _lock.EnterReadLock();
        try {
            return _list.Contains(item);
        }
        finally {
            _lock.ExitReadLock();
        }
    }

    public void CopyTo(T[] array, int arrayIndex) {
        _lock.EnterReadLock();
        try {
            _list.CopyTo(array, arrayIndex);
        }
        finally {
            _lock.ExitReadLock();
        }
    }

    public T Find(Predicate<T> match) {
        _lock.EnterReadLock();
        try {
            return _list.Find(match);
        }
        finally {
            _lock.ExitReadLock();
        }
    }

    public int FindIndex(Predicate<T> match) {
        _lock.EnterReadLock();
        try {
            return _list.FindIndex(match);
        }
        finally {
            _lock.ExitReadLock();
        }
    }

    public int IndexOf(T item) {
        _lock.EnterReadLock();
        try {
            return _list.IndexOf(item);
        }
        finally {
            _lock.ExitReadLock();
        }
    }

    public void Insert(int index, T item) {
        _lock.EnterWriteLock();
        try {
            _list.Insert(index, item);
        }
        finally {
            _lock.ExitWriteLock();
        }
    }

    public int RemoveAll(Predicate<T> match) {
        _lock.EnterWriteLock();
        try {
            return _list.RemoveAll(match);
        }
        finally {
            _lock.ExitWriteLock();
        }
    }

    public IEnumerator<T> GetEnumerator() {
        List<T> snapshot;
        _lock.EnterReadLock();
        try {
            snapshot = new List<T>(_list);
        }
        finally {
            _lock.ExitReadLock();
        }
        return snapshot.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    public List<T> ToList() {
        _lock.EnterReadLock();
        try {
            return new List<T>(_list);
        }
        finally {
            _lock.ExitReadLock();
        }
    }

}
