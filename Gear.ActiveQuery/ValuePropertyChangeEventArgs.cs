using System;

namespace Gear.ActiveQuery
{
    public class ValuePropertyChangeEventArgs<TKey, TValue> : EventArgs
    {
        public ValuePropertyChangeEventArgs(TKey key, TValue value, string propertyName)
        {
            Key = key;
            Value = value;
            PropertyName = propertyName;
        }

        public TKey Key { get; }
        public string PropertyName { get; }
        public TValue Value { get; }
    }
}
