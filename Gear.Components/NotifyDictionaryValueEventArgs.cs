using System;

namespace Gear.Components
{
    public class NotifyDictionaryValueEventArgs<TKey, TValue> : EventArgs
    {
        public NotifyDictionaryValueEventArgs(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }

        public TKey Key { get; }
        public TValue Value { get; }
    }
}
