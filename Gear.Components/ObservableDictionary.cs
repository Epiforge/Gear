using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Gear.Components
{
    public class ObservableDictionary<TKey, TValue> : PropertyChangeNotifier, ICollection, ICollection<KeyValuePair<TKey, TValue>>, IDictionary, IDictionary<TKey, TValue>, IEnumerable, IEnumerable<KeyValuePair<TKey, TValue>>, INotifyDictionaryChanged<TKey, TValue>, IReadOnlyCollection<KeyValuePair<TKey, TValue>>, IReadOnlyDictionary<TKey, TValue>
    {
        public ObservableDictionary()
        {
            dictionary = new Dictionary<TKey, TValue>();
            collectionInterface = dictionary;
            dictionaryInterface = dictionary;
            enumerableInterface = dictionary;
            genericCollectionInterface = dictionary;
            genericEnumerableInterface = dictionary;
        }

        public ObservableDictionary(IDictionary<TKey, TValue> dictionary)
        {
            this.dictionary = new Dictionary<TKey, TValue>(dictionary);
            collectionInterface = this.dictionary;
            dictionaryInterface = this.dictionary;
            enumerableInterface = this.dictionary;
            genericCollectionInterface = this.dictionary;
            genericEnumerableInterface = this.dictionary;
        }

        public ObservableDictionary(IEqualityComparer<TKey> comparer)
        {
            dictionary = new Dictionary<TKey, TValue>(comparer);
            collectionInterface = dictionary;
            dictionaryInterface = dictionary;
            enumerableInterface = dictionary;
            genericCollectionInterface = dictionary;
            genericEnumerableInterface = dictionary;
        }

        public ObservableDictionary(int capacity)
        {
            dictionary = new Dictionary<TKey, TValue>(capacity);
            collectionInterface = dictionary;
            dictionaryInterface = dictionary;
            enumerableInterface = dictionary;
            genericCollectionInterface = dictionary;
            genericEnumerableInterface = dictionary;
        }

        public ObservableDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
        {
            this.dictionary = new Dictionary<TKey, TValue>(dictionary, comparer);
            collectionInterface = this.dictionary;
            dictionaryInterface = this.dictionary;
            enumerableInterface = this.dictionary;
            genericCollectionInterface = this.dictionary;
            genericEnumerableInterface = this.dictionary;
        }

        public ObservableDictionary(int capacity, IEqualityComparer<TKey> comparer)
        {
            dictionary = new Dictionary<TKey, TValue>(capacity, comparer);
            collectionInterface = dictionary;
            dictionaryInterface = dictionary;
            enumerableInterface = dictionary;
            genericCollectionInterface = dictionary;
            genericEnumerableInterface = dictionary;
        }

        readonly Dictionary<TKey, TValue> dictionary;
        readonly ICollection collectionInterface;
        readonly IDictionary dictionaryInterface;
        readonly IEnumerable enumerableInterface;
        readonly ICollection<KeyValuePair<TKey, TValue>> genericCollectionInterface;
        readonly IEnumerable<KeyValuePair<TKey, TValue>> genericEnumerableInterface;

        public event EventHandler<NotifyDictionaryChangedEventArgs<TKey, TValue>> DictionaryChanged;

        public virtual void Add(TKey key, TValue value)
        {
            if (!dictionary.ContainsKey(key))
                OnKeysChanging();
            dictionary.Add(key, value);
            OnKeyValuePairsAdded(new List<KeyValuePair<TKey, TValue>> { new KeyValuePair<TKey, TValue>(key, value) });
            OnKeysChanged();
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            if (!dictionary.ContainsKey(item.Key))
                OnKeysChanging();
            genericCollectionInterface.Add(item);
            OnKeyValuePairsAdded(new List<KeyValuePair<TKey, TValue>> { item });
            OnKeysChanged();
        }

        void IDictionary.Add(object key, object value)
        {
            if (!dictionaryInterface.Contains(key) && value is TValue)
                OnKeysChanging();
            dictionaryInterface.Add(key, value);
            OnKeyValuePairsAdded(new List<KeyValuePair<TKey, TValue>> { new KeyValuePair<TKey, TValue>((TKey)key, (TValue)value) });
            OnKeysChanged();
        }

        public virtual void Clear()
        {
            if (dictionary.Count > 0)
            {
                OnKeysChanging();
                var keyValuePairs = dictionary.ToList();
                dictionary.Clear();
                OnKeyValuePairsRemoved(keyValuePairs);
                OnKeysChanged();
            }
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item) => genericCollectionInterface.Contains(item);

        bool IDictionary.Contains(object key) => dictionaryInterface.Contains(key);

        public virtual bool ContainsKey(TKey key) => dictionary.ContainsKey(key);

        public virtual bool ContainsValue(TValue value) => dictionary.ContainsValue(value);

        void ICollection.CopyTo(Array array, int index) => collectionInterface.CopyTo(array, index);

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => genericCollectionInterface.CopyTo(array, arrayIndex);

        public Dictionary<TKey, TValue>.Enumerator GetEnumerator() => dictionary.GetEnumerator();

        IDictionaryEnumerator IDictionary.GetEnumerator() => dictionaryInterface.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => enumerableInterface.GetEnumerator();

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => genericEnumerableInterface.GetEnumerator();

        protected virtual void OnDictionaryChanged(NotifyDictionaryChangedEventArgs<TKey, TValue> e) => DictionaryChanged?.Invoke(this, e);

        void OnKeysChanged()
        {
            OnPropertyChanged(nameof(Count));
            OnPropertyChanged(nameof(Keys));
            OnPropertyChanged(nameof(Values));
        }

        void OnKeysChanging()
        {
            OnPropertyChanging(nameof(Count));
            OnPropertyChanging(nameof(Keys));
            OnPropertyChanging(nameof(Values));
        }

        protected void OnKeyValuePairsAdded(IReadOnlyList<KeyValuePair<TKey, TValue>> added) => OnDictionaryChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(added));

        protected void OnKeyValuePairsAddedAndRemoved(IReadOnlyList<KeyValuePair<TKey, TValue>> added, IReadOnlyList<KeyValuePair<TKey, TValue>> removed) => OnDictionaryChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(added, removed));

        protected void OnKeyValuePairsRemoved(IReadOnlyList<KeyValuePair<TKey, TValue>> removed) => OnDictionaryChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(removed: removed));

        void OnValuesChanged() => OnPropertyChanged(nameof(Values));

        void OnValuesChanging() => OnPropertyChanging(nameof(Values));

        public bool Remove(TKey key)
        {
            if (dictionary.ContainsKey(key))
            {
                OnKeysChanging();
                dictionary.Remove(key);
                OnKeysChanged();
                return true;
            }
            return false;
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            if (genericCollectionInterface.Contains(item))
            {
                OnKeysChanging();
                genericCollectionInterface.Remove(item);
                OnKeysChanged();
                return true;
            }
            return false;
        }

        void IDictionary.Remove(object key)
        {
            if (dictionaryInterface.Contains(key))
            {
                OnKeysChanging();
                dictionaryInterface.Remove(key);
                OnKeysChanged();
            }
        }

        public virtual bool TryGetValue(TKey key, out TValue value) => dictionary.TryGetValue(key, out value);

        public virtual TValue this[TKey key]
        {
            get => dictionary[key];
            set
            {
                var oldValue = dictionary[key];
                OnValuesChanging();
                dictionary[key] = value;
                OnKeyValuePairsAddedAndRemoved(new List<KeyValuePair<TKey, TValue>> { new KeyValuePair<TKey, TValue>(key, value) }, new List<KeyValuePair<TKey, TValue>> { new KeyValuePair<TKey, TValue>(key, oldValue) });
                OnValuesChanged();
            }
        }

        object IDictionary.this[object key]
        {
            get => dictionaryInterface[key];
            set
            {
                var oldValue = dictionaryInterface[key];
                OnValuesChanging();
                dictionaryInterface[key] = value;
                var typedKey = (TKey)key;
                OnKeyValuePairsAddedAndRemoved(new List<KeyValuePair<TKey, TValue>> { new KeyValuePair<TKey, TValue>(typedKey, (TValue)value) }, new List<KeyValuePair<TKey, TValue>> { new KeyValuePair<TKey, TValue>(typedKey, (TValue)oldValue) });
                OnValuesChanged();
            }
        }

        public IEqualityComparer<TKey> Comparer => dictionary.Comparer;

        public virtual int Count => dictionary.Count;

        bool IDictionary.IsFixedSize => dictionaryInterface.IsFixedSize;

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => genericCollectionInterface.IsReadOnly;

        bool IDictionary.IsReadOnly => dictionaryInterface.IsReadOnly;

        bool ICollection.IsSynchronized => collectionInterface.IsSynchronized;

        public virtual Dictionary<TKey, TValue>.KeyCollection Keys => dictionary.Keys;

        ICollection<TKey> IDictionary<TKey, TValue>.Keys => dictionary.Keys;

        ICollection IDictionary.Keys => dictionary.Keys;

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => dictionary.Keys;

        object ICollection.SyncRoot => collectionInterface.SyncRoot;

        public virtual Dictionary<TKey, TValue>.ValueCollection Values => dictionary.Values;

        ICollection<TValue> IDictionary<TKey, TValue>.Values => dictionary.Values;

        ICollection IDictionary.Values => dictionary.Values;

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => dictionary.Values;
    }
}
