using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Gear.Components
{
    public class ObservableSortedDictionary<TKey, TValue> : PropertyChangeNotifier, ICollection, ICollection<KeyValuePair<TKey, TValue>>, IDictionary, IDictionary<TKey, TValue>, IEnumerable, IEnumerable<KeyValuePair<TKey, TValue>>, INotifyDictionaryChanged<TKey, TValue>, IRangeDictionary<TKey, TValue>, IReadOnlyCollection<KeyValuePair<TKey, TValue>>, IReadOnlyDictionary<TKey, TValue>
    {
        public ObservableSortedDictionary()
        {
            gsd = new SortedDictionary<TKey, TValue>();
            ci = gsd;
            gci = gsd;
            di = gsd;
            gdi = gsd;
            ei = gsd;
            gei = gsd;
            grodi = gsd;
        }

        public ObservableSortedDictionary(IComparer<TKey> comparer)
        {
            gsd = new SortedDictionary<TKey, TValue>(comparer);
            ci = gsd;
            gci = gsd;
            di = gsd;
            gdi = gsd;
            ei = gsd;
            gei = gsd;
            grodi = gsd;
        }

        public ObservableSortedDictionary(IDictionary<TKey, TValue> dictionary)
        {
            gsd = new SortedDictionary<TKey, TValue>(dictionary);
            ci = gsd;
            gci = gsd;
            di = gsd;
            gdi = gsd;
            ei = gsd;
            gei = gsd;
            grodi = gsd;
        }

        public ObservableSortedDictionary(IDictionary<TKey, TValue> dictionary, IComparer<TKey> comparer)
        {
            gsd = new SortedDictionary<TKey, TValue>(dictionary, comparer);
            ci = gsd;
            gci = gsd;
            di = gsd;
            gdi = gsd;
            ei = gsd;
            gei = gsd;
            grodi = gsd;
        }

        readonly SortedDictionary<TKey, TValue> gsd;
        readonly ICollection ci;
        readonly ICollection<KeyValuePair<TKey, TValue>> gci;
        readonly IDictionary di;
        readonly IDictionary<TKey, TValue> gdi;
        readonly IEnumerable ei;
        readonly IEnumerable<KeyValuePair<TKey, TValue>> gei;
        readonly IReadOnlyDictionary<TKey, TValue> grodi;

        public event EventHandler<NotifyDictionaryValueEventArgs<TKey, TValue>> ValueAdded;
        public event EventHandler<NotifyDictionaryValueEventArgs<TKey, TValue>> ValueRemoved;
        public event EventHandler<NotifyDictionaryValueReplacedEventArgs<TKey, TValue>> ValueReplaced;
        public event EventHandler<NotifyDictionaryValuesEventArgs<TKey, TValue>> ValuesAdded;
        public event EventHandler<NotifyDictionaryValuesEventArgs<TKey, TValue>> ValuesRemoved;

        public virtual void Add(TKey key, TValue value)
        {
            if (gsd.ContainsKey(key))
                NotifyCountChanging();
            gsd.Add(key, value);
            OnValueAdded(key, value);
            NotifyCountChanged();
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => Add(item);

        void IDictionary.Add(object key, object value) => Add(key, value);

        protected virtual void Add(object key, object value)
        {
            if (key is TKey typedKey && gsd.ContainsKey(typedKey))
                NotifyCountChanging();
            di.Add(key, value);
            OnValueAdded((TKey)key, (TValue)value);
            NotifyCountChanged();
        }

        protected virtual void Add(KeyValuePair<TKey, TValue> item)
        {
            if (gsd.ContainsKey(item.Key))
                NotifyCountChanging();
            gci.Add(item);
            OnValueAdded(item.Key, item.Value);
            NotifyCountChanged();
        }

        public virtual void AddRange(IReadOnlyList<KeyValuePair<TKey, TValue>> keyValuePairs)
        {
            if (keyValuePairs.Any(kvp => gsd.ContainsKey(kvp.Key)))
                throw new ArgumentException("One of the keys was already found in the dictionary", nameof(keyValuePairs));
            NotifyCountChanging();
            foreach (var keyValuePair in keyValuePairs)
                gsd.Add(keyValuePair.Key, keyValuePair.Value);
            OnValuesAdded(new NotifyDictionaryValuesEventArgs<TKey, TValue>(keyValuePairs));
            NotifyCountChanged();
        }

        public virtual void Clear()
        {
            var removed = gsd.ToList();
            if (removed.Any())
                NotifyCountChanging();
            gsd.Clear();
            OnValuesRemoved(removed);
            NotifyCountChanged();
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item) => Contains(item);

        bool IDictionary.Contains(object key) => Contains(key);

        protected virtual bool Contains(object key) => di.Contains(key);

        protected virtual bool Contains(KeyValuePair<TKey, TValue> item) => gci.Contains(item);

        public virtual bool ContainsKey(TKey key) => gsd.ContainsKey(key);

        public virtual bool ContainsValue(TValue value) => gsd.ContainsValue(value);

        public virtual void CopyTo(KeyValuePair<TKey, TValue>[] array, int index) => gsd.CopyTo(array, index);

        void ICollection.CopyTo(Array array, int index) => CopyTo(array, index);

        protected virtual void CopyTo(Array array, int index) => ci.CopyTo(array, index);

        protected virtual IDictionaryEnumerator GetDictionaryEnumerator() => di.GetEnumerator();

        public virtual SortedDictionary<TKey, TValue>.Enumerator GetEnumerator() => gsd.GetEnumerator();

        IDictionaryEnumerator IDictionary.GetEnumerator() => GetDictionaryEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetNonGenericEnumerator();

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => GetKeyValuePairEnumerator();

        protected virtual IEnumerator<KeyValuePair<TKey, TValue>> GetKeyValuePairEnumerator() => gei.GetEnumerator();

        protected virtual IEnumerator GetNonGenericEnumerator() => ei.GetEnumerator();

        protected virtual object GetValue(object key) => di[key];

        protected void NotifyCountChanged() => OnPropertyChanged(nameof(Count));

        protected void NotifyCountChanging() => OnPropertyChanging(nameof(Count));

        protected virtual void OnValueAdded(NotifyDictionaryValueEventArgs<TKey, TValue> e) => ValueAdded?.Invoke(this, e);

        protected void OnValueAdded(TKey key, TValue value) => OnValueAdded(new NotifyDictionaryValueEventArgs<TKey, TValue>(key, value));

        protected virtual void OnValueRemoved(NotifyDictionaryValueEventArgs<TKey, TValue> e) => ValueRemoved?.Invoke(this, e);

        protected void OnValueRemoved(TKey key, TValue value) => OnValueRemoved(new NotifyDictionaryValueEventArgs<TKey, TValue>(key, value));

        protected virtual void OnValueReplaced(NotifyDictionaryValueReplacedEventArgs<TKey, TValue> e) => ValueReplaced?.Invoke(this, e);

        protected void OnValueReplaced(TKey key, TValue oldValue, TValue newValue) => OnValueReplaced(new NotifyDictionaryValueReplacedEventArgs<TKey, TValue>(key, oldValue, newValue));

        protected virtual void OnValuesAdded(NotifyDictionaryValuesEventArgs<TKey, TValue> e) => ValuesAdded?.Invoke(this, e);

        protected virtual void OnValuesRemoved(NotifyDictionaryValuesEventArgs<TKey, TValue> e) => ValuesRemoved?.Invoke(this, e);

        protected void OnValuesRemoved(IReadOnlyList<KeyValuePair<TKey, TValue>> keyValuePairs) => OnValuesRemoved(new NotifyDictionaryValuesEventArgs<TKey, TValue>(keyValuePairs));

        public virtual bool Remove(TKey key)
        {
            if (gsd.TryGetValue(key, out var value))
            {
                NotifyCountChanging();
                gsd.Remove(key);
                OnValueRemoved(key, value);
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
                OnValueRemoved((TKey)key, (TValue)value);
                NotifyCountChanged();
            }
        }

        protected virtual bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (gci.Contains(item))
            {
                NotifyCountChanging();
                gci.Remove(item);
                OnValueRemoved(item.Key, item.Value);
                NotifyCountChanged();
                return true;
            }
            return false;
        }

        public virtual IReadOnlyList<TKey> RemoveRange(IReadOnlyList<TKey> keys)
        {
            var removingKeyValuePairs = new List<KeyValuePair<TKey, TValue>>();
            foreach (var key in keys)
                if (gsd.TryGetValue(key, out var value))
                    removingKeyValuePairs.Add(new KeyValuePair<TKey, TValue>(key, value));
            var removedKeys = new List<TKey>();
            if (removingKeyValuePairs.Any())
            {
                NotifyCountChanging();
                foreach (var removingKeyValuePair in removingKeyValuePairs)
                {
                    gsd.Remove(removingKeyValuePair.Key);
                    removedKeys.Add(removingKeyValuePair.Key);
                }
                OnValuesRemoved(new NotifyDictionaryValuesEventArgs<TKey, TValue>(removingKeyValuePairs));
                NotifyCountChanged();
            }
            return removedKeys;
        }

        protected virtual void SetValue(object key, object value)
        {
            var oldValue = GetValue(key);
            di[key] = value;
            OnValueReplaced((TKey)key, (TValue)oldValue, (TValue)value);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            bool valueRetrieved;
            (valueRetrieved, value) = TryGetValue(key);
            return valueRetrieved;
        }

        protected virtual (bool valueRetrieved, TValue value) TryGetValue(TKey key)
        {
            var valueRetrieved = gsd.TryGetValue(key, out var value);
            return (valueRetrieved, value);
        }

        public virtual TValue this[TKey key]
        {
            get => gsd[key];
            set => gsd[key] = value;
        }

        object IDictionary.this[object key]
        {
            get => GetValue(key);
            set => SetValue(key, value);
        }

        public virtual IComparer<TKey> Comparer => gsd.Comparer;

        public virtual int Count => gsd.Count;

        protected virtual bool DictionaryIsReadOnly => di.IsReadOnly;

        protected virtual bool GenericCollectionIsReadOnly => gci.IsReadOnly;

        public virtual SortedDictionary<TKey, TValue>.KeyCollection Keys => gsd.Keys;

        ICollection IDictionary.Keys => KeysCollection;

        ICollection<TKey> IDictionary<TKey, TValue>.Keys => KeysGenericCollection;

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

        public virtual SortedDictionary<TKey, TValue>.ValueCollection Values => gsd.Values;

        ICollection IDictionary.Values => ValuesCollection;

        ICollection<TValue> IDictionary<TKey, TValue>.Values => ValuesGenericCollection;

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => ValuesGenericEnumerable;

        protected virtual ICollection ValuesCollection => di.Values;

        protected virtual ICollection<TValue> ValuesGenericCollection => gdi.Values;

        protected virtual IEnumerable<TValue> ValuesGenericEnumerable => grodi.Values;
    }
}
