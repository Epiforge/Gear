using System;
using System.Collections.Generic;

namespace Gear.Components
{
    public class NotifyDictionaryChangedEventArgs<TKey, TValue> : EventArgs
    {
        public NotifyDictionaryChangedEventArgs(IReadOnlyList<KeyValuePair<TKey, TValue>> added = default, IReadOnlyList<KeyValuePair<TKey, TValue>> removed = default)
        {
            Added = added;
            Removed = removed;
        }

        public IReadOnlyList<KeyValuePair<TKey, TValue>> Added { get; }
        public IReadOnlyList<KeyValuePair<TKey, TValue>> Removed { get; }
    }
}
