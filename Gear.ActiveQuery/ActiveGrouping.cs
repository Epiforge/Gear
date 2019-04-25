using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Gear.ActiveQuery
{
    /// <summary>
    /// Represents a group of elements that is itself an element in the results of a group by active query
    /// </summary>
    /// <typeparam name="TKey">The type of the values by which the source elements are being grouped</typeparam>
    /// <typeparam name="TElement">The type of the source elements</typeparam>
    public class ActiveGrouping<TKey, TElement> : ActiveEnumerable<TElement>, IGrouping<TKey, TElement>
    {
        public ActiveGrouping(TKey key, ObservableCollection<TElement> list, INotifyElementFaultChanges faultNotifier = null, Action onDispose = null) : base(list, faultNotifier, onDispose) => Key = key;

        /// <summary>
        /// Gets the value shared by the source elements in this group
        /// </summary>
        public TKey Key { get; }
    }
}
