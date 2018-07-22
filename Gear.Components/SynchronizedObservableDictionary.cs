using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Gear.Components
{
    public class SynchronizedObservableDictionary<TKey, TValue> : ObservableDictionary<TKey, TValue>, ISynchronizable
    {
        public SynchronizedObservableDictionary(SynchronizationContext synchronizationContext, bool isSynchronized = true) : base()
        {
            SynchronizationContext = synchronizationContext;
            this.isSynchronized = isSynchronized;
        }

        public SynchronizedObservableDictionary(SynchronizationContext synchronizationContext, IDictionary<TKey, TValue> dictionary, bool isSynchronized = true)
        {
            SynchronizationContext = synchronizationContext;
            this.isSynchronized = isSynchronized;
        }

        public SynchronizedObservableDictionary(SynchronizationContext synchronizationContext, IEqualityComparer<TKey> comparer, bool isSynchronized = true)
        {
            SynchronizationContext = synchronizationContext;
            this.isSynchronized = isSynchronized;
        }

        public SynchronizedObservableDictionary(SynchronizationContext synchronizationContext, int capacity, bool isSynchronized = true)
        {
            SynchronizationContext = synchronizationContext;
            this.isSynchronized = isSynchronized;
        }

        public SynchronizedObservableDictionary(SynchronizationContext synchronizationContext, IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer, bool isSynchronized = true)
        {
            SynchronizationContext = synchronizationContext;
            this.isSynchronized = isSynchronized;
        }

        public SynchronizedObservableDictionary(SynchronizationContext synchronizationContext, int capacity, IEqualityComparer<TKey> comparer, bool isSynchronized = true)
        {
            SynchronizationContext = synchronizationContext;
            this.isSynchronized = isSynchronized;
        }

        bool isSynchronized;

        public override void Add(TKey key, TValue value) => this.Execute(() => base.Add(key, value));

        protected override void Add(object key, object value) => this.Execute(() => base.Add(key, value));

        protected override void Add(KeyValuePair<TKey, TValue> item) => this.Execute(() => base.Add(item));

        public override void AddRange(IReadOnlyList<KeyValuePair<TKey, TValue>> keyValuePairs) => this.Execute(() => base.AddRange(keyValuePairs));

        public override void Clear() => this.Execute(() => base.Clear());

        protected override bool Contains(object key) => this.Execute(() => base.Contains(key));

        protected override bool Contains(KeyValuePair<TKey, TValue> item) => this.Execute(() => base.Contains(item));

        public override bool ContainsKey(TKey key) => this.Execute(() => base.ContainsKey(key));

        public override bool ContainsValue(TValue value) => this.Execute(() => base.ContainsValue(value));

        protected override void CopyTo(Array array, int index) => this.Execute(() => base.CopyTo(array, index));

        protected override void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => this.Execute(() => base.CopyTo(array, arrayIndex));

        protected override IDictionaryEnumerator GetDictionaryEnumerator() => this.Execute(() => base.GetDictionaryEnumerator());

        public override Dictionary<TKey, TValue>.Enumerator GetEnumerator() => this.Execute(() => base.GetEnumerator());

        protected override IEnumerator<KeyValuePair<TKey, TValue>> GetKeyValuePairEnumerator() => this.Execute(() => base.GetKeyValuePairEnumerator());

        protected override IEnumerator GetNonGenericEnumerator() => this.Execute(() => base.GetNonGenericEnumerator());

        protected override object GetValue(object key) => this.Execute(() => base.GetValue(key));

        public override bool Remove(TKey key) => this.Execute(() => base.Remove(key));

        protected override void Remove(object key) => this.Execute(() => base.Remove(key));

        protected override bool Remove(KeyValuePair<TKey, TValue> item) => this.Execute(() => base.Remove(item));

        public override IReadOnlyList<TKey> RemoveRange(IReadOnlyList<TKey> keys) => this.Execute(() => base.RemoveRange(keys));

        protected override void SetValue(object key, object value) => this.Execute(() => base.SetValue(key, value));

        protected override (bool valueRetrieved, TValue value) TryGetValue(TKey key) => this.Execute(() => base.TryGetValue(key));

        public override TValue this[TKey key]
        {
            get => this.Execute(() => base[key]);
            set => this.Execute(() => base[key] = value);
        }

        public override IEqualityComparer<TKey> Comparer => this.Execute(() => base.Comparer);

        public override int Count => this.Execute(() => base.Count);

        protected override bool DictionaryIsReadOnly => this.Execute(() => base.DictionaryIsReadOnly);

        protected override bool GenericCollectionIsReadOnly => this.Execute(() => base.GenericCollectionIsReadOnly);

        protected override bool IsCollectionSynchronized => this.Execute(() => base.IsCollectionSynchronized);

        protected override bool IsFixedSize => this.Execute(() => base.IsFixedSize);

        public bool IsSynchronized
        {
            get => isSynchronized;
            set => SetBackedProperty(ref isSynchronized, in value);
        }

        public override Dictionary<TKey, TValue>.KeyCollection Keys => this.Execute(() => base.Keys);

        protected override ICollection KeysCollection => this.Execute(() => base.KeysCollection);

        protected override ICollection<TKey> KeysGenericCollection => this.Execute(() => base.KeysGenericCollection);

        protected override IEnumerable<TKey> KeysGenericEnumerable => this.Execute(() => base.KeysGenericEnumerable);

        public SynchronizationContext SynchronizationContext { get; }

        protected override object SyncRoot => this.Execute(() => base.SyncRoot);

        public override Dictionary<TKey, TValue>.ValueCollection Values => this.Execute(() => base.Values);

        protected override ICollection ValuesCollection => this.Execute(() => base.ValuesCollection);

        protected override ICollection<TValue> ValuesGenericCollection => this.Execute(() => base.ValuesGenericCollection);

        protected override IEnumerable<TValue> ValuesGenericEnumerable => this.Execute(() => base.ValuesGenericEnumerable);
    }
}
