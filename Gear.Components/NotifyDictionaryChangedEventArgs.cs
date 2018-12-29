using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Gear.Components
{
    public class NotifyDictionaryChangedEventArgs<TKey, TValue> : EventArgs
    {
        public NotifyDictionaryChangedEventArgs(NotifyDictionaryChangedAction action)
        {
            if (action != NotifyDictionaryChangedAction.Reset)
                throw new ArgumentOutOfRangeException(nameof(action));
            InitializeAdd(action);
        }

        public NotifyDictionaryChangedEventArgs(NotifyDictionaryChangedAction action, TKey key, TValue value) : this(action, new KeyValuePair<TKey, TValue>(key, value))
        {
        }

        public NotifyDictionaryChangedEventArgs(NotifyDictionaryChangedAction action, KeyValuePair<TKey, TValue> changedItem) : this(action, new KeyValuePair<TKey, TValue>[] { changedItem })
        {
        }

        public NotifyDictionaryChangedEventArgs(NotifyDictionaryChangedAction action, IEnumerable<KeyValuePair<TKey, TValue>> changedItems)
        {
            switch (action)
            {
                case NotifyDictionaryChangedAction.Add:
                    InitializeAdd(action, changedItems);
                    break;
                case NotifyDictionaryChangedAction.Remove:
                    InitializeRemove(action, changedItems);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action));
            }
        }

        public NotifyDictionaryChangedEventArgs(NotifyDictionaryChangedAction action, TKey key, TValue newValue, TValue oldValue) : this(action, new KeyValuePair<TKey, TValue>(key, newValue), new KeyValuePair<TKey, TValue>(key, oldValue))
        {
        }

        public NotifyDictionaryChangedEventArgs(NotifyDictionaryChangedAction action, KeyValuePair<TKey, TValue> newItem, KeyValuePair<TKey, TValue> oldItem) : this(action, new KeyValuePair<TKey, TValue>[] { newItem }, new KeyValuePair<TKey, TValue>[] { oldItem })
        {
        }

        public NotifyDictionaryChangedEventArgs(NotifyDictionaryChangedAction action, IEnumerable<KeyValuePair<TKey, TValue>> newItems, IEnumerable<KeyValuePair<TKey, TValue>> oldItems)
        {
            if (action != NotifyDictionaryChangedAction.Replace)
                throw new ArgumentOutOfRangeException(nameof(action));
            InitializeAdd(action, newItems);
            InitializeRemove(action, oldItems);
        }

        void InitializeAdd(NotifyDictionaryChangedAction action, IEnumerable<KeyValuePair<TKey, TValue>> newItems = null)
        {
            Action = action;
            NewItems = newItems?.ToImmutableArray();
        }

        void InitializeRemove(NotifyDictionaryChangedAction action, IEnumerable<KeyValuePair<TKey, TValue>> oldItems)
        {
            Action = action;
            OldItems = oldItems?.ToImmutableArray();
        }

        public NotifyDictionaryChangedAction Action { get; private set; }

        public IReadOnlyList<KeyValuePair<TKey, TValue>> NewItems { get; private set; }

        public IReadOnlyList<KeyValuePair<TKey, TValue>> OldItems { get; private set; }
    }
}
