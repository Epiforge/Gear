using Gear.Components;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Gear.ActiveQuery
{
    public class ActiveDictionaryMonitor<TKey, TValue> : SyncDisposable
    {
        static readonly Dictionary<(IDictionary<TKey, TValue> dictionary, string relevantProperties), object> monitors = new Dictionary<(IDictionary<TKey, TValue> dictionary, string relevantProperties), object>();
        static readonly object instanceManagementLock = new object();

        public static ActiveDictionaryMonitor<TKey, TValue> Monitor(IDictionary<TKey, TValue> dictionary, params string[] relevantPropertyNames)
        {
            string relevantProperties;
            if (relevantPropertyNames?.Any() ?? false)
                relevantProperties = string.Join("|", relevantPropertyNames.OrderBy(s => s));
            else
                relevantProperties = null;
            var key = (dictionary, relevantProperties);
            lock (instanceManagementLock)
            {
                if (!monitors.TryGetValue(key, out var obj))
                {
                    obj = new ActiveDictionaryMonitor<TKey, TValue>(dictionary, relevantPropertyNames);
                    monitors.Add(key, obj);
                }
                var monitor = obj as ActiveDictionaryMonitor<TKey, TValue>;
                ++monitor.instances;
                return monitor;
            }
        }

        readonly IDictionary<TKey, TValue> dictionary;
        int instances;
        readonly Dictionary<TKey, PropertyChangedEventHandler> valuePropertyChangedEventHandlers = new Dictionary<TKey, PropertyChangedEventHandler>();
        readonly Dictionary<TKey, PropertyChangingEventHandler> valuePropertyChangingEventHandlers = new Dictionary<TKey, PropertyChangingEventHandler>();
        readonly IReadOnlyList<string> relevantPropertyNames;

        public event EventHandler<NotifyDictionaryValueEventArgs<TKey, TValue>> ValueAdded;
        public event EventHandler<ValuePropertyChangeEventArgs<TKey, TValue>> ValuePropertyChanged;
        public event EventHandler<ValuePropertyChangeEventArgs<TKey, TValue>> ValuePropertyChanging;
        public event EventHandler<NotifyDictionaryValueEventArgs<TKey, TValue>> ValueRemoved;
        public event EventHandler<NotifyDictionaryValueReplacedEventArgs<TKey, TValue>> ValueReplaced;
        public event EventHandler<NotifyDictionaryValuesEventArgs<TKey, TValue>> ValuesAdded;
        public event EventHandler<NotifyDictionaryValuesEventArgs<TKey, TValue>> ValuesRemoved;

        ActiveDictionaryMonitor(IDictionary<TKey, TValue> dictionary, params string[] relevantPropertyNames)
        {
            var valueTypeInfo = typeof(TValue).GetTypeInfo();
            ValuesNotifyChanging = typeof(INotifyPropertyChanging).GetTypeInfo().IsAssignableFrom(valueTypeInfo);
            ValuesNotifyChanged = typeof(INotifyPropertyChanged).GetTypeInfo().IsAssignableFrom(valueTypeInfo);
            this.relevantPropertyNames = relevantPropertyNames.ToList();

            this.dictionary = dictionary;
            if (this.dictionary is INotifyDictionaryChanged<TKey, TValue> notifyingDictionary)
            {
                notifyingDictionary.ValueAdded += ValueAddedHandler;
                notifyingDictionary.ValueRemoved += ValueRemovedHandler;
                notifyingDictionary.ValueReplaced += ValueReplacedHandler;
                notifyingDictionary.ValuesAdded += ValuesAddedHandler;
                notifyingDictionary.ValuesRemoved += ValuesRemovedHandler;
            }
            AttachToValues(dictionary);
        }

        void AttachToValue(TKey key, TValue value)
        {
            if (ValuesNotifyChanging && ValuesNotifyChanged)
            {
                void valuePropertyChangingEventHandler(object sender, PropertyChangingEventArgs e) => OnValuePropertyChanging(key, value, e.PropertyName);
                void valuePropertyChangedEventHandler(object sender, PropertyChangedEventArgs e) => OnValuePropertyChanged(key, value, e.PropertyName);
                valuePropertyChangingEventHandlers.Add(key, valuePropertyChangingEventHandler);
                valuePropertyChangedEventHandlers.Add(key, valuePropertyChangedEventHandler);
                ((INotifyPropertyChanging)value).PropertyChanging += valuePropertyChangingEventHandler;
                ((INotifyPropertyChanged)value).PropertyChanged += valuePropertyChangedEventHandler;
            }
            else if (ValuesNotifyChanging)
            {
                void valuePropertyChangingEventHandler(object sender, PropertyChangingEventArgs e) => OnValuePropertyChanging(key, value, e.PropertyName);
                valuePropertyChangingEventHandlers.Add(key, valuePropertyChangingEventHandler);
                ((INotifyPropertyChanging)value).PropertyChanging += valuePropertyChangingEventHandler;
            }
            else if (ValuesNotifyChanged)
            {
                void valuePropertyChangedEventHandler(object sender, PropertyChangedEventArgs e) => OnValuePropertyChanged(key, value, e.PropertyName);
                valuePropertyChangedEventHandlers.Add(key, valuePropertyChangedEventHandler);
                ((INotifyPropertyChanged)value).PropertyChanged += valuePropertyChangedEventHandler;
            }
        }

        void AttachToValues(IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs)
        {
            if (ValuesNotifyChanging && ValuesNotifyChanged)
                foreach (var keyValuePair in keyValuePairs)
                {
                    var key = keyValuePair.Key;
                    var value = keyValuePair.Value;
                    void valuePropertyChangingEventHandler(object sender, PropertyChangingEventArgs e) => OnValuePropertyChanging(key, value, e.PropertyName);
                    void valuePropertyChangedEventHandler(object sender, PropertyChangedEventArgs e) => OnValuePropertyChanged(key, value, e.PropertyName);
                    valuePropertyChangingEventHandlers.Add(key, valuePropertyChangingEventHandler);
                    valuePropertyChangedEventHandlers.Add(key, valuePropertyChangedEventHandler);
                    ((INotifyPropertyChanging)value).PropertyChanging += valuePropertyChangingEventHandler;
                    ((INotifyPropertyChanged)value).PropertyChanged += valuePropertyChangedEventHandler;
                }
            else if (ValuesNotifyChanging)
                foreach (var keyValuePair in keyValuePairs)
                {
                    var key = keyValuePair.Key;
                    var value = keyValuePair.Value;
                    void valuePropertyChangingEventHandler(object sender, PropertyChangingEventArgs e) => OnValuePropertyChanging(key, value, e.PropertyName);
                    valuePropertyChangingEventHandlers.Add(key, valuePropertyChangingEventHandler);
                    ((INotifyPropertyChanging)value).PropertyChanging += valuePropertyChangingEventHandler;
                }
            else if (ValuesNotifyChanged)
                foreach (var keyValuePair in keyValuePairs)
                {
                    var key = keyValuePair.Key;
                    var value = keyValuePair.Value;
                    void valuePropertyChangedEventHandler(object sender, PropertyChangedEventArgs e) => OnValuePropertyChanged(key, value, e.PropertyName);
                    valuePropertyChangedEventHandlers.Add(key, valuePropertyChangedEventHandler);
                    ((INotifyPropertyChanged)value).PropertyChanged += valuePropertyChangedEventHandler;
                }
        }

        void DetachFromValue(TKey key, TValue value)
        {
            if (ValuesNotifyChanging && ValuesNotifyChanged)
            {
                ((INotifyPropertyChanging)value).PropertyChanging -= valuePropertyChangingEventHandlers[key];
                valuePropertyChangingEventHandlers.Remove(key);
                ((INotifyPropertyChanged)value).PropertyChanged -= valuePropertyChangedEventHandlers[key];
                valuePropertyChangedEventHandlers.Remove(key);
            }
            else if (ValuesNotifyChanging)
            {
                ((INotifyPropertyChanging)value).PropertyChanging -= valuePropertyChangingEventHandlers[key];
                valuePropertyChangingEventHandlers.Remove(key);
            }
            else if (ValuesNotifyChanged)
            {
                ((INotifyPropertyChanged)value).PropertyChanged -= valuePropertyChangedEventHandlers[key];
                valuePropertyChangedEventHandlers.Remove(key);
            }
        }

        void DetachFromValues(IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs)
        {
            if (ValuesNotifyChanging && ValuesNotifyChanged)
                foreach (var keyValuePair in keyValuePairs)
                {
                    var key = keyValuePair.Key;
                    var value = keyValuePair.Value;
                    ((INotifyPropertyChanging)value).PropertyChanging -= valuePropertyChangingEventHandlers[key];
                    valuePropertyChangingEventHandlers.Remove(key);
                    ((INotifyPropertyChanged)value).PropertyChanged -= valuePropertyChangedEventHandlers[key];
                    valuePropertyChangedEventHandlers.Remove(key);
                }
            else if (ValuesNotifyChanging)
                foreach (var keyValuePair in keyValuePairs)
                {
                    var key = keyValuePair.Key;
                    ((INotifyPropertyChanging)keyValuePair.Value).PropertyChanging -= valuePropertyChangingEventHandlers[key];
                    valuePropertyChangingEventHandlers.Remove(key);
                }
            else if (ValuesNotifyChanged)
                foreach (var keyValuePair in keyValuePairs)
                {
                    var key = keyValuePair.Key;
                    ((INotifyPropertyChanged)keyValuePair.Value).PropertyChanged -= valuePropertyChangedEventHandlers[key];
                    valuePropertyChangedEventHandlers.Remove(key);
                }
        }

        protected override void Dispose(bool disposing)
        {
            lock (instanceManagementLock)
            {
                if (--instances > 0)
                    return;
                string relevantProperties;
                if (relevantPropertyNames?.Any() ?? false)
                    relevantProperties = string.Join("|", relevantPropertyNames.OrderBy(s => s));
                else
                    relevantProperties = null;
                monitors.Remove((dictionary, relevantProperties));
                if (disposing)
                {
                    if (dictionary is INotifyDictionaryChanged<TKey, TValue> notifyingDictionary)
                    {
                        notifyingDictionary.ValueAdded -= ValueAddedHandler;
                        notifyingDictionary.ValueRemoved -= ValueRemovedHandler;
                        notifyingDictionary.ValueReplaced -= ValueReplacedHandler;
                        notifyingDictionary.ValuesAdded -= ValuesAddedHandler;
                        notifyingDictionary.ValuesRemoved -= ValuesRemovedHandler;
                    }
                    DetachFromValues(dictionary);
                }
            }
        }

        protected virtual void OnValueAdded(NotifyDictionaryValueEventArgs<TKey, TValue> e) => ValueAdded?.Invoke(this, e);

        protected virtual void OnValuePropertyChanged(ValuePropertyChangeEventArgs<TKey, TValue> e) => ValuePropertyChanged?.Invoke(this, e);

        void OnValuePropertyChanged(TKey key, TValue value, string propertyName)
        {
            if (relevantPropertyNames.Count == 0 || relevantPropertyNames.Contains(propertyName))
                OnValuePropertyChanged(new ValuePropertyChangeEventArgs<TKey, TValue>(key, value, propertyName));
        }

        protected virtual void OnValuePropertyChanging(ValuePropertyChangeEventArgs<TKey, TValue> e) => ValuePropertyChanging?.Invoke(this, e);

        void OnValuePropertyChanging(TKey key, TValue value, string propertyName)
        {
            if (relevantPropertyNames.Count == 0 || relevantPropertyNames.Contains(propertyName))
                OnValuePropertyChanging(new ValuePropertyChangeEventArgs<TKey, TValue>(key, value, propertyName));
        }

        protected virtual void OnValueRemoved(NotifyDictionaryValueEventArgs<TKey, TValue> e) => ValueRemoved?.Invoke(this, e);

        protected virtual void OnValueReplaced(NotifyDictionaryValueReplacedEventArgs<TKey, TValue> e) => ValueReplaced?.Invoke(this, e);

        protected virtual void OnValuesAdded(NotifyDictionaryValuesEventArgs<TKey, TValue> e) => ValuesAdded?.Invoke(this, e);

        protected virtual void OnValuesRemoved(NotifyDictionaryValuesEventArgs<TKey, TValue> e) => ValuesRemoved?.Invoke(this, e);

        private void ValueAddedHandler(object sender, NotifyDictionaryValueEventArgs<TKey, TValue> e)
        {
            AttachToValue(e.Key, e.Value);
            OnValueAdded(e);
        }

        private void ValueRemovedHandler(object sender, NotifyDictionaryValueEventArgs<TKey, TValue> e)
        {
            DetachFromValue(e.Key, e.Value);
            OnValueRemoved(e);
        }

        private void ValueReplacedHandler(object sender, NotifyDictionaryValueReplacedEventArgs<TKey, TValue> e)
        {
            DetachFromValue(e.Key, e.OldValue);
            AttachToValue(e.Key, e.NewValue);
            OnValueReplaced(e);
        }

        private void ValuesAddedHandler(object sender, NotifyDictionaryValuesEventArgs<TKey, TValue> e)
        {
            AttachToValues(e.KeyValuePairs);
            OnValuesAdded(e);
        }

        private void ValuesRemovedHandler(object sender, NotifyDictionaryValuesEventArgs<TKey, TValue> e)
        {
            DetachFromValues(e.KeyValuePairs);
            OnValuesRemoved(e);
        }

        public bool ValuesNotifyChanged { get; }

        public bool ValuesNotifyChanging { get; }
    }
}
