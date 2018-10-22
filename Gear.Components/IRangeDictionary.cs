using System.Collections.Generic;

namespace Gear.Components
{
    public interface IRangeDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
    {
        void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs);
        void AddRange(IReadOnlyList<KeyValuePair<TKey, TValue>> keyValuePairs);
        new bool ContainsKey(TKey key);
        IReadOnlyList<TKey> RemoveRange(IEnumerable<TKey> keys);
        new bool TryGetValue(TKey key, out TValue value);

        new TValue this[TKey key] { get; set; }
        new IEnumerable<TKey> Keys { get; }
        new IEnumerable<TValue> Values { get; }
    }
}
