namespace System.Collections.Generic;

public sealed class SynchronizedList<T> : IList<T> where T : notnull {
    private readonly List<T> _list = [];
    private readonly Lock _mutex = new Lock();

    public int Count {
        get {
            lock (_mutex) {
                return _list.Count;
            }
        }
    }

    public bool IsReadOnly => false;

    public T this[int index] {
        get {
            lock (_mutex) {
                return _list[index];
            }
        }
        set {
            lock (_mutex) {
                _list[index] = value;
            }
        }
    }

    public void Add(T item) {
        lock (_mutex) {
            _list.Add(item);
        }
    }

    public bool Remove(T item) {
        lock (_mutex) {
            return _list.Remove(item);
        }
    }

    public void RemoveAt(int index) {
        lock (_mutex) {
            _list.RemoveAt(index);
        }
    }

    public void Clear() {
        lock (_mutex) {
            _list.Clear();
        }
    }

    public bool Contains(T item) {
        lock (_mutex) {
            return _list.Contains(item);
        }
    }

    public void CopyTo(T[] array, int arrayIndex) {
        lock (_mutex) {
            _list.CopyTo(array, arrayIndex);
        }
    }

    public T Find(Predicate<T> match) {
        lock (_mutex) {
            return _list.Find(match);
        }
    }

    public int FindIndex(Predicate<T> match) {
        lock (_mutex) {
            return _list.FindIndex(match);
        }
    }

    public int IndexOf(T item) {
        lock (_mutex) {
            return _list.IndexOf(item);
        }
    }

    public void Insert(int index, T item) {
        lock (_mutex) {
            _list.Insert(index, item);
        }
    }

    public int RemoveAll(Predicate<T> match) {
        lock (_mutex) {
            return _list.RemoveAll(match);
        }
    }

    public IEnumerator<T> GetEnumerator() {
        List<T> snapshot;
        lock (_mutex) {
            snapshot = new List<T>(_list);
        }
        return snapshot.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    public List<T> ToList() {
        lock (_mutex) {
            return new List<T>(_list);
        }
    }
}

