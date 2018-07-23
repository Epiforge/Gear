using Gear.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace Gear.ActiveQuery
{
    public class ActiveLookup<TKey, TValue> : SyncDisposablePropertyChangeNotifier, INotifyDictionaryChanged<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
    {
        internal ActiveLookup(IReadOnlyDictionary<TKey, TValue> readOnlyDictionary, Action<bool> onDispose = null)
        {
            if (readOnlyDictionary is ActiveLookup<TKey, TValue> activeLookup)
                this.readOnlyDictionary = activeLookup.readOnlyDictionary;
            else
                this.readOnlyDictionary = readOnlyDictionary;
            if (this.readOnlyDictionary is INotifyDictionaryChanged<TKey, TValue> dictionaryNotifier)
            {
                dictionaryNotifier.ValueAdded += ValueAddedHandler;
                dictionaryNotifier.ValueRemoved += ValueRemovedHandler;
                dictionaryNotifier.ValueReplaced += ValueReplacedHandler;
                dictionaryNotifier.ValuesAdded += ValuesAddedHandler;
                dictionaryNotifier.ValuesRemoved += ValuesRemovedHandler;
            }
            if (this.readOnlyDictionary is INotifyPropertyChanged propertyChangedNotifier)
                propertyChangedNotifier.PropertyChanged += PropertyChangedHandler;
            if (this.readOnlyDictionary is INotifyPropertyChanging propertyChangingNotifier)
                propertyChangingNotifier.PropertyChanging += PropertyChangingHandler;
            this.onDispose = onDispose;
        }

        readonly IReadOnlyDictionary<TKey, TValue> readOnlyDictionary;
        readonly Action<bool> onDispose;

        public event EventHandler<NotifyDictionaryValueEventArgs<TKey, TValue>> ValueAdded;
        public event EventHandler<NotifyDictionaryValueEventArgs<TKey, TValue>> ValueRemoved;
        public event EventHandler<NotifyDictionaryValueReplacedEventArgs<TKey, TValue>> ValueReplaced;
        public event EventHandler<NotifyDictionaryValuesEventArgs<TKey, TValue>> ValuesAdded;
        public event EventHandler<NotifyDictionaryValuesEventArgs<TKey, TValue>> ValuesRemoved;

        public bool ContainsKey(TKey key) => readOnlyDictionary.ContainsKey(key);

        protected override void Dispose(bool disposing)
        {
            onDispose?.Invoke(disposing);
            if (disposing)
            {
                if (readOnlyDictionary is INotifyDictionaryChanged<TKey, TValue> dictionaryNotifier)
                {
                    dictionaryNotifier.ValueAdded -= ValueAddedHandler;
                    dictionaryNotifier.ValueRemoved -= ValueRemovedHandler;
                    dictionaryNotifier.ValueReplaced -= ValueReplacedHandler;
                    dictionaryNotifier.ValuesAdded -= ValuesAddedHandler;
                    dictionaryNotifier.ValuesRemoved -= ValuesRemovedHandler;
                }
                if (readOnlyDictionary is INotifyPropertyChanged propertyChangedNotifier)
                    propertyChangedNotifier.PropertyChanged -= PropertyChangedHandler;
                if (readOnlyDictionary is INotifyPropertyChanging propertyChangingNotifier)
                    propertyChangingNotifier.PropertyChanging -= PropertyChangingHandler;
            }
        }

        void PropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Count))
                OnPropertyChanged(e);
        }

        void PropertyChangingHandler(object sender, PropertyChangingEventArgs e)
        {
            if (e.PropertyName == nameof(Count))
                OnPropertyChanging(e);
        }

        public bool TryGetValue(TKey key, out TValue value) => readOnlyDictionary.TryGetValue(key, out value);

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => readOnlyDictionary.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)readOnlyDictionary).GetEnumerator();

        void ValueAddedHandler(object sender, NotifyDictionaryValueEventArgs<TKey, TValue> e) => ValueAdded?.Invoke(this, e);

        void ValueRemovedHandler(object sender, NotifyDictionaryValueEventArgs<TKey, TValue> e) => ValueRemoved?.Invoke(this, e);

        void ValueReplacedHandler(object sender, NotifyDictionaryValueReplacedEventArgs<TKey, TValue> e) => ValueReplaced?.Invoke(this, e);

        void ValuesAddedHandler(object sender, NotifyDictionaryValuesEventArgs<TKey, TValue> e) => ValuesAdded?.Invoke(this, e);

        void ValuesRemovedHandler(object sender, NotifyDictionaryValuesEventArgs<TKey, TValue> e) => ValuesRemoved?.Invoke(this, e);

        public TValue this[TKey key] => readOnlyDictionary[key];

        public int Count => readOnlyDictionary.Count;

        public IEnumerable<TKey> Keys => readOnlyDictionary.Keys;

        public IEnumerable<TValue> Values => readOnlyDictionary.Values;
    }
}
