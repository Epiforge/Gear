using System;
using System.Collections.Generic;

namespace Gear.Components
{
    public class NotifyDictionaryValuesEventArgs<TKey, TValue> : EventArgs
    {
        public NotifyDictionaryValuesEventArgs(IReadOnlyList<KeyValuePair<TKey, TValue>> keyValuePairs) => KeyValuePairs = keyValuePairs;

        public IReadOnlyList<KeyValuePair<TKey, TValue>> KeyValuePairs { get; }
    }
}
