using System;
using System.Collections.Generic;

namespace Gear.Components
{
    public interface IRangeDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
    {
        void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs);
        void AddRange(IReadOnlyList<KeyValuePair<TKey, TValue>> keyValuePairs);
        new bool ContainsKey(TKey key);
        IReadOnlyList<KeyValuePair<TKey, TValue>> GetRange(IEnumerable<TKey> keys);
        IReadOnlyList<KeyValuePair<TKey, TValue>> RemoveAll(Func<TKey, TValue, bool> predicate);
        IReadOnlyList<TKey> RemoveRange(IEnumerable<TKey> keys);
        void ReplaceRange(IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs);
        void Reset();
        void Reset(IDictionary<TKey, TValue> dictionary);
        new bool TryGetValue(TKey key, out TValue value);

        new TValue this[TKey key] { get; set; }
        new IEnumerable<TKey> Keys { get; }
        new IEnumerable<TValue> Values { get; }
    }
}
