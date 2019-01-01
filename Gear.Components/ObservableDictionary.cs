using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Linq;

namespace Gear.Components
{
    public class ObservableDictionary<TKey, TValue> : PropertyChangeNotifier, ICollection, ICollection<KeyValuePair<TKey, TValue>>, IDictionary, IDictionary<TKey, TValue>, IEnumerable, IEnumerable<KeyValuePair<TKey, TValue>>, IObservableRangeDictionary<TKey, TValue>, IReadOnlyCollection<KeyValuePair<TKey, TValue>>, IReadOnlyDictionary<TKey, TValue>
    {
        public ObservableDictionary()
        {
            gd = new Dictionary<TKey, TValue>();
            Cast();
        }

        public ObservableDictionary(IDictionary<TKey, TValue> dictionary)
        {
            gd = new Dictionary<TKey, TValue>(dictionary);
            Cast();
        }

        public ObservableDictionary(IEqualityComparer<TKey> comparer)
        {
            gd = new Dictionary<TKey, TValue>(comparer);
            this.comparer = comparer;
            Cast();
        }

        public ObservableDictionary(int capacity)
        {
            gd = new Dictionary<TKey, TValue>(capacity);
            Cast();
        }

        public ObservableDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
        {
            gd = new Dictionary<TKey, TValue>(dictionary, comparer);
            this.comparer = comparer;
            Cast();
        }

        public ObservableDictionary(int capacity, IEqualityComparer<TKey> comparer)
        {
            gd = new Dictionary<TKey, TValue>(capacity, comparer);
            this.comparer = comparer;
            Cast();
        }

        readonly IEqualityComparer<TKey> comparer;
        Dictionary<TKey, TValue> gd;
        ICollection ci;
        ICollection<KeyValuePair<TKey, TValue>> gci;
        IDictionary di;
        IDictionary<TKey, TValue> gdi;
        IEnumerable ei;
        IEnumerable<KeyValuePair<TKey, TValue>> gei;
        IReadOnlyDictionary<TKey, TValue> grodi;

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event EventHandler<NotifyDictionaryChangedEventArgs<TKey, TValue>> DictionaryChanged;
        event EventHandler<NotifyDictionaryChangedEventArgs<object, object>> DictionaryChangedBoxed;
        public event EventHandler<NotifyGenericCollectionChangedEventArgs<KeyValuePair<TKey, TValue>>> GenericCollectionChanged;

        public virtual void Add(TKey key, TValue value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (!gd.ContainsKey(key))
                NotifyCountChanging();
            gd.Add(key, value);
            OnDictionaryChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Add, key, value));
            NotifyCountChanged();
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => Add(item);

        void IDictionary.Add(object key, object value) => Add(key, value);

        protected virtual void Add(object key, object value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (key is TKey typedKey && !gd.ContainsKey(typedKey))
                NotifyCountChanging();
            di.Add(key, value);
            OnDictionaryChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Add, (TKey)key, (TValue)value));
            NotifyCountChanged();
        }

        protected virtual void Add(KeyValuePair<TKey, TValue> item)
        {
            if (item.Key == null)
                throw new ArgumentNullException("key");
            if (!gd.ContainsKey(item.Key))
                NotifyCountChanging();
            gci.Add(item);
            OnDictionaryChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Add, item));
            NotifyCountChanged();
        }

        public virtual void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs) =>
            AddRange(keyValuePairs.ToImmutableArray());

        public virtual void AddRange(IReadOnlyList<KeyValuePair<TKey, TValue>> keyValuePairs)
        {
            if (keyValuePairs.Any(kvp => kvp.Key == null || gd.ContainsKey(kvp.Key)))
                throw new ArgumentException("One of the keys was null or already found in the dictionary", nameof(keyValuePairs));
            NotifyCountChanging();
            foreach (var keyValuePair in keyValuePairs)
                gd.Add(keyValuePair.Key, keyValuePair.Value);
            OnDictionaryChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Add, keyValuePairs));
            NotifyCountChanged();
        }

        void Cast()
        {
            ci = gd;
            gci = gd;
            di = gd;
            gdi = gd;
            ei = gd;
            gei = gd;
            grodi = gd;
        }

        void CastAndNotifyReset()
        {
            Cast();
            OnDictionaryChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Reset));
        }

        public virtual void Clear()
        {
            if (Count > 0)
            {
                NotifyCountChanging();
                gd.Clear();
                OnDictionaryChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Reset));
                NotifyCountChanged();
            }
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item) => Contains(item);

        bool IDictionary.Contains(object key) => Contains(key);

        protected virtual bool Contains(object key) => di.Contains(key);

        protected virtual bool Contains(KeyValuePair<TKey, TValue> item) => gci.Contains(item);

        public virtual bool ContainsKey(TKey key) => gd.ContainsKey(key);

        public virtual bool ContainsValue(TValue value) => gd.ContainsValue(value);

        void ICollection.CopyTo(Array array, int index) => CopyTo(array, index);

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => CopyTo(array, arrayIndex);

        protected virtual void CopyTo(Array array, int index) => ci.CopyTo(array, index);

        protected virtual void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => gci.CopyTo(array, arrayIndex);

        protected virtual IDictionaryEnumerator GetDictionaryEnumerator() => di.GetEnumerator();

        public virtual Dictionary<TKey, TValue>.Enumerator GetEnumerator() => gd.GetEnumerator();

        IDictionaryEnumerator IDictionary.GetEnumerator() => GetDictionaryEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetNonGenericEnumerator();

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => GetKeyValuePairEnumerator();

        protected virtual IEnumerator<KeyValuePair<TKey, TValue>> GetKeyValuePairEnumerator() => gei.GetEnumerator();

        protected virtual IEnumerator GetNonGenericEnumerator() => ei.GetEnumerator();

        public virtual IReadOnlyList<KeyValuePair<TKey, TValue>> GetRange(IEnumerable<TKey> keys) => keys.Select(key => new KeyValuePair<TKey, TValue>(key, gd[key])).ToImmutableArray();

        protected virtual object GetValue(object key) => di[key];

        protected void NotifyCountChanged() => OnPropertyChanged(nameof(Count));

        protected void NotifyCountChanging() => OnPropertyChanging(nameof(Count));

        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e) => CollectionChanged?.Invoke(this, e);

        protected virtual void OnDictionaryChanged(NotifyDictionaryChangedEventArgs<TKey, TValue> e)
        {
            if (CollectionChanged != null)
                switch (e.Action)
                {
                    case NotifyDictionaryChangedAction.Add:
                        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, e.NewItems));
                        break;
                    case NotifyDictionaryChangedAction.Remove:
                        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, e.OldItems));
                        break;
                    case NotifyDictionaryChangedAction.Replace:
                        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, e.NewItems, e.OldItems));
                        break;
                    case NotifyDictionaryChangedAction.Reset:
                        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                        break;
                    default:
                        throw new NotSupportedException();
                }
            if (GenericCollectionChanged != null)
                switch (e.Action)
                {
                    case NotifyDictionaryChangedAction.Add:
                        OnGenericCollectionChanged(new NotifyGenericCollectionChangedEventArgs<KeyValuePair<TKey, TValue>>(NotifyCollectionChangedAction.Add, e.NewItems));
                        break;
                    case NotifyDictionaryChangedAction.Remove:
                        OnGenericCollectionChanged(new NotifyGenericCollectionChangedEventArgs<KeyValuePair<TKey, TValue>>(NotifyCollectionChangedAction.Remove, e.OldItems));
                        break;
                    case NotifyDictionaryChangedAction.Replace:
                        OnGenericCollectionChanged(new NotifyGenericCollectionChangedEventArgs<KeyValuePair<TKey, TValue>>(NotifyCollectionChangedAction.Replace, e.NewItems, e.OldItems));
                        break;
                    case NotifyDictionaryChangedAction.Reset:
                        OnGenericCollectionChanged(new NotifyGenericCollectionChangedEventArgs<KeyValuePair<TKey, TValue>>(NotifyCollectionChangedAction.Reset));
                        break;
                    default:
                        throw new NotSupportedException();
                }
            if (DictionaryChangedBoxed != null)
                switch (e.Action)
                {
                    case NotifyDictionaryChangedAction.Add:
                        OnDictionaryChangedBoxed(new NotifyDictionaryChangedEventArgs<object, object>(NotifyDictionaryChangedAction.Add, e.NewItems.Select(kv => new KeyValuePair<object, object>(kv.Key, kv.Value))));
                        break;
                    case NotifyDictionaryChangedAction.Remove:
                        OnDictionaryChangedBoxed(new NotifyDictionaryChangedEventArgs<object, object>(NotifyDictionaryChangedAction.Remove, e.OldItems.Select(kv => new KeyValuePair<object, object>(kv.Key, kv.Value))));
                        break;
                    case NotifyDictionaryChangedAction.Replace:
                        OnDictionaryChangedBoxed(new NotifyDictionaryChangedEventArgs<object, object>(NotifyDictionaryChangedAction.Replace, e.NewItems.Select(kv => new KeyValuePair<object, object>(kv.Key, kv.Value)), e.OldItems.Select(kv => new KeyValuePair<object, object>(kv.Key, kv.Value))));
                        break;
                    case NotifyDictionaryChangedAction.Reset:
                        OnDictionaryChangedBoxed(new NotifyDictionaryChangedEventArgs<object, object>(NotifyDictionaryChangedAction.Reset));
                        break;
                }
            DictionaryChanged?.Invoke(this, e);
        }

        protected virtual void OnDictionaryChangedBoxed(NotifyDictionaryChangedEventArgs<object, object> e) => DictionaryChangedBoxed?.Invoke(this, e);

        protected virtual void OnGenericCollectionChanged(NotifyGenericCollectionChangedEventArgs<KeyValuePair<TKey, TValue>> e) => GenericCollectionChanged?.Invoke(this, e);

        public virtual bool Remove(TKey key)
        {
            if (gd.TryGetValue(key, out var value))
            {
                NotifyCountChanging();
                gd.Remove(key);
                OnDictionaryChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Remove, key, value));
                NotifyCountChanged();
                return true;
            }
            return false;
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) => Remove(item);

        void IDictionary.Remove(object key) => Remove(key);

        protected virtual void Remove(object key)
        {
            if (di.Contains(key))
            {
                var value = di[key];
                NotifyCountChanging();
                Remove(key);
                OnDictionaryChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Remove, (TKey)key, (TValue)value));
                NotifyCountChanged();
            }
        }

        protected virtual bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (gci.Contains(item))
            {
                NotifyCountChanging();
                gci.Remove(item);
                OnDictionaryChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Remove, item));
                NotifyCountChanged();
                return true;
            }
            return false;
        }

        public virtual IReadOnlyList<KeyValuePair<TKey, TValue>> RemoveAll(Func<TKey, TValue, bool> predicate)
        {
            var removed = new List<KeyValuePair<TKey, TValue>>();
            foreach (var kv in gd.ToList())
                if (predicate(kv.Key, kv.Value))
                {
                    gd.Remove(kv.Key);
                    removed.Add(kv);
                }
            OnDictionaryChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Remove, removed));
            return removed.ToImmutableArray();
        }

        public virtual IReadOnlyList<TKey> RemoveRange(IEnumerable<TKey> keys)
        {
            var removingKeyValuePairs = new List<KeyValuePair<TKey, TValue>>();
            foreach (var key in keys)
                if (gd.TryGetValue(key, out var value))
                    removingKeyValuePairs.Add(new KeyValuePair<TKey, TValue>(key, value));
            var removedKeys = new List<TKey>();
            if (removingKeyValuePairs.Any())
            {
                NotifyCountChanging();
                foreach (var removingKeyValuePair in removingKeyValuePairs)
                {
                    gd.Remove(removingKeyValuePair.Key);
                    removedKeys.Add(removingKeyValuePair.Key);
                }
                OnDictionaryChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Remove, removingKeyValuePairs));
                NotifyCountChanged();
            }
            return removedKeys.ToImmutableArray();
        }

        public virtual void ReplaceRange(IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs)
        {
            if (keyValuePairs.Any(kvp => !gd.ContainsKey(kvp.Key)))
                throw new ArgumentException("One of the keys was not found in the dictionary", nameof(keyValuePairs));
            var oldItems = GetRange(keyValuePairs.Select(kv => kv.Key));
            foreach (var keyValuePair in keyValuePairs)
                gd[keyValuePair.Key] = keyValuePair.Value;
            OnDictionaryChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Replace, keyValuePairs, oldItems));
        }

        public virtual void Reset()
        {
            if (comparer == null)
                gd = new Dictionary<TKey, TValue>();
            else
                gd = new Dictionary<TKey, TValue>(comparer);
            CastAndNotifyReset();
        }

        public virtual void Reset(IDictionary<TKey, TValue> dictionary)
        {
            if (comparer == null)
                gd = new Dictionary<TKey, TValue>(dictionary);
            else
                gd = new Dictionary<TKey, TValue>(dictionary, comparer);
            CastAndNotifyReset();
        }

        protected virtual void SetValue(object key, object value)
        {
            var oldValue = GetValue(key);
            di[key] = value;
            OnDictionaryChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Replace, (TKey)key, (TValue)value, (TValue)oldValue));
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            bool valueRetrieved;
            (valueRetrieved, value) = TryGetValue(key);
            return valueRetrieved;
        }

        protected virtual (bool valueRetrieved, TValue value) TryGetValue(TKey key)
        {
            var valueRetrieved = gd.TryGetValue(key, out var value);
            return (valueRetrieved, value);
        }

        event EventHandler<NotifyDictionaryChangedEventArgs<object, object>> INotifyDictionaryChanged.DictionaryChanged
        {
            add => DictionaryChangedBoxed += value;
            remove => DictionaryChangedBoxed -= value;
        }

        public virtual TValue this[TKey key]
        {
            get => gd[key];
            set
            {
                var oldValue = gd[key];
                gd[key] = value;
                OnDictionaryChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Replace, key, value, oldValue));
            }
        }

        object IDictionary.this[object key]
        {
            get => GetValue(key);
            set => SetValue(key, value);
        }

        public virtual IEqualityComparer<TKey> Comparer => gd.Comparer;

        public virtual int Count => gd.Count;

        protected virtual bool DictionaryIsReadOnly => di.IsReadOnly;

        protected virtual bool GenericCollectionIsReadOnly => gci.IsReadOnly;

        public virtual Dictionary<TKey, TValue>.KeyCollection Keys => gd.Keys;

        ICollection IDictionary.Keys => KeysCollection;

        ICollection<TKey> IDictionary<TKey, TValue>.Keys => KeysGenericCollection;

        IEnumerable<TKey> IRangeDictionary<TKey, TValue>.Keys => KeysGenericEnumerable;

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => KeysGenericEnumerable;

        protected virtual ICollection KeysCollection => di.Keys;

        protected virtual ICollection<TKey> KeysGenericCollection => gdi.Keys;

        protected virtual IEnumerable<TKey> KeysGenericEnumerable => grodi.Keys;

        bool IDictionary.IsFixedSize => IsFixedSize;

        protected virtual bool IsFixedSize => di.IsFixedSize;

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => GenericCollectionIsReadOnly;

        bool IDictionary.IsReadOnly => DictionaryIsReadOnly;

        bool ICollection.IsSynchronized => IsCollectionSynchronized;

        protected virtual bool IsCollectionSynchronized => ci.IsSynchronized;

        object ICollection.SyncRoot => SyncRoot;

        protected virtual object SyncRoot => ci.SyncRoot;

        public virtual Dictionary<TKey, TValue>.ValueCollection Values => gd.Values;

        ICollection IDictionary.Values => ValuesCollection;

        ICollection<TValue> IDictionary<TKey, TValue>.Values => ValuesGenericCollection;

        IEnumerable<TValue> IRangeDictionary<TKey, TValue>.Values => ValuesGenericEnumerable;

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => ValuesGenericEnumerable;

        protected virtual ICollection ValuesCollection => di.Values;

        protected virtual ICollection<TValue> ValuesGenericCollection => gdi.Values;

        protected virtual IEnumerable<TValue> ValuesGenericEnumerable => grodi.Values;
    }
}
