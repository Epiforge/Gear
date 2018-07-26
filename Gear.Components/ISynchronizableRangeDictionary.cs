using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gear.Components
{
    public interface ISynchronizableRangeDictionary<TKey, TValue> : IRangeDictionary<TKey, TValue>, ISynchronizable
    {
        Task AddAsync(TKey key, TValue value);
        Task AddRangeAsync(IReadOnlyList<KeyValuePair<TKey, TValue>> keyValuePairs);
        Task ClearAsync();
        Task<bool> ContainsKeyAsync(TKey key);
        Task<TValue> GetValueAsync(TKey key);
        Task<bool> RemoveAsync(TKey key);
        Task<IReadOnlyList<TKey>> RemoveRangeAsync(IReadOnlyList<TKey> keys);
        Task SetValueAsync(TKey key, TValue value);
        Task<(bool valueRetrieved, TValue value)> TryGetValueAsync(TKey key);

        Task<int> CountAsync { get; }
    }
}
