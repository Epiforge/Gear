using System.Collections.Generic;

namespace Gear.Components
{
    public interface IRangeDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs);
        void AddRange(IReadOnlyList<KeyValuePair<TKey, TValue>> keyValuePairs);
        IReadOnlyList<TKey> RemoveRange(IEnumerable<TKey> keys);
    }
}
