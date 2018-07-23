using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Gear.Components
{
    public class SynchronizedObservableSortedDictionary<TKey, TValue> : ObservableSortedDictionary<TKey, TValue>, ISynchronizable
    {
        public SynchronizedObservableSortedDictionary(SynchronizationContext synchronizationContext, bool isSynchronized = true) : base()
        {
            SynchronizationContext = synchronizationContext;
            this.isSynchronized = isSynchronized;
        }

        public SynchronizedObservableSortedDictionary(SynchronizationContext synchronizationContext, IComparer<TKey> comparer, bool isSynchronized = true) : base(comparer)
        {
            SynchronizationContext = synchronizationContext;
            this.isSynchronized = isSynchronized;
        }

        public SynchronizedObservableSortedDictionary(SynchronizationContext synchronizationContext, IDictionary<TKey, TValue> dictionary, bool isSynchronized = true) : base(dictionary)
        {
            SynchronizationContext = synchronizationContext;
            this.isSynchronized = isSynchronized;
        }

        public SynchronizedObservableSortedDictionary(SynchronizationContext synchronizationContext, IDictionary<TKey, TValue> dictionary, IComparer<TKey> comparer, bool isSynchronized = true) : base(dictionary, comparer)
        {
            SynchronizationContext = synchronizationContext;
            this.isSynchronized = isSynchronized;
        }

        bool isSynchronized;

        public override void Add(TKey key, TValue value) => this.Execute(() => base.Add(key, value));

        protected override void Add(object key, object value) => this.Execute(() => base.Add(key, value));

        protected override void Add(KeyValuePair<TKey, TValue> item) => this.Execute(() => base.Add(item));

        public virtual Task AddAsync(TKey key, TValue value) => this.ExecuteAsync(() => base.Add(key, value));

        public override void AddRange(IReadOnlyList<KeyValuePair<TKey, TValue>> keyValuePairs) => this.Execute(() => base.AddRange(keyValuePairs));

        public virtual Task AddRangeAsync(IReadOnlyList<KeyValuePair<TKey, TValue>> keyValuePairs) => this.ExecuteAsync(() => base.AddRange(keyValuePairs));

        public override void Clear() => this.Execute(() => base.Clear());

        public virtual Task ClearAsync() => this.ExecuteAsync(() => base.Clear());

        protected override bool Contains(object key) => this.Execute(() => base.Contains(key));

        protected override bool Contains(KeyValuePair<TKey, TValue> item) => this.Execute(() => base.Contains(item));

        public override bool ContainsKey(TKey key) => this.Execute(() => base.ContainsKey(key));

        public virtual Task<bool> ContainsKeyAsync(TKey key) => this.ExecuteAsync(() => base.ContainsKey(key));

        public override bool ContainsValue(TValue value) => this.Execute(() => base.ContainsValue(value));

        public virtual Task<bool> ContainsValueAsync(TValue value) => this.ExecuteAsync(() => base.ContainsValue(value));

        protected override void CopyTo(Array array, int index) => this.Execute(() => base.CopyTo(array, index));

        public override void CopyTo(KeyValuePair<TKey, TValue>[] array, int index) => this.Execute(() => base.CopyTo(array, index));

        public virtual Task CopyToAsync(KeyValuePair<TKey, TValue>[] array, int index) => this.ExecuteAsync(() => base.CopyTo(array, index));

        protected override IDictionaryEnumerator GetDictionaryEnumerator() => this.Execute(() => base.GetDictionaryEnumerator());

        public override SortedDictionary<TKey, TValue>.Enumerator GetEnumerator() => this.Execute(() => base.GetEnumerator());

        public virtual Task<SortedDictionary<TKey, TValue>.Enumerator> GetEnumeratorAsync() => this.ExecuteAsync(() => base.GetEnumerator());

        protected override IEnumerator<KeyValuePair<TKey, TValue>> GetKeyValuePairEnumerator() => this.Execute(() => base.GetKeyValuePairEnumerator());

        protected override IEnumerator GetNonGenericEnumerator() => this.Execute(() => base.GetNonGenericEnumerator());

        protected override object GetValue(object key) => this.Execute(() => base.GetValue(key));

        public virtual Task<TValue> GetValueAsync(TKey key) => this.ExecuteAsync(() => base[key]);

        public override bool Remove(TKey key) => this.Execute(() => base.Remove(key));

        protected override void Remove(object key) => this.Execute(() => base.Remove(key));

        protected override bool Remove(KeyValuePair<TKey, TValue> item) => this.Execute(() => base.Remove(item));

        public virtual Task<bool> RemoveAsync(TKey key) => this.ExecuteAsync(() => base.Remove(key));

        public override IReadOnlyList<TKey> RemoveRange(IReadOnlyList<TKey> keys) => this.Execute(() => base.RemoveRange(keys));

        public virtual Task<IReadOnlyList<TKey>> RemoveRangeAsync(IReadOnlyList<TKey> keys) => this.ExecuteAsync(() => base.RemoveRange(keys));

        protected override void SetValue(object key, object value) => this.Execute(() => base.SetValue(key, value));

        public virtual Task SetValue(TKey key, TValue value) => this.ExecuteAsync(() => base[key] = value);

        protected override (bool valueRetrieved, TValue value) TryGetValue(TKey key) => this.Execute(() => base.TryGetValue(key));

        public virtual Task<(bool valueRetrieved, TValue value)> TryGetValueAsync(TKey key) => this.ExecuteAsync(() => base.TryGetValue(key));

        public override TValue this[TKey key]
        {
            get => this.Execute(() => base[key]);
            set => this.Execute(() => base[key] = value);
        }

        public override IComparer<TKey> Comparer => this.Execute(() => base.Comparer);

        public virtual Task<IComparer<TKey>> ComparerAsync => this.ExecuteAsync(() => base.Comparer);

        public override int Count => this.Execute(() => base.Count);

        public virtual Task<int> CountAsync => this.ExecuteAsync(() => base.Count);

        protected override bool DictionaryIsReadOnly => this.Execute(() => base.DictionaryIsReadOnly);

        protected override bool GenericCollectionIsReadOnly => this.Execute(() => base.GenericCollectionIsReadOnly);

        protected override bool IsCollectionSynchronized => this.Execute(() => base.IsCollectionSynchronized);

        protected override bool IsFixedSize => this.Execute(() => base.IsFixedSize);

        public bool IsSynchronized
        {
            get => isSynchronized;
            set => SetBackedProperty(ref isSynchronized, in value);
        }

        public override SortedDictionary<TKey, TValue>.KeyCollection Keys => this.Execute(() => base.Keys);

        public virtual Task<SortedDictionary<TKey, TValue>.KeyCollection> KeysAsync => this.ExecuteAsync(() => base.Keys);

        protected override ICollection KeysCollection => this.Execute(() => base.KeysCollection);

        protected override ICollection<TKey> KeysGenericCollection => this.Execute(() => base.KeysGenericCollection);

        protected override IEnumerable<TKey> KeysGenericEnumerable => this.Execute(() => base.KeysGenericEnumerable);

        public SynchronizationContext SynchronizationContext { get; }

        protected override object SyncRoot => this.Execute(() => base.SyncRoot);

        public override SortedDictionary<TKey, TValue>.ValueCollection Values => this.Execute(() => base.Values);

        public virtual Task<SortedDictionary<TKey, TValue>.ValueCollection> ValuesAsync => this.ExecuteAsync(() => base.Values);

        protected override ICollection ValuesCollection => this.Execute(() => base.ValuesCollection);

        protected override ICollection<TValue> ValuesGenericCollection => this.Execute(() => base.ValuesGenericCollection);

        protected override IEnumerable<TValue> ValuesGenericEnumerable => this.Execute(() => base.ValuesGenericEnumerable);
    }
}
