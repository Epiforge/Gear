namespace Gear.Components
{
    public interface ISynchronizedObservableRangeDictionary<TKey, TValue> : IObservableRangeDictionary<TKey, TValue>, ISynchronized
    {
    }
}
