using System.Collections.Generic;

namespace Gear.Components
{
    public interface IRangeDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        void AddRange(IReadOnlyList<KeyValuePair<TKey, TValue>> keyValuePairs);
        IReadOnlyList<TKey> RemoveRange(IReadOnlyList<TKey> keys);
    }
}
