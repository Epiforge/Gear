using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Gear.Components
{
    public class NotifyDictionaryValuesEventArgs : EventArgs
    {
        public NotifyDictionaryValuesEventArgs(IReadOnlyList<KeyValuePair<object, object>> keyValuePairs) => KeyValuePairs = keyValuePairs.ToImmutableList();

        public IReadOnlyList<KeyValuePair<object, object>> KeyValuePairs { get; }
    }

    public class NotifyDictionaryValuesEventArgs<TKey, TValue> : EventArgs
    {
        public NotifyDictionaryValuesEventArgs(IReadOnlyList<KeyValuePair<TKey, TValue>> keyValuePairs) => KeyValuePairs = keyValuePairs.ToImmutableList();

        public IReadOnlyList<KeyValuePair<TKey, TValue>> KeyValuePairs { get; }
    }
}
