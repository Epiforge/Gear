namespace Gear.Components
{
    public interface IObservableRangeDictionary<TKey, TValue> : INotifyDictionaryChanged<TKey, TValue>, IRangeDictionary<TKey, TValue>
    {
    }
}
