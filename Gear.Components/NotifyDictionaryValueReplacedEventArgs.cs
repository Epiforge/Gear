using System;

namespace Gear.Components
{
    public class NotifyDictionaryValueReplacedEventArgs : EventArgs
    {
        public NotifyDictionaryValueReplacedEventArgs(object key, object oldValue, object newValue)
        {
            Key = key;
            OldValue = oldValue;
            NewValue = newValue;
        }

        public object Key { get; }
        public object NewValue { get; }
        public object OldValue { get; }
    }

    public class NotifyDictionaryValueReplacedEventArgs<TKey, TValue> : EventArgs
    {
        public NotifyDictionaryValueReplacedEventArgs(TKey key, TValue oldValue, TValue newValue)
        {
            Key = key;
            OldValue = oldValue;
            NewValue = newValue;
        }

        public TKey Key { get; }
        public TValue NewValue { get; }
        public TValue OldValue { get; }
    }
}
