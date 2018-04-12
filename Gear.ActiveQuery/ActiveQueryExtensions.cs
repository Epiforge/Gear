using Gear.Components;
using Nito.AsyncEx;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;

namespace Gear.ActiveQuery
{
    public static class ActiveQueryExtensions
    {
        public static ActiveEnumerable<TResult> ActiveCast<TResult>(this IEnumerable source) =>
            ActiveCast<TResult>(source, (source as IsSynchronizable)?.SynchronizationContext);

        public static ActiveEnumerable<TResult> ActiveCast<TResult>(this IEnumerable source, SynchronizationContext synchronizationContext)
        {
            var rangeObservableCollection = new SynchronizedRangeObservableCollection<TResult>(synchronizationContext, source.Cast<TResult>(), false);
            var rangeObservableCollectionAccess = new AsyncLock();

            async void notifyCollectionChangedEventHandler(object sender, NotifyCollectionChangedEventArgs e)
            {
                using (await rangeObservableCollectionAccess.LockAsync().ConfigureAwait(false))
                {
                    if (e.Action == NotifyCollectionChangedAction.Reset)
                        await rangeObservableCollection.ReplaceAllAsync(source.Cast<TResult>()).ConfigureAwait(false);
                    else if (e.Action == NotifyCollectionChangedAction.Move)
                        await rangeObservableCollection.MoveRangeAsync(e.OldStartingIndex, e.NewStartingIndex, e.OldItems.Count).ConfigureAwait(false);
                    else
                    {
                        if (e.OldItems != null && e.OldStartingIndex >= 0)
                            await rangeObservableCollection.RemoveRangeAsync(e.OldStartingIndex, e.OldItems.Count).ConfigureAwait(false);
                        if (e.NewItems != null && e.NewStartingIndex >= 0)
                            await rangeObservableCollection.InsertRangeAsync(e.NewStartingIndex, e.NewItems.Cast<TResult>()).ConfigureAwait(false);
                    }
                }
            }

            if (source is INotifyCollectionChanged)
                ((INotifyCollectionChanged)source).CollectionChanged += notifyCollectionChangedEventHandler;
            var result = new ActiveEnumerable<TResult>(rangeObservableCollection, disposing =>
            {
                if (source is INotifyCollectionChanged)
                    ((INotifyCollectionChanged)source).CollectionChanged -= notifyCollectionChangedEventHandler;
            });
            if (synchronizationContext != null)
                rangeObservableCollection.IsSynchronized = true;
            return result;
        }

        public static ActiveEnumerable<TSource> ActiveConcat<TSource>(this IList<TSource> first, IList<TSource> second) =>
            ActiveConcat(first, second, (first as IsSynchronizable)?.SynchronizationContext ?? (second as IsSynchronizable)?.SynchronizationContext);

        public static ActiveEnumerable<TSource> ActiveConcat<TSource>(this IList<TSource> first, IList<TSource> second, SynchronizationContext synchronizationContext)
        {
            var rangeObservableCollection = new SynchronizedRangeObservableCollection<TSource>(synchronizationContext, first.Concat(second), false);
            var rangeObservableCollectionAccess = new AsyncLock();

            async void firstNotifyCollectionChangedEventHandler(object sender, NotifyCollectionChangedEventArgs e)
            {
                using (await rangeObservableCollectionAccess.LockAsync().ConfigureAwait(false))
                {
                    if (e.Action == NotifyCollectionChangedAction.Reset)
                    {
                        await rangeObservableCollection.RemoveRangeAsync(0, (await rangeObservableCollection.CountAsync.ConfigureAwait(false)) - second.Count).ConfigureAwait(false);
                        await rangeObservableCollection.InsertRangeAsync(0, first).ConfigureAwait(false);
                    }
                    else
                    {
                        if (e.OldItems != null && e.OldStartingIndex >= 0)
                            await rangeObservableCollection.RemoveRangeAsync(e.OldStartingIndex, e.OldItems.Count).ConfigureAwait(false);
                        if (e.NewItems != null && e.NewStartingIndex >= 0)
                            await rangeObservableCollection.InsertRangeAsync(e.NewStartingIndex, e.NewItems.Cast<TSource>()).ConfigureAwait(false);
                    }
                }
            }

            async void secondNotifyCollectionChangedEventHandler(object sender, NotifyCollectionChangedEventArgs e)
            {
                using (await rangeObservableCollectionAccess.LockAsync().ConfigureAwait(false))
                {
                    if (e.Action == NotifyCollectionChangedAction.Reset)
                    {
                        await rangeObservableCollection.RemoveRangeAsync(first.Count, (await rangeObservableCollection.CountAsync.ConfigureAwait(false)) - first.Count).ConfigureAwait(false);
                        await rangeObservableCollection.AddRangeAsync(second).ConfigureAwait(false);
                    }
                    else
                    {
                        if (e.OldItems != null && e.OldStartingIndex >= 0)
                            await rangeObservableCollection.RemoveRangeAsync(first.Count + e.OldStartingIndex, e.OldItems.Count).ConfigureAwait(false);
                        if (e.NewItems != null && e.NewStartingIndex >= 0)
                            await rangeObservableCollection.InsertRangeAsync(first.Count + e.NewStartingIndex, e.NewItems.Cast<TSource>()).ConfigureAwait(false);
                    }
                }
            }

            if (first is INotifyCollectionChanged)
                ((INotifyCollectionChanged)first).CollectionChanged += firstNotifyCollectionChangedEventHandler;
            if (second is INotifyCollectionChanged)
                ((INotifyCollectionChanged)second).CollectionChanged += secondNotifyCollectionChangedEventHandler;
            var result = new ActiveEnumerable<TSource>(rangeObservableCollection, disposing =>
            {
                if (first is INotifyCollectionChanged)
                    ((INotifyCollectionChanged)first).CollectionChanged -= firstNotifyCollectionChangedEventHandler;
                if (second is INotifyCollectionChanged)
                    ((INotifyCollectionChanged)second).CollectionChanged -= secondNotifyCollectionChangedEventHandler;
            });
            if (synchronizationContext != null)
                rangeObservableCollection.IsSynchronized = true;
            return result;
        }

        public static ActiveEnumerable<TSource> ActiveDistinct<TSource>(this IList<TSource> source) =>
            ActiveDistinct(source, (source as IsSynchronizable)?.SynchronizationContext);

        public static ActiveEnumerable<TSource> ActiveDistinct<TSource>(this IList<TSource> source, SynchronizationContext synchronizationContext)
        {
            var rangeObservableCollection = new SynchronizedRangeObservableCollection<TSource>(synchronizationContext, false);
            var rangeObservableCollectionAccess = new AsyncLock();
            var distinctCounts = new Dictionary<TSource, int>();
            foreach (var element in source)
            {
                if (distinctCounts.TryGetValue(element, out int distinctCount))
                    distinctCounts[element] = ++distinctCount;
                else
                {
                    distinctCounts.Add(element, 1);
                    rangeObservableCollection.Add(element);
                }
            }

            async void collectionChangedHandler(object sender, NotifyCollectionChangedEventArgs e)
            {
                using (await rangeObservableCollectionAccess.LockAsync().ConfigureAwait(false))
                {
                    if (e.Action == NotifyCollectionChangedAction.Reset)
                    {
                        distinctCounts.Clear();
                        await rangeObservableCollection.ClearAsync().ConfigureAwait(false);
                    }
                    else if (e.Action != NotifyCollectionChangedAction.Move)
                    {
                        if (e.OldItems != null && e.OldStartingIndex >= 0)
                            foreach (TSource oldItem in e.OldItems)
                            {
                                if (--distinctCounts[oldItem] == 0)
                                {
                                    distinctCounts.Remove(oldItem);
                                    await rangeObservableCollection.RemoveAsync(oldItem).ConfigureAwait(false);
                                }
                            }
                        if (e.NewItems != null && e.NewStartingIndex >= 0)
                            foreach (TSource newItem in e.NewItems)
                            {
                                if (distinctCounts.TryGetValue(newItem, out int distinctCount))
                                    distinctCounts[newItem] = ++distinctCount;
                                else
                                {
                                    distinctCounts.Add(newItem, 1);
                                    await rangeObservableCollection.AddAsync(newItem).ConfigureAwait(false);
                                }
                            }
                    }
                }
            }

            if (source is INotifyCollectionChanged)
                ((INotifyCollectionChanged)source).CollectionChanged += collectionChangedHandler;
            var result = new ActiveEnumerable<TSource>(rangeObservableCollection, disposing =>
            {
                if (source is INotifyCollectionChanged)
                    ((INotifyCollectionChanged)source).CollectionChanged -= collectionChangedHandler;
            });
            if (synchronizationContext != null)
                rangeObservableCollection.IsSynchronized = true;
            return result;
        }

        public static ActiveEnumerable<ActiveGrouping<TKey, TSource>> ActiveGroupBy<TKey, TSource>(this IList<TSource> source, Func<TSource, TKey> keySelector, params string[] keySelectorProperties) where TKey : IEquatable<TKey> where TSource : class =>
            ActiveGroupBy(source, (source as IsSynchronizable)?.SynchronizationContext, keySelector, keySelectorProperties);

        public static ActiveEnumerable<ActiveGrouping<TKey, TSource>> ActiveGroupBy<TKey, TSource>(this IList<TSource> source, SynchronizationContext synchronizationContext, Func<TSource, TKey> keySelector, params string[] keySelectorProperties) where TKey : IEquatable<TKey> where TSource : class
        {
            var rangeObservableCollection = new SynchronizedRangeObservableCollection<ActiveGrouping<TKey, TSource>>(synchronizationContext, false);
            var rangeObservableCollectionAccess = new AsyncLock();
            var collectionAndGroupingDictionary = new Dictionary<TKey, (SynchronizedRangeObservableCollection<TSource> groupingObservableCollection, ActiveGrouping<TKey, TSource> grouping)>();
            var keyDictionary = new Dictionary<TSource, TKey>();

            var monitor = ActiveCollectionMonitor<TSource>.Monitor(source, keySelectorProperties);

            void addElement(TSource element)
            {
                var key = keySelector(element);
                if (!monitor.ElementsNotifyChanging)
                    keyDictionary.Add(element, key);
                SynchronizedRangeObservableCollection<TSource> groupingObservableCollection;
                if (!collectionAndGroupingDictionary.TryGetValue(key, out (SynchronizedRangeObservableCollection<TSource> groupingObservableCollection, ActiveGrouping<TKey, TSource> grouping) collectionAndGrouping))
                {
                    groupingObservableCollection = new SynchronizedRangeObservableCollection<TSource>(synchronizationContext);
                    var grouping = new ActiveGrouping<TKey, TSource>(key, groupingObservableCollection);
                    collectionAndGrouping = (groupingObservableCollection, grouping);
                    rangeObservableCollection.Add(grouping);
                }
                else
                    groupingObservableCollection = collectionAndGrouping.groupingObservableCollection;
                groupingObservableCollection.Add(element);
            }

            foreach (var element in source)
                addElement(element);

            async void elementPropertyChangedHandler(object sender, ElementPropertyChangeEventArgs<TSource> e)
            {
                using (await rangeObservableCollectionAccess.LockAsync().ConfigureAwait(false))
                {
                    var element = e.Element;
                    var oldKey = keyDictionary[element];
                    var newKey = keySelector(element);
                    if (!oldKey.Equals(newKey))
                    {
                        if (!monitor.ElementsNotifyChanging)
                            keyDictionary[element] = newKey;
                        var oldCollectionAndGrouping = collectionAndGroupingDictionary[oldKey];
                        await oldCollectionAndGrouping.groupingObservableCollection.RemoveAsync(element).ConfigureAwait(false);
                        if ((await oldCollectionAndGrouping.groupingObservableCollection.CountAsync.ConfigureAwait(false)) == 0)
                            collectionAndGroupingDictionary.Remove(oldKey);
                        SynchronizedRangeObservableCollection<TSource> groupingObservableCollection;
                        if (!collectionAndGroupingDictionary.TryGetValue(newKey, out (SynchronizedRangeObservableCollection<TSource> groupingObservableCollection, ActiveGrouping<TKey, TSource> grouping) collectionAndGrouping))
                        {
                            groupingObservableCollection = new SynchronizedRangeObservableCollection<TSource>(synchronizationContext);
                            var grouping = new ActiveGrouping<TKey, TSource>(newKey, groupingObservableCollection);
                            collectionAndGrouping = (groupingObservableCollection, grouping);
                            await rangeObservableCollection.AddAsync(grouping).ConfigureAwait(false);
                        }
                        else
                            groupingObservableCollection = collectionAndGrouping.groupingObservableCollection;
                        await groupingObservableCollection.AddAsync(element).ConfigureAwait(false);
                    }
                    if (monitor.ElementsNotifyChanging)
                        keyDictionary.Remove(element);
                }
            }

            void elementPropertyChangingHandler(object sender, ElementPropertyChangeEventArgs<TSource> e)
            {
                var element = e.Element;
                var key = keySelector(element);
                if (keyDictionary.ContainsKey(element))
                    keyDictionary[element] = key;
                else
                    keyDictionary.Add(element, key);
            }

            async void elementsAddedHandler(object sender, ElementMembershipEventArgs<TSource> e)
            {
                using (await rangeObservableCollectionAccess.LockAsync().ConfigureAwait(false))
                {
                    foreach (var element in e.Elements)
                        addElement(element);
                }
            }

            async void elementsRemovedHandler(object sender, ElementMembershipEventArgs<TSource> e)
            {
                using (await rangeObservableCollectionAccess.LockAsync().ConfigureAwait(false))
                    foreach (var element in e.Elements)
                    {
                        var key = keyDictionary[element];
                        keyDictionary.Remove(element);
                        var (groupingObservableCollection, grouping) = collectionAndGroupingDictionary[key];
                        await groupingObservableCollection.RemoveAsync(element).ConfigureAwait(false);
                        if ((await groupingObservableCollection.CountAsync.ConfigureAwait(false)) == 0)
                            collectionAndGroupingDictionary.Remove(key);
                    }
            }

            monitor.ElementPropertyChanged += elementPropertyChangedHandler;
            monitor.ElementPropertyChanging += elementPropertyChangingHandler;
            monitor.ElementsAdded += elementsAddedHandler;
            monitor.ElementsRemoved += elementsRemovedHandler;
            var result = new ActiveEnumerable<ActiveGrouping<TKey, TSource>>(rangeObservableCollection, disposing =>
            {
                monitor.ElementPropertyChanged -= elementPropertyChangedHandler;
                monitor.ElementsAdded -= elementsAddedHandler;
                monitor.ElementsRemoved -= elementsRemovedHandler;
                if (disposing)
                    monitor.Dispose();
            });
            if (synchronizationContext != null)
                rangeObservableCollection.IsSynchronized = true;
            return result;
        }

        public static ActiveEnumerable<TSource> ActiveGroupBy<TSource>(this IList<TSource> source, Func<TSource, IComparable> ascendingOrderSelector, IEnumerable<string> ascendingSelectorProperties = null, bool indexed = false) where TSource : class =>
            ActiveGroupBy(source, (source as IsSynchronizable)?.SynchronizationContext, ascendingOrderSelector, ascendingSelectorProperties, indexed);

        public static ActiveEnumerable<TSource> ActiveGroupBy<TSource>(this IList<TSource> source, SynchronizationContext synchronizationContext, Func<TSource, IComparable> ascendingOrderSelector, IEnumerable<string> ascendingSelectorProperties = null, bool indexed = false) where TSource : class =>
            ActiveGroupBy(source, synchronizationContext, new Func<TSource, IComparable>[] { ascendingOrderSelector }, ascendingSelectorProperties, indexed);

        public static ActiveEnumerable<TSource> ActiveGroupBy<TSource>(this IList<TSource> source, IEnumerable<Func<TSource, IComparable>> ascendingOrderSelectors, IEnumerable<string> ascendingSelectorsProperties = null, bool indexed = false) where TSource : class =>
            ActiveGroupBy(source, (source as IsSynchronizable)?.SynchronizationContext, ascendingOrderSelectors, ascendingSelectorsProperties, indexed);

        public static ActiveEnumerable<TSource> ActiveGroupBy<TSource>(this IList<TSource> source, SynchronizationContext synchronizationContext, IEnumerable<Func<TSource, IComparable>> ascendingOrderSelectors, IEnumerable<string> ascendingSelectorsProperties = null, bool indexed = false) where TSource : class =>
            ActiveGroupBy(source, synchronizationContext, ascendingOrderSelectors.Select(aos => new ActiveOrderingDescriptor<TSource>(aos, false)), ascendingSelectorsProperties, indexed);

        public static ActiveEnumerable<TSource> ActiveGroupBy<TSource>(this IList<TSource> source, ActiveOrderingDescriptor<TSource> orderingDescriptor, IEnumerable<string> selectorProperties = null, bool indexed = false) where TSource : class =>
            ActiveGroupBy(source, (source as IsSynchronizable)?.SynchronizationContext, orderingDescriptor, selectorProperties, indexed);

        public static ActiveEnumerable<TSource> ActiveGroupBy<TSource>(this IList<TSource> source, SynchronizationContext synchronizationContext, ActiveOrderingDescriptor<TSource> orderingDescriptor, IEnumerable<string> selectorProperties = null, bool indexed = false) where TSource : class =>
            ActiveGroupBy(source, synchronizationContext, new ActiveOrderingDescriptor<TSource>[] { orderingDescriptor }, selectorProperties, indexed);

        public static ActiveEnumerable<TSource> ActiveGroupBy<TSource>(this IList<TSource> source, IEnumerable<ActiveOrderingDescriptor<TSource>> orderingDescriptors, IEnumerable<string> selectorsProperties = null, bool indexed = false) where TSource : class =>
            ActiveGroupBy(source, (source as IsSynchronizable)?.SynchronizationContext, orderingDescriptors, selectorsProperties, indexed);

        public static ActiveEnumerable<TSource> ActiveGroupBy<TSource>(this IList<TSource> source, SynchronizationContext synchronizationContext, IEnumerable<ActiveOrderingDescriptor<TSource>> orderingDescriptors, IEnumerable<string> selectorsProperties = null, bool indexed = false) where TSource : class
        {
            var comparer = new ActiveOrderingComparer<TSource>(orderingDescriptors);
            var sortedSource = source.ToList();
            sortedSource.Sort(comparer);
            Dictionary<TSource, int> sortingIndicies = null;

            void rebuildSortingIndicies(IList<TSource> fromSort)
            {
                sortingIndicies = new Dictionary<TSource, int>();
                for (var i = 0; i < fromSort.Count; ++i)
                    sortingIndicies.Add(fromSort[i], i);
            }

            if (indexed)
                rebuildSortingIndicies(sortedSource);
            var rangeObservableCollection = new SynchronizedRangeObservableCollection<TSource>(synchronizationContext, sortedSource, false);
            var rangeObservableCollectionAccess = new AsyncLock();

            void repositionElement(TSource element)
            {
                var startingPosition = indexed ? sortingIndicies[element] : rangeObservableCollection.IndexOf(element);
                var position = startingPosition;
                if (indexed)
                {
                    while (position > 0 && comparer.Compare(element, rangeObservableCollection[position - 1]) < 0)
                        ++sortingIndicies[rangeObservableCollection[--position]];
                    while (position < rangeObservableCollection.Count - 1 && comparer.Compare(element, rangeObservableCollection[position + 1]) > 0)
                        --sortingIndicies[rangeObservableCollection[++position]];
                }
                else
                {
                    while (position > 0 && comparer.Compare(element, rangeObservableCollection[position - 1]) < 0)
                        --position;
                    while (position < rangeObservableCollection.Count - 1 && comparer.Compare(element, rangeObservableCollection[position + 1]) > 0)
                        ++position;
                }
                if (startingPosition != position)
                {
                    if (indexed)
                        sortingIndicies[element] = position;
                    rangeObservableCollection.Move(startingPosition, position);
                }
            }

            var monitor = ActiveCollectionMonitor<TSource>.Monitor(source, selectorsProperties == null ? new string[0] : selectorsProperties.ToArray());

            async void elementPropertyChangedHandler(object sender, ElementPropertyChangeEventArgs<TSource> e)
            {
                using (await rangeObservableCollectionAccess.LockAsync().ConfigureAwait(false))
                    repositionElement(e.Element);
            }

            async void elementsAddedHandler(object sender, ElementMembershipEventArgs<TSource> e)
            {
                using (await rangeObservableCollectionAccess.LockAsync().ConfigureAwait(false))
                {
                    if ((await rangeObservableCollection.CountAsync.ConfigureAwait(false)) == 0)
                    {
                        var sorted = e.Elements.ToList();
                        sorted.Sort(comparer);
                        if (indexed)
                            rebuildSortingIndicies(sorted);
                        await rangeObservableCollection.ResetAsync(sorted).ConfigureAwait(false);
                    }
                    else
                        foreach (var element in e.Elements)
                        {
                            var position = 0;
                            while (position < (await rangeObservableCollection.CountAsync.ConfigureAwait(false)) && comparer.Compare(element, await rangeObservableCollection.GetItemAsync(position).ConfigureAwait(false)) >= 0)
                                ++position;
                            var insertionPosition = position;
                            if (indexed)
                            {
                                while (position < await rangeObservableCollection.CountAsync.ConfigureAwait(false))
                                {
                                    sortingIndicies[await rangeObservableCollection.GetItemAsync(position).ConfigureAwait(false)] = position + 1;
                                    ++position;
                                }
                                sortingIndicies.Add(element, insertionPosition);
                            }
                            await rangeObservableCollection.InsertAsync(insertionPosition, element).ConfigureAwait(false);
                        }
                }
            }

            async void elementsRemovedHandler(object sender, ElementMembershipEventArgs<TSource> e)
            {
                using (await rangeObservableCollectionAccess.LockAsync().ConfigureAwait(false))
                {
                    if ((await rangeObservableCollection.CountAsync.ConfigureAwait(false)) == e.Count)
                    {
                        if (indexed)
                            sortingIndicies = new Dictionary<TSource, int>();
                        await rangeObservableCollection.ResetAsync(new TSource[0]).ConfigureAwait(false);
                    }
                    else
                    {
                        var elementsRemovedByIndex = new List<(TSource element, int index)>();
                        if (indexed)
                            foreach (var element in e.Elements)
                                elementsRemovedByIndex.Add((element, sortingIndicies.TryGetValue(element, out int index) ? index : -1));
                        else
                            foreach (var element in e.Elements)
                                elementsRemovedByIndex.Add((element, await rangeObservableCollection.IndexOfAsync(element).ConfigureAwait(false)));
                        var elementsRemovedByIndexSorted = elementsRemovedByIndex.Where(ie => ie.index >= 0).OrderByDescending(ie => ie.index).ToList();
                        if (indexed)
                            foreach (var (element, index) in elementsRemovedByIndexSorted)
                            {
                                await rangeObservableCollection.RemoveAtAsync(index).ConfigureAwait(false);
                                sortingIndicies.Remove(element);
                            }
                        else
                            foreach (var (element, index) in elementsRemovedByIndexSorted)
                                await rangeObservableCollection.RemoveAtAsync(index).ConfigureAwait(false);
                        if (indexed && elementsRemovedByIndexSorted.Any())
                        {
                            for (int i = elementsRemovedByIndexSorted.Last().index, ii = await rangeObservableCollection.CountAsync.ConfigureAwait(false); i < ii; ++i)
                                sortingIndicies[await rangeObservableCollection.GetItemAsync(i).ConfigureAwait(false)] = i;
                        }
                    }
                }
            }

            monitor.ElementPropertyChanged += elementPropertyChangedHandler;
            monitor.ElementsAdded += elementsAddedHandler;
            monitor.ElementsRemoved += elementsRemovedHandler;
            var result = new ActiveEnumerable<TSource>(rangeObservableCollection, disposing =>
            {
                monitor.ElementPropertyChanged -= elementPropertyChangedHandler;
                monitor.ElementsAdded -= elementsAddedHandler;
                monitor.ElementsRemoved -= elementsRemovedHandler;
                if (disposing)
                    monitor.Dispose();
            });
            if (synchronizationContext != null)
                rangeObservableCollection.IsSynchronized = true;
            return result;
        }

        public static ActiveEnumerable<TResult> ActiveSelect<TSource, TResult>(this IList<TSource> source, Func<TSource, TResult> selector, Action<TResult> releaser = null, Action<TSource, string, TResult> updater = null, bool indexed = false, params string[] selectorProperties) where TSource : class =>
            ActiveSelect(source, (source as IsSynchronizable)?.SynchronizationContext, selector, releaser, updater, indexed, selectorProperties);

        public static ActiveEnumerable<TResult> ActiveSelect<TSource, TResult>(this IList<TSource> source, SynchronizationContext synchronizationContext, Func<TSource, TResult> selector, Action<TResult> releaser = null, Action<TSource, string, TResult> updater = null, bool indexed = false, params string[] selectorProperties) where TSource : class
        {
            var sourceToIndex = indexed ? new Dictionary<TSource, int>() : null;
            var rangeObservableCollection = new SynchronizedRangeObservableCollection<TResult>(synchronizationContext, indexed ? source.Select((element, index) =>
            {
                sourceToIndex.Add(element, index);
                return selector(element);
            }) : source.Select(element => selector(element)), false);
            var rangeObservableCollectionAccess = new AsyncLock();
            var monitor = ActiveCollectionMonitor<TSource>.Monitor(source, selectorProperties);

            async void elementPropertyChangedHandler(object sender, ElementPropertyChangeEventArgs<TSource> e)
            {
                using (await rangeObservableCollectionAccess.LockAsync().ConfigureAwait(false))
                {
                    var sourceElement = e.Element;
                    var index = indexed ? sourceToIndex[sourceElement] : source.IndexOf(sourceElement);
                    if (updater == null)
                    {
                        var replacedElement = await rangeObservableCollection.GetItemAsync(index).ConfigureAwait(false);
                        await rangeObservableCollection.SetItemAsync(index, selector(sourceElement)).ConfigureAwait(false);
                        releaser?.Invoke(replacedElement);
                    }
                    else
                        updater(source[index], e.PropertyName, await rangeObservableCollection.GetItemAsync(index).ConfigureAwait(false));
                }
            }

            async void elementsAddedHandler(object sender, ElementMembershipEventArgs<TSource> e)
            {
                using (await rangeObservableCollectionAccess.LockAsync().ConfigureAwait(false))
                {
                    if (indexed)
                    {
                        var index = e.Index - 1;
                        foreach (var element in e.Elements)
                            sourceToIndex.Add(element, ++index);
                        for (var i = e.Index + e.Count; i < source.Count; ++i)
                            sourceToIndex[source[i]] = i;
                    }
                    await rangeObservableCollection.InsertRangeAsync(e.Index, e.Elements.Select(selector)).ConfigureAwait(false);
                }
            }

            async void elementsMovedHandler(object sender, ElementsMovedEventArgs<TSource> e)
            {
                using (await rangeObservableCollectionAccess.LockAsync().ConfigureAwait(false))
                {
                    if (indexed)
                    {
                        var endIndex = (e.FromIndex > e.ToIndex ? e.FromIndex : e.ToIndex) + e.Count;
                        for (var i = e.FromIndex < e.ToIndex ? e.FromIndex : e.ToIndex; i < endIndex; ++i)
                            sourceToIndex[source[i]] = i;
                    }
                    await rangeObservableCollection.MoveRangeAsync(e.FromIndex, e.ToIndex, e.Count).ConfigureAwait(false);
                }
            }

            async void elementsRemovedHandler(object sender, ElementMembershipEventArgs<TSource> e)
            {
                using (await rangeObservableCollectionAccess.LockAsync().ConfigureAwait(false))
                {
                    var removedItems = new List<TResult>(await rangeObservableCollection.GetRangeAsync(e.Index, e.Count).ConfigureAwait(false));
                    if (indexed)
                        for (var i = e.Index; i < source.Count; ++i)
                            sourceToIndex[source[i]] = i;
                    await rangeObservableCollection.RemoveRangeAsync(e.Index, e.Count).ConfigureAwait(false);
                    if (releaser != null)
                        foreach (var removedItem in removedItems)
                            releaser(removedItem);
                }
            }

            monitor.ElementPropertyChanged += elementPropertyChangedHandler;
            monitor.ElementsAdded += elementsAddedHandler;
            monitor.ElementsMoved += elementsMovedHandler;
            monitor.ElementsRemoved += elementsRemovedHandler;
            var result = new ActiveEnumerable<TResult>(rangeObservableCollection, disposing =>
            {
                monitor.ElementPropertyChanged -= elementPropertyChangedHandler;
                monitor.ElementsAdded -= elementsAddedHandler;
                monitor.ElementsMoved -= elementsMovedHandler;
                monitor.ElementsRemoved -= elementsRemovedHandler;
                if (disposing)
                    monitor.Dispose();
            });
            if (synchronizationContext != null)
                rangeObservableCollection.IsSynchronized = true;
            return result;
        }

        public static ActiveEnumerable<TResult> ActiveSelectMany<TSource, TResult>(this IList<TSource> source, Func<TSource, IEnumerable<TResult>> selector, Action<TResult> releaser = null, Action<TSource, string, IList<TResult>> updater = null, params string[] selectorProperties) where TSource : class =>
            ActiveSelectMany(source, (source as IsSynchronizable)?.SynchronizationContext, selector, releaser, updater, selectorProperties);

        public static ActiveEnumerable<TResult> ActiveSelectMany<TSource, TResult>(this IList<TSource> source, SynchronizationContext synchronizationContext, Func<TSource, IEnumerable<TResult>> selector, Action<TResult> releaser = null, Action<TSource, string, IList<TResult>> updater = null, params string[] selectorProperties) where TSource : class
        {
            var sourceToSourceIndex = new Dictionary<TSource, int>();
            var sourceToResultsIndex = new Dictionary<TSource, int>();
            var selectedResults = new Dictionary<TSource, IList<TResult>>();
            var resultsCount = 0;
            var rangeObservableCollection = new SynchronizedRangeObservableCollection<TResult>(synchronizationContext, source.SelectMany((element, index) =>
            {
                var results = selector(element);
                var resultsList = new List<TResult>(results);
                sourceToSourceIndex.Add(element, index);
                sourceToResultsIndex.Add(element, resultsCount);
                selectedResults.Add(element, resultsList);
                resultsCount += resultsList.Count;
                return results;
            }), false);
            var rangeObservableCollectionAccess = new AsyncLock();
            var monitor = ActiveCollectionMonitor<TSource>.Monitor(source, selectorProperties);

            async void elementPropertyChangedHandler(object sender, ElementPropertyChangeEventArgs<TSource> e)
            {
                using (await rangeObservableCollectionAccess.LockAsync().ConfigureAwait(false))
                {
                    var sourceElement = e.Element;
                    if (!sourceToSourceIndex.TryGetValue(sourceElement, out int sourceIndex))
                        return;
                    var pastResultsList = selectedResults[sourceElement];
                    if (updater == null)
                    {
                        var newResults = selector(sourceElement);
                        var newResultsList = new List<TResult>(newResults);
                        selectedResults[sourceElement] = newResultsList;
                        var lengthDiff = newResultsList.Count - pastResultsList.Count;
                        if (lengthDiff != 0)
                            for (var i = sourceIndex + 1; i < source.Count; ++i)
                                sourceToResultsIndex[source[i]] += lengthDiff;
                        await rangeObservableCollection.ReplaceRangeAsync(sourceToResultsIndex[sourceElement], pastResultsList.Count, newResults).ConfigureAwait(false);
                    }
                    else
                    {
                        var pastResults = pastResultsList.ToList();
                        updater(sourceElement, e.PropertyName, pastResultsList);
                        if (!pastResults.SequenceEqual(pastResultsList))
                        {
                            var lengthDiff = pastResultsList.Count - pastResults.Count;
                            if (lengthDiff != 0)
                                for (var i = sourceIndex + 1; i < source.Count; ++i)
                                    sourceToResultsIndex[source[i]] += lengthDiff;
                            await rangeObservableCollection.ReplaceRangeAsync(sourceToResultsIndex[sourceElement], pastResults.Count, pastResultsList).ConfigureAwait(false);
                            if (releaser != null)
                                foreach (var removedItem in pastResults.Except(pastResultsList))
                                    releaser(removedItem);
                        }
                    }
                }
            }

            async void elementsAddedHandler(object sender, ElementMembershipEventArgs<TSource> e)
            {
                using (await rangeObservableCollectionAccess.LockAsync().ConfigureAwait(false))
                {
                    var resultsIndex = 0;
                    if (sourceToResultsIndex.Count > 0)
                    {
                        if (e.Index == sourceToResultsIndex.Count)
                            resultsIndex = await rangeObservableCollection.CountAsync.ConfigureAwait(false);
                        else
                            resultsIndex = sourceToResultsIndex[source[e.Index + e.Count]];
                    }
                    var iteratingResultsIndex = resultsIndex;
                    var resultsAdded = new List<TResult>(e.Elements.SelectMany((element, index) =>
                    {
                        var results = selector(element);
                        var resultsList = new List<TResult>(results);
                        sourceToSourceIndex.Add(element, e.Index + index);
                        sourceToResultsIndex.Add(element, resultsIndex);
                        selectedResults.Add(element, resultsList);
                        iteratingResultsIndex += resultsList.Count;
                        return results;
                    }));
                    for (var i = e.Index + e.Count; i < source.Count; ++i)
                    {
                        sourceToSourceIndex[source[i]] = i;
                        sourceToResultsIndex[source[i]] += resultsAdded.Count;
                    }
                    await rangeObservableCollection.InsertRangeAsync(resultsIndex, resultsAdded).ConfigureAwait(false);
                }
            }

            async void elementsMovedHandler(object sender, ElementsMovedEventArgs<TSource> e)
            {
                using (await rangeObservableCollectionAccess.LockAsync().ConfigureAwait(false))
                {
                    var firstElement = e.Elements.First();
                    var resultFromIndex = sourceToResultsIndex[firstElement];
                    var sourceStartIndex = e.FromIndex < e.ToIndex ? e.FromIndex : e.ToIndex;
                    var sourceEndIndex = (e.FromIndex > e.ToIndex ? e.FromIndex : e.ToIndex) + e.Count;
                    var resultStartIndex = 0;
                    if (sourceStartIndex > 0)
                    {
                        var prefixedSourceElement = source[sourceStartIndex - 1];
                        resultStartIndex = sourceToResultsIndex[prefixedSourceElement] + selectedResults[prefixedSourceElement].Count;
                    }
                    var iterativeResultIndex = resultStartIndex;
                    for (var i = sourceStartIndex; i < sourceEndIndex; ++i)
                    {
                        var sourceElement = source[i];
                        sourceToSourceIndex[sourceElement] = i;
                        sourceToResultsIndex[sourceElement] = iterativeResultIndex;
                        var results = selectedResults[sourceElement];
                        iterativeResultIndex += results.Count;
                    }
                    var resultToIndex = sourceToResultsIndex[firstElement];
                    await rangeObservableCollection.MoveRangeAsync(resultFromIndex, resultToIndex, e.Elements.Sum(element => selectedResults[element].Count)).ConfigureAwait(false);
                }
            }

            async void elementsRemovedHandler(object sender, ElementMembershipEventArgs<TSource> e)
            {
                using (await rangeObservableCollectionAccess.LockAsync().ConfigureAwait(false))
                {
                    var removedItems = new List<TResult>();
                    foreach (var sourceElement in e.Elements)
                    {
                        removedItems.AddRange(selectedResults[sourceElement]);
                        sourceToSourceIndex.Remove(sourceElement);
                        sourceToResultsIndex.Remove(sourceElement);
                        selectedResults.Remove(sourceElement);
                    }
                    var resultIndex = 0;
                    if (e.Index > 0)
                        resultIndex = sourceToResultsIndex[source[e.Index - 1]];
                    var iteratingResultIndex = resultIndex;
                    for (var i = e.Index; i < source.Count; ++i)
                    {
                        var sourceElement = source[i];
                        sourceToSourceIndex[sourceElement] = i;
                        sourceToResultsIndex[sourceElement] = iteratingResultIndex;
                        iteratingResultIndex += selectedResults[sourceElement].Count;
                    }
                    await rangeObservableCollection.RemoveRangeAsync(resultIndex, removedItems.Count).ConfigureAwait(false);
                    if (releaser != null)
                        foreach (var removedItem in removedItems)
                            releaser(removedItem);
                }
            }

            monitor.ElementPropertyChanged += elementPropertyChangedHandler;
            monitor.ElementsAdded += elementsAddedHandler;
            monitor.ElementsMoved += elementsMovedHandler;
            monitor.ElementsRemoved += elementsRemovedHandler;
            var result = new ActiveEnumerable<TResult>(rangeObservableCollection, disposing =>
            {
                monitor.ElementPropertyChanged -= elementPropertyChangedHandler;
                monitor.ElementsAdded -= elementsAddedHandler;
                monitor.ElementsMoved -= elementsMovedHandler;
                monitor.ElementsRemoved -= elementsRemovedHandler;
                if (disposing)
                    monitor.Dispose();
            });
            if (synchronizationContext != null)
                rangeObservableCollection.IsSynchronized = true;
            return result;
        }

        public static ActiveEnumerable<TSource> ToActiveEnumerable<TSource>(this IList<TSource> source) => new ActiveEnumerable<TSource>(source);

        public static ActiveEnumerable<TSource> LiveWhere<TSource>(this IList<TSource> source, Func<TSource, bool> predicate, params string[] predicateProperties) where TSource : class =>
            LiveWhere(source, (source as IsSynchronizable)?.SynchronizationContext, predicate, predicateProperties);

        public static ActiveEnumerable<TSource> LiveWhere<TSource>(this IList<TSource> source, SynchronizationContext synchronizationContext, Func<TSource, bool> predicate, params string[] predicateProperties) where TSource : class
        {
            var rangeObservableCollection = new SynchronizedRangeObservableCollection<TSource>(synchronizationContext, source.Where(predicate));
            var rangeObservableCollectionAccess = new AsyncLock();
            var monitor = ActiveCollectionMonitor<TSource>.Monitor(source, predicateProperties);
            HashSet<TSource> currentItems;
            if (monitor.ElementsNotifyChanging)
                currentItems = new HashSet<TSource>();
            else
                currentItems = new HashSet<TSource>(rangeObservableCollection);

            async void elementPropertyChangedHandler(object sender, ElementPropertyChangeEventArgs<TSource> e)
            {
                using (await rangeObservableCollectionAccess.LockAsync().ConfigureAwait(false))
                {
                    var element = e.Element;
                    if (predicate(element))
                    {
                        if (currentItems.Add(element))
                        {
                            await rangeObservableCollection.AddAsync(element).ConfigureAwait(false);
                            if (monitor.ElementsNotifyChanging)
                                currentItems.Remove(element);
                        }
                    }
                    else if (currentItems.Remove(element))
                        await rangeObservableCollection.RemoveAsync(element).ConfigureAwait(false);
                }
            }

            async void elementPropertyChangingHandler(object sender, ElementPropertyChangeEventArgs<TSource> e)
            {
                using (await rangeObservableCollectionAccess.LockAsync().ConfigureAwait(false))
                {
                    var element = e.Element;
                    if (predicate(element))
                        currentItems.Add(element);
                }
            }

            async void elementsAddedHandler(object sender, ElementMembershipEventArgs<TSource> e)
            {
                using (await rangeObservableCollectionAccess.LockAsync().ConfigureAwait(false))
                {
                    var matchingElements = e.Elements.Where(predicate);
                    if (!monitor.ElementsNotifyChanging)
                        currentItems.UnionWith(matchingElements);
                    await rangeObservableCollection.AddRangeAsync(matchingElements).ConfigureAwait(false);
                }
            }

            async void elementsRemovedHandler(object sender, ElementMembershipEventArgs<TSource> e)
            {
                using (await rangeObservableCollectionAccess.LockAsync().ConfigureAwait(false))
                {
                    var matchingElements = new HashSet<TSource>(currentItems);
                    matchingElements.IntersectWith(e.Elements);
                    currentItems.ExceptWith(e.Elements);
                    await rangeObservableCollection.RemoveRangeAsync(matchingElements).ConfigureAwait(false);
                }
            }

            monitor.ElementPropertyChanged += elementPropertyChangedHandler;
            monitor.ElementPropertyChanging += elementPropertyChangingHandler;
            monitor.ElementsAdded += elementsAddedHandler;
            monitor.ElementsRemoved += elementsRemovedHandler;
            var result = new ActiveEnumerable<TSource>(rangeObservableCollection, disposing =>
            {
                monitor.ElementPropertyChanged -= elementPropertyChangedHandler;
                monitor.ElementPropertyChanging -= elementPropertyChangingHandler;
                monitor.ElementsAdded -= elementsAddedHandler;
                monitor.ElementsRemoved -= elementsRemovedHandler;
                if (disposing)
                    monitor.Dispose();
            });
            if (synchronizationContext != null)
                rangeObservableCollection.IsSynchronized = true;
            return result;
        }
    }
}
