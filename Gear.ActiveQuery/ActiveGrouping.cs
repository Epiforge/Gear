using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Gear.ActiveQuery
{
    public class ActiveGrouping<TKey, TElement> : ActiveEnumerable<TElement>, IGrouping<TKey, TElement>
    {
        internal ActiveGrouping(TKey key, ObservableCollection<TElement> list, INotifyElementFaultChanges faultNotifier = null, Action onDispose = null) : base(list, faultNotifier, onDispose) => Key = key;

        internal ActiveGrouping(TKey key, ObservableCollection<TElement> list, Action onDispose) : base(list, onDispose) => Key = key;

        public TKey Key { get; }
    }
}
