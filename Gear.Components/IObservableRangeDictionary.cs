using System.Collections.Generic;
using System.Collections.Specialized;

namespace Gear.Components
{
    public interface IObservableRangeDictionary<TKey, TValue> : INotifyCollectionChanged, INotifyGenericCollectionChanged<KeyValuePair<TKey, TValue>>, INotifyDictionaryChanged, INotifyDictionaryChanged<TKey, TValue>, IRangeDictionary<TKey, TValue>
    {
    }
}
