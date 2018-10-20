namespace Gear.Components
{
    public interface ISynchronizableObservableRangeDictionary<TKey, TValue> : IObservableRangeDictionary<TKey, TValue>, ISynchronizable
    {
    }
}
