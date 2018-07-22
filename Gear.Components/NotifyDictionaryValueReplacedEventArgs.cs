namespace Gear.Components
{
    public class NotifyDictionaryValueReplacedEventArgs<TKey, TValue>
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
