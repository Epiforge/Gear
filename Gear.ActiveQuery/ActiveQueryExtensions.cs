using Gear.Components;
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
            ActiveCast<TResult>(source, (source as ISynchronizable)?.SynchronizationContext);

        public static ActiveEnumerable<TResult> ActiveCast<TResult>(this IEnumerable source, SynchronizationContext synchronizationContext)
        {
            var rangeObservableCollection = new SynchronizedRangeObservableCollection<TResult>(synchronizationContext, source.Cast<TResult>(), false);
            var rangeObservableCollectionAccess = new object();

            void notifyCollectionChangedEventHandler(object sender, NotifyCollectionChangedEventArgs e)
            {
                lock (rangeObservableCollectionAccess)
                {
                    if (e.Action == NotifyCollectionChangedAction.Reset)
                        rangeObservableCollection.ReplaceAll(source.Cast<TResult>());
                    else if (e.Action == NotifyCollectionChangedAction.Move)
                        rangeObservableCollection.MoveRange(e.OldStartingIndex, e.NewStartingIndex, e.OldItems.Count);
                    else
                    {
                        if (e.OldItems != null && e.NewItems != null && e.OldStartingIndex >= 0 && e.OldStartingIndex == e.NewStartingIndex)
                        {
                            if (e.OldItems.Count == 1 && e.NewItems.Count == 1)
                                rangeObservableCollection.Replace(e.OldStartingIndex, (TResult)e.NewItems[0]);
                            else
                                rangeObservableCollection.ReplaceRange(e.OldStartingIndex, e.OldItems.Count, e.NewItems.Cast<TResult>());
                        }
                        else
                        {
                            if (e.OldItems != null && e.OldStartingIndex >= 0)
                                rangeObservableCollection.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
                            if (e.NewItems != null && e.NewStartingIndex >= 0)
                                rangeObservableCollection.InsertRange(e.NewStartingIndex, e.NewItems.Cast<TResult>());
                        }
                    }
                }
            }

            {
                if (source is INotifyCollectionChanged notifyingSource)
                    notifyingSource.CollectionChanged += notifyCollectionChangedEventHandler;
            }
            var result = new ActiveEnumerable<TResult>(rangeObservableCollection, disposing =>
            {
                if (source is INotifyCollectionChanged notifyingSource)
                    notifyingSource.CollectionChanged -= notifyCollectionChangedEventHandler;
            });
            if (synchronizationContext != null)
                rangeObservableCollection.IsSynchronized = true;
            return result;
        }

        public static ActiveEnumerable<TSource> ActiveConcat<TSource>(this IReadOnlyList<TSource> first, IReadOnlyList<TSource> second) =>
            ActiveConcat(first, second, (first as ISynchronizable)?.SynchronizationContext ?? (second as ISynchronizable)?.SynchronizationContext);

        public static ActiveEnumerable<TSource> ActiveConcat<TSource>(this IReadOnlyList<TSource> first, IReadOnlyList<TSource> second, SynchronizationContext synchronizationContext)
        {
            var rangeObservableCollection = new SynchronizedRangeObservableCollection<TSource>(synchronizationContext, first.Concat(second), false);
            var rangeObservableCollectionAccess = new object();

            void firstNotifyCollectionChangedEventHandler(object sender, NotifyCollectionChangedEventArgs e)
            {
                lock (rangeObservableCollectionAccess)
                {
                    if (e.Action == NotifyCollectionChangedAction.Reset)
                        rangeObservableCollection.ReplaceRange(0, rangeObservableCollection.Count - second.Count, first);
                    else
                    {
                        if (e.OldItems != null && e.NewItems != null && e.OldStartingIndex >= 0 && e.OldStartingIndex == e.NewStartingIndex)
                        {
                            if (e.OldItems.Count == 1 && e.NewItems.Count == 1)
                                rangeObservableCollection.Replace(e.OldStartingIndex, (TSource)e.NewItems[0]);
                            else
                                rangeObservableCollection.ReplaceRange(e.OldStartingIndex, e.OldItems.Count, e.NewItems.Cast<TSource>());
                        }
                        else
                        {
                            if (e.OldItems != null && e.OldStartingIndex >= 0)
                                rangeObservableCollection.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
                            if (e.NewItems != null && e.NewStartingIndex >= 0)
                                rangeObservableCollection.InsertRange(e.NewStartingIndex, e.NewItems.Cast<TSource>());
                        }
                    }
                }
            }

            void secondNotifyCollectionChangedEventHandler(object sender, NotifyCollectionChangedEventArgs e)
            {
                lock (rangeObservableCollectionAccess)
                {
                    if (e.Action == NotifyCollectionChangedAction.Reset)
                        rangeObservableCollection.ReplaceRange(first.Count, rangeObservableCollection.Count - first.Count, second);
                    else
                    {
                        if (e.OldItems != null && e.NewItems != null && e.OldStartingIndex >= 0 && e.OldStartingIndex == e.NewStartingIndex)
                        {
                            if (e.OldItems.Count == 1 && e.NewItems.Count == 1)
                                rangeObservableCollection.Replace(first.Count + e.OldStartingIndex, (TSource)e.NewItems[0]);
                            else
                                rangeObservableCollection.ReplaceRange(first.Count + e.OldStartingIndex, e.OldItems.Count, e.NewItems.Cast<TSource>());
                        }
                        else
                        {
                            if (e.OldItems != null && e.OldStartingIndex >= 0)
                                rangeObservableCollection.RemoveRange(first.Count + e.OldStartingIndex, e.OldItems.Count);
                            if (e.NewItems != null && e.NewStartingIndex >= 0)
                                rangeObservableCollection.InsertRange(first.Count + e.NewStartingIndex, e.NewItems.Cast<TSource>());
                        }
                    }
                }
            }

            {
                if (first is INotifyCollectionChanged notifyingFirst)
                    notifyingFirst.CollectionChanged += firstNotifyCollectionChangedEventHandler;
                if (second is INotifyCollectionChanged notifyingSecond)
                    notifyingSecond.CollectionChanged += secondNotifyCollectionChangedEventHandler;
            }
            var result = new ActiveEnumerable<TSource>(rangeObservableCollection, disposing =>
            {
                if (first is INotifyCollectionChanged notifyingFirst)
                    notifyingFirst.CollectionChanged -= firstNotifyCollectionChangedEventHandler;
                if (second is INotifyCollectionChanged notifyingSecond)
                    notifyingSecond.CollectionChanged -= secondNotifyCollectionChangedEventHandler;
            });
            if (synchronizationContext != null)
                rangeObservableCollection.IsSynchronized = true;
            return result;
        }

        public static ActiveEnumerable<TSource> ActiveDistinct<TSource>(this IReadOnlyList<TSource> source) =>
            ActiveDistinct(source, (source as ISynchronizable)?.SynchronizationContext);

        public static ActiveEnumerable<TSource> ActiveDistinct<TSource>(this IReadOnlyList<TSource> source, SynchronizationContext synchronizationContext)
        {
            var rangeObservableCollection = new SynchronizedRangeObservableCollection<TSource>(synchronizationContext, false);
            var rangeObservableCollectionAccess = new object();
            var distinctCounts = new Dictionary<TSource, int>();
            foreach (var element in source)
            {
                if (distinctCounts.TryGetValue(element, out var distinctCount))
                    distinctCounts[element] = ++distinctCount;
                else
                {
                    distinctCounts.Add(element, 1);
                    rangeObservableCollection.Add(element);
                }
            }

            void collectionChangedHandler(object sender, NotifyCollectionChangedEventArgs e)
            {
                lock (rangeObservableCollectionAccess)
                {
                    if (e.Action == NotifyCollectionChangedAction.Reset)
                    {
                        distinctCounts.Clear();
                        rangeObservableCollection.Clear();
                    }
                    else if (e.Action != NotifyCollectionChangedAction.Move)
                    {
                        if (e.OldItems != null && e.OldStartingIndex >= 0)
                        {
                            var removingResults = new List<TSource>();
                            foreach (TSource oldItem in e.OldItems)
                            {
                                if (--distinctCounts[oldItem] == 0)
                                {
                                    distinctCounts.Remove(oldItem);
                                    removingResults.Add(oldItem);
                                }
                            }
                            if (removingResults.Count > 0)
                                rangeObservableCollection.RemoveRange(removingResults);
                        }
                        if (e.NewItems != null && e.NewStartingIndex >= 0)
                        {
                            var addingResults = new List<TSource>();
                            foreach (TSource newItem in e.NewItems)
                            {
                                if (distinctCounts.TryGetValue(newItem, out var distinctCount))
                                    distinctCounts[newItem] = ++distinctCount;
                                else
                                {
                                    distinctCounts.Add(newItem, 1);
                                    addingResults.Add(newItem);
                                }
                            }
                            if (addingResults.Count > 0)
                                rangeObservableCollection.AddRange(addingResults);
                        }
                    }
                }
            }

            {
                if (source is INotifyCollectionChanged notifyingSource)
                    notifyingSource.CollectionChanged += collectionChangedHandler;
            }
            var result = new ActiveEnumerable<TSource>(rangeObservableCollection, disposing =>
            {
                if (source is INotifyCollectionChanged notifyingSource)
                    notifyingSource.CollectionChanged -= collectionChangedHandler;
            });
            if (synchronizationContext != null)
                rangeObservableCollection.IsSynchronized = true;
            return result;
        }

        public static ActiveEnumerable<ActiveGrouping<TKey, TSource>> ActiveGroupBy<TKey, TSource>(this IReadOnlyList<TSource> source, Func<TSource, TKey> keySelector, params string[] keySelectorProperties) where TKey : IEquatable<TKey> where TSource : class =>
            ActiveGroupBy(source, (source as ISynchronizable)?.SynchronizationContext, keySelector, keySelectorProperties);

        public static ActiveEnumerable<ActiveGrouping<TKey, TSource>> ActiveGroupBy<TKey, TSource>(this IReadOnlyList<TSource> source, SynchronizationContext synchronizationContext, Func<TSource, TKey> keySelector, params string[] keySelectorProperties) where TKey : IEquatable<TKey> where TSource : class
        {
            var rangeObservableCollection = new SynchronizedRangeObservableCollection<ActiveGrouping<TKey, TSource>>(synchronizationContext, false);
            var rangeObservableCollectionAccess = new object();
            var collectionAndGroupingDictionary = new Dictionary<TKey, (SynchronizedRangeObservableCollection<TSource> groupingObservableCollection, ActiveGrouping<TKey, TSource> grouping)>();
            var keyDictionary = new Dictionary<TSource, TKey>();

            var monitor = ActiveListMonitor<TSource>.Monitor(source, keySelectorProperties);

            void addElement(TSource element)
            {
                var key = keySelector(element);
                if (!monitor.ElementsNotifyChanging)
                    keyDictionary.Add(element, key);
                SynchronizedRangeObservableCollection<TSource> groupingObservableCollection;
                if (!collectionAndGroupingDictionary.TryGetValue(key, out var collectionAndGrouping))
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

            void elementPropertyChangedHandler(object sender, ElementPropertyChangeEventArgs<TSource> e)
            {
                lock (rangeObservableCollectionAccess)
                {
                    var element = e.Element;
                    var oldKey = keyDictionary[element];
                    var newKey = keySelector(element);
                    if (!oldKey.Equals(newKey))
                    {
                        if (!monitor.ElementsNotifyChanging)
                            keyDictionary[element] = newKey;
                        var oldCollectionAndGrouping = collectionAndGroupingDictionary[oldKey];
                        oldCollectionAndGrouping.groupingObservableCollection.Remove(element);
                        if (oldCollectionAndGrouping.groupingObservableCollection.Count == 0)
                            collectionAndGroupingDictionary.Remove(oldKey);
                        SynchronizedRangeObservableCollection<TSource> groupingObservableCollection;
                        if (!collectionAndGroupingDictionary.TryGetValue(newKey, out var collectionAndGrouping))
                        {
                            groupingObservableCollection = new SynchronizedRangeObservableCollection<TSource>(synchronizationContext);
                            var grouping = new ActiveGrouping<TKey, TSource>(newKey, groupingObservableCollection);
                            collectionAndGrouping = (groupingObservableCollection, grouping);
                            rangeObservableCollection.Add(grouping);
                        }
                        else
                            groupingObservableCollection = collectionAndGrouping.groupingObservableCollection;
                        groupingObservableCollection.Add(element);
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

            void elementsAddedHandler(object sender, ElementMembershipEventArgs<TSource> e)
            {
                lock (rangeObservableCollectionAccess)
                {
                    foreach (var element in e.Elements)
                        addElement(element);
                }
            }

            void elementsRemovedHandler(object sender, ElementMembershipEventArgs<TSource> e)
            {
                lock (rangeObservableCollectionAccess)
                    foreach (var element in e.Elements)
                    {
                        var key = keyDictionary[element];
                        keyDictionary.Remove(element);
                        var (groupingObservableCollection, grouping) = collectionAndGroupingDictionary[key];
                        groupingObservableCollection.Remove(element);
                        if (groupingObservableCollection.Count == 0)
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

        public static ActiveAggregateValue<TResult> ActiveMax<TKey, TValue, TResult>(this IReadOnlyDictionary<TKey, TValue> source, Func<TKey, TValue, TResult> selector, bool isThreadSafe = false, params string[] selectorProperties) where TResult : IComparable<TResult>
        {
            var selectorValues = source is SortedDictionary<TKey, TValue> || source is ObservableSortedDictionary<TKey, TValue> || source is SynchronizedObservableSortedDictionary<TKey, TValue> ? (IDictionary<TKey, TResult>)new SortedDictionary<TKey, TResult>() : new Dictionary<TKey, TResult>();
            var firstIsValid = false;
            TResult firstMax = default;
            if (source.Count > 0)
            {
                firstIsValid = true;
                var firstKeyValuePair = source.First();
                var firstKey = firstKeyValuePair.Key;
                firstMax = selector(firstKey, firstKeyValuePair.Value);
                selectorValues.Add(firstKey, firstMax);
                foreach (var keyValuePair in source.Skip(1))
                {
                    var key = keyValuePair.Key;
                    var selectorValue = selector(key, keyValuePair.Value);
                    if (selectorValue.CompareTo(firstMax) > 0)
                        firstMax = selectorValue;
                    selectorValues.Add(key, selectorValue);
                }
            }
            var monitor = ActiveDictionaryMonitor<TKey, TValue>.Monitor(source, selectorProperties);
            EventHandler<NotifyDictionaryValueEventArgs<TKey, TValue>> valueAdded = null;
            EventHandler<ValuePropertyChangeEventArgs<TKey, TValue>> valuePropertyChanged = null;
            EventHandler<NotifyDictionaryValueEventArgs<TKey, TValue>> valueRemoved = null;
            EventHandler<NotifyDictionaryValueReplacedEventArgs<TKey, TValue>> valueReplaced = null;
            EventHandler<NotifyDictionaryValuesEventArgs<TKey, TValue>> valuesAdded = null;
            EventHandler<NotifyDictionaryValuesEventArgs<TKey, TValue>> valuesRemoved = null;
            var resultAccess = isThreadSafe ? new object() : null;
            var result = new ActiveAggregateValue<TResult>(firstIsValid, firstMax, out var setValidity, out var setValue, disposing =>
            {
                monitor.ValueAdded -= valueAdded;
                monitor.ValuePropertyChanged -= valuePropertyChanged;
                monitor.ValueRemoved -= valueRemoved;
                monitor.ValueReplaced -= valueReplaced;
                monitor.ValuesAdded -= valuesAdded;
                monitor.ValuesRemoved -= valuesRemoved;
                if (disposing)
                    monitor.Dispose();
            });

            void valueAddedLogic(NotifyDictionaryValueEventArgs<TKey, TValue> e)
            {
                var key = e.Key;
                if (selectorValues.Count == 0)
                {
                    var currentMax = selector(key, e.Value);
                    selectorValues.Add(key, currentMax);
                    setValidity(true);
                    setValue(currentMax);
                }
                else
                {
                    var selectorValue = selector(key, e.Value);
                    selectorValues.Add(key, selectorValue);
                    if (selectorValue.CompareTo(result.Value) > 0)
                        setValue(selectorValue);
                }
            }

            void valuePropertyChangedLogic(ValuePropertyChangeEventArgs<TKey, TValue> e)
            {
                var key = e.Key;
                var previousSelectorValue = selectorValues[key];
                var newSelectorValue = selector(key, e.Value);
                selectorValues[key] = newSelectorValue;
                var currentMax = result.Value;
                var comparison = newSelectorValue.CompareTo(currentMax);
                if (comparison > 0)
                    setValue(newSelectorValue);
                else if (comparison < 0 && previousSelectorValue.CompareTo(currentMax) == 0)
                    setValue(selectorValues.Values.Max());
            }

            void valueRemovedLogic(NotifyDictionaryValueEventArgs<TKey, TValue> e)
            {
                if (selectorValues.Count == 1)
                {
                    selectorValues = source is SortedDictionary<TKey, TResult> || source is ObservableSortedDictionary<TKey, TValue> || source is SynchronizedObservableSortedDictionary<TKey, TValue> ? (IDictionary<TKey, TResult>)new SortedDictionary<TKey, TResult>() : new Dictionary<TKey, TResult>();
                    setValidity(false);
                }
                else
                {
                    var key = e.Key;
                    if (selectorValues.TryGetValue(key, out var selectorValue))
                    {
                        selectorValues.Remove(key);
                        if (selectorValue.CompareTo(result.Value) == 0)
                            setValue(selectorValues.Values.Max());
                    }
                }
            }

            void valueReplacedLogic(NotifyDictionaryValueReplacedEventArgs<TKey, TValue> e)
            {
                var key = e.Key;
                var previousSelectorValue = selectorValues[key];
                var currentSelectorValue = selector(key, e.NewValue);
                selectorValues[key] = currentSelectorValue;
                var previousToCurrent = previousSelectorValue.CompareTo(currentSelectorValue);
                if (previousToCurrent < 0 && currentSelectorValue.CompareTo(result.Value) > 0)
                    setValue(currentSelectorValue);
                else if (previousToCurrent > 0 && previousSelectorValue.CompareTo(result.Value) == 0)
                    setValue(selectorValues.Values.Max());
            }

            void valuesAddedLogic(NotifyDictionaryValuesEventArgs<TKey, TValue> e)
            {
                if (selectorValues.Count == 0)
                {
                    var firstKeyValuePair = e.KeyValuePairs.First();
                    var currentMax = selector(firstKeyValuePair.Key, firstKeyValuePair.Value);
                    selectorValues.Add(firstKeyValuePair.Key, currentMax);
                    foreach (var keyValuePair in e.KeyValuePairs.Skip(1))
                    {
                        var key = keyValuePair.Key;
                        var selectorValue = selector(key, keyValuePair.Value);
                        if (selectorValue.CompareTo(currentMax) > 0)
                            currentMax = selectorValue;
                        selectorValues.Add(key, selectorValue);
                    }
                    setValidity(true);
                    setValue(currentMax);
                }
                else
                {
                    var currentMax = result.Value;
                    foreach (var keyValuePair in e.KeyValuePairs)
                    {
                        var key = keyValuePair.Key;
                        var selectorValue = selector(key, keyValuePair.Value);
                        selectorValues.Add(key, selectorValue);
                        if (selectorValue.CompareTo(currentMax) > 0)
                            currentMax = selectorValue;
                    }
                    if (currentMax.CompareTo(result.Value) > 0)
                        setValue(currentMax);
                }
            }

            void valuesRemovedLogic(NotifyDictionaryValuesEventArgs<TKey, TValue> e)
            {
                if (selectorValues.Count == e.KeyValuePairs.Count)
                {
                    selectorValues = source is SortedDictionary<TKey, TValue> || source is ObservableSortedDictionary<TKey, TValue> || source is SynchronizedObservableSortedDictionary<TKey, TValue> ? (IDictionary<TKey, TResult>)new SortedDictionary<TKey, TResult>() : new Dictionary<TKey, TResult>();
                    setValidity(false);
                }
                else
                {
                    var currentMax = result.Value;
                    var maxRemoved = false;
                    foreach (var keyValuePair in e.KeyValuePairs)
                    {
                        var key = keyValuePair.Key;
                        if (selectorValues.TryGetValue(key, out var selectorValue))
                        {
                            if (selectorValue.CompareTo(currentMax) == 0)
                                maxRemoved = true;
                            selectorValues.Remove(key);
                        }
                    }
                    if (maxRemoved)
                        setValue(selectorValues.Values.Max());
                }
            }

            if (isThreadSafe)
            {
                valueAdded = (sender, e) =>
                {
                    lock (resultAccess)
                        valueAddedLogic(e);
                };
                valuePropertyChanged = (sender, e) =>
                {
                    lock (resultAccess)
                        valuePropertyChangedLogic(e);
                };
                valueRemoved = (sender, e) =>
                {
                    lock (resultAccess)
                        valueRemovedLogic(e);
                };
                valueReplaced = (sender, e) =>
                {
                    lock (resultAccess)
                        valueReplacedLogic(e);
                };
                valuesAdded = (sender, e) =>
                {
                    lock (resultAccess)
                        valuesAddedLogic(e);
                };
                valuesRemoved = (sender, e) =>
                {
                    lock (resultAccess)
                        valuesRemovedLogic(e);
                };
            }
            else
            {
                valueAdded = (sender, e) => valueAddedLogic(e);
                valuePropertyChanged = (sender, e) => valuePropertyChangedLogic(e);
                valueRemoved = (sender, e) => valueRemovedLogic(e);
                valueReplaced = (sender, e) => valueReplacedLogic(e);
                valuesAdded = (sender, e) => valuesAddedLogic(e);
                valuesRemoved = (sender, e) => valuesRemovedLogic(e);
            }

            monitor.ValueAdded += valueAdded;
            monitor.ValuePropertyChanged += valuePropertyChanged;
            monitor.ValueRemoved += valueRemoved;
            monitor.ValueReplaced += valueReplaced;
            monitor.ValuesAdded += valuesAdded;
            monitor.ValuesRemoved += valuesRemoved;
            return result;
        }

        public static ActiveAggregateValue<TResult?> ActiveMax<TKey, TValue, TResult>(this IReadOnlyDictionary<TKey, TValue> source, Func<TKey, TValue, TResult?> selector, bool isThreadSafe = false, params string[] selectorProperties) where TResult : struct, IComparable<TResult>
        {
            var selectorValues = source is SortedDictionary<TKey, TValue> || source is ObservableSortedDictionary<TKey, TValue> || source is SynchronizedObservableSortedDictionary<TKey, TValue> ? (IDictionary<TKey, TResult?>)new SortedDictionary<TKey, TResult?>() : new Dictionary<TKey, TResult?>();
            var firstIsValid = false;
            TResult? firstMax = default;
            if (source.Count > 0)
            {
                firstIsValid = true;
                var firstKeyValuePair = source.First();
                var firstKey = firstKeyValuePair.Key;
                firstMax = selector(firstKey, firstKeyValuePair.Value);
                selectorValues.Add(firstKey, firstMax);
                foreach (var keyValuePair in source.Skip(1))
                {
                    var key = keyValuePair.Key;
                    var selectorValue = selector(key, keyValuePair.Value);
                    if (selectorValue != null && (firstMax == null || selectorValue.Value.CompareTo(firstMax.Value) > 0))
                        firstMax = selectorValue;
                    selectorValues.Add(key, selectorValue);
                }
            }
            var monitor = ActiveDictionaryMonitor<TKey, TValue>.Monitor(source, selectorProperties);
            EventHandler<NotifyDictionaryValueEventArgs<TKey, TValue>> valueAdded = null;
            EventHandler<ValuePropertyChangeEventArgs<TKey, TValue>> valuePropertyChanged = null;
            EventHandler<NotifyDictionaryValueEventArgs<TKey, TValue>> valueRemoved = null;
            EventHandler<NotifyDictionaryValueReplacedEventArgs<TKey, TValue>> valueReplaced = null;
            EventHandler<NotifyDictionaryValuesEventArgs<TKey, TValue>> valuesAdded = null;
            EventHandler<NotifyDictionaryValuesEventArgs<TKey, TValue>> valuesRemoved = null;
            var resultAccess = isThreadSafe ? new object() : null;
            var result = new ActiveAggregateValue<TResult?>(firstIsValid, firstMax, out var setValidity, out var setValue, disposing =>
            {
                monitor.ValueAdded -= valueAdded;
                monitor.ValuePropertyChanged -= valuePropertyChanged;
                monitor.ValueRemoved -= valueRemoved;
                monitor.ValueReplaced -= valueReplaced;
                monitor.ValuesAdded -= valuesAdded;
                monitor.ValuesRemoved -= valuesRemoved;
                if (disposing)
                    monitor.Dispose();
            });

            void valueAddedLogic(NotifyDictionaryValueEventArgs<TKey, TValue> e)
            {
                var key = e.Key;
                if (selectorValues.Count == 0)
                {
                    var selectorValue = selector(key, e.Value);
                    selectorValues.Add(key, selectorValue);
                    setValidity(true);
                    setValue(selectorValue);
                }
                else
                {
                    var currentMax = result.Value;
                    var selectorValue = selector(key, e.Value);
                    selectorValues.Add(key, selectorValue);
                    if (selectorValue != null && (currentMax == null || selectorValue.Value.CompareTo(currentMax.Value) > 0))
                        currentMax = selectorValue;
                    if (((currentMax == null) != (result.Value == null)) || (currentMax != null && result.Value != null && currentMax.Value.CompareTo(result.Value.Value) > 0))
                        setValue(currentMax);
                }
            }

            void valuePropertyChangedLogic(ValuePropertyChangeEventArgs<TKey, TValue> e)
            {
                var key = e.Key;
                var previousSelectorValue = selectorValues[key];
                var newSelectorValue = selector(key, e.Value);
                selectorValues[key] = newSelectorValue;
                var currentMax = result.Value;
                if (newSelectorValue != null && (currentMax == null || newSelectorValue.Value.CompareTo(currentMax.Value) > 0))
                    setValue(newSelectorValue);
                else if (previousSelectorValue != null && currentMax != null && previousSelectorValue.Value.CompareTo(currentMax.Value) == 0 && (newSelectorValue == null || newSelectorValue.Value.CompareTo(previousSelectorValue.Value) < 0))
                    setValue(selectorValues.Values.Max());
            }

            void valueRemovedLogic(NotifyDictionaryValueEventArgs<TKey, TValue> e)
            {
                if (selectorValues.Count == 1)
                {
                    selectorValues = source is SortedDictionary<TKey, TValue> || source is ObservableSortedDictionary<TKey, TValue> || source is SynchronizedObservableSortedDictionary<TKey, TValue> ? (IDictionary<TKey, TResult?>)new SortedDictionary<TKey, TResult?>() : new Dictionary<TKey, TResult?>();
                    setValidity(false);
                }
                else
                {
                    var currentMax = result.Value;
                    var maxRemoved = false;
                    var key = e.Key;
                    if (selectorValues.TryGetValue(key, out var selectorValue))
                    {
                        if (currentMax != null && selectorValue != null && selectorValue.Value.CompareTo(currentMax.Value) == 0)
                            maxRemoved = true;
                        selectorValues.Remove(key);
                    }
                    if (maxRemoved)
                        setValue(selectorValues.Values.Max());
                }
            }

            void valueReplacedLogic(NotifyDictionaryValueReplacedEventArgs<TKey, TValue> e)
            {
                var key = e.Key;
                var previousSelectorValue = selectorValues[key];
                var currentSelectorValue = selector(key, e.NewValue);
                selectorValues[key] = currentSelectorValue;
                var previousToCurrent = (previousSelectorValue ?? default).CompareTo(currentSelectorValue ?? default);
                if (currentSelectorValue != null && (previousSelectorValue == null || previousToCurrent < 0) && (result.Value == null || currentSelectorValue.Value.CompareTo(result.Value.Value) > 0))
                    setValue(currentSelectorValue);
                else if (previousSelectorValue != null && (currentSelectorValue == null || (previousToCurrent > 0 && previousSelectorValue.Value.CompareTo(result.Value.Value) == 0)))
                    setValue(selectorValues.Values.Max());
            }

            void valuesAddedLogic(NotifyDictionaryValuesEventArgs<TKey, TValue> e)
            {
                if (selectorValues.Count == 0)
                {
                    var firstKeyValuePair = source.First();
                    var firstKey = firstKeyValuePair.Key;
                    var currentMax = selector(firstKey, firstKeyValuePair.Value);
                    selectorValues.Add(firstKey, currentMax);
                    foreach (var keyValuePair in source.Skip(1))
                    {
                        var key = keyValuePair.Key;
                        var selectorValue = selector(key, keyValuePair.Value);
                        if (selectorValue != null && (currentMax == null || selectorValue.Value.CompareTo(currentMax.Value) > 0))
                            currentMax = selectorValue;
                        selectorValues.Add(key, selectorValue);
                    }
                    setValidity(true);
                    setValue(currentMax);
                }
                else
                {
                    var currentMax = result.Value;
                    foreach (var keyValuePair in e.KeyValuePairs)
                    {
                        var key = keyValuePair.Key;
                        var selectorValue = selector(key, keyValuePair.Value);
                        selectorValues.Add(key, selectorValue);
                        if (selectorValue != null && (currentMax == null || selectorValue.Value.CompareTo(currentMax.Value) > 0))
                            currentMax = selectorValue;
                    }
                    if (((currentMax == null) != (result.Value == null)) || (currentMax != null && result.Value != null && currentMax.Value.CompareTo(result.Value.Value) > 0))
                        setValue(currentMax);
                }
            }

            void valuesRemovedLogic(NotifyDictionaryValuesEventArgs<TKey, TValue> e)
            {
                if (selectorValues.Count == e.KeyValuePairs.Count)
                {
                    selectorValues = source is SortedDictionary<TKey, TValue> || source is ObservableSortedDictionary<TKey, TValue> || source is SynchronizedObservableSortedDictionary<TKey, TValue> ? (IDictionary<TKey, TResult?>)new SortedDictionary<TKey, TResult?>() : new Dictionary<TKey, TResult?>();
                    setValidity(false);
                }
                else
                {
                    var currentMax = result.Value;
                    var maxRemoved = false;
                    foreach (var keyValuePair in e.KeyValuePairs)
                    {
                        var key = keyValuePair.Key;
                        if (selectorValues.TryGetValue(key, out var selectorValue))
                        {
                            if (currentMax != null && selectorValue != null && selectorValue.Value.CompareTo(currentMax.Value) == 0)
                                maxRemoved = true;
                            selectorValues.Remove(key);
                        }
                    }
                    if (maxRemoved)
                        setValue(selectorValues.Values.Max());
                }
            }

            if (isThreadSafe)
            {
                valueAdded = (sender, e) =>
                {
                    lock (resultAccess)
                        valueAddedLogic(e);
                };
                valuePropertyChanged = (sender, e) =>
                {
                    lock (resultAccess)
                        valuePropertyChangedLogic(e);
                };
                valueRemoved = (sender, e) =>
                {
                    lock (resultAccess)
                        valueRemovedLogic(e);
                };
                valueReplaced = (sender, e) =>
                {
                    lock (resultAccess)
                        valueReplacedLogic(e);
                };
                valuesAdded = (sender, e) =>
                {
                    lock (resultAccess)
                        valuesAddedLogic(e);
                };
                valuesRemoved = (sender, e) =>
                {
                    lock (resultAccess)
                        valuesRemovedLogic(e);
                };
            }
            else
            {
                valueAdded = (sender, e) => valueAddedLogic(e);
                valuePropertyChanged = (sender, e) => valuePropertyChangedLogic(e);
                valueRemoved = (sender, e) => valueRemovedLogic(e);
                valueReplaced = (sender, e) => valueReplacedLogic(e);
                valuesAdded = (sender, e) => valuesAddedLogic(e);
                valuesRemoved = (sender, e) => valuesRemovedLogic(e);
            }

            monitor.ValueAdded += valueAdded;
            monitor.ValuePropertyChanged += valuePropertyChanged;
            monitor.ValueRemoved += valueRemoved;
            monitor.ValueReplaced += valueReplaced;
            monitor.ValuesAdded += valuesAdded;
            monitor.ValuesRemoved += valuesRemoved;
            return result;
        }

        public static ActiveAggregateValue<TResult> ActiveMax<TSource, TResult>(this IReadOnlyList<TSource> source, Func<TSource, TResult> selector, bool isThreadSafe = false, params string[] selectorProperties) where TSource : class where TResult : IComparable<TResult>
        {
            var selectorValues = new Dictionary<object, TResult>();
            var firstIsValid = false;
            TResult firstMax = default;
            if (source.Count > 0)
            {
                firstIsValid = true;
                var firstElement = source.First();
                firstMax = selector(firstElement);
                selectorValues.Add(firstElement, firstMax);
                foreach (var element in source.Skip(1))
                {
                    var selectorValue = selector(element);
                    if (selectorValue.CompareTo(firstMax) > 0)
                        firstMax = selectorValue;
                    selectorValues.Add(element, selectorValue);
                }
            }
            var monitor = ActiveListMonitor<TSource>.Monitor(source, selectorProperties);
            EventHandler<ElementPropertyChangeEventArgs<TSource>> elementPropertyChanged = null;
            EventHandler<ElementMembershipEventArgs<TSource>> elementsAdded = null;
            EventHandler<ElementMembershipEventArgs<TSource>> elementsRemoved = null;
            var resultAccess = isThreadSafe ? new object() : null;
            var result = new ActiveAggregateValue<TResult>(firstIsValid, firstMax, out var setValidity, out var setValue, disposing =>
            {
                monitor.ElementPropertyChanged -= elementPropertyChanged;
                monitor.ElementsAdded -= elementsAdded;
                monitor.ElementsRemoved -= elementsRemoved;
                if (disposing)
                    monitor.Dispose();
            });

            void elementPropertyChangedLogic(ElementPropertyChangeEventArgs<TSource> e)
            {
                var element = e.Element;
                var previousSelectorValue = selectorValues[element];
                var newSelectorValue = selector(element);
                selectorValues[element] = newSelectorValue;
                var currentMax = result.Value;
                var comparison = newSelectorValue.CompareTo(currentMax);
                if (comparison > 0)
                    setValue(newSelectorValue);
                else if (comparison < 0 && previousSelectorValue.CompareTo(currentMax) == 0)
                    setValue(selectorValues.Values.Max());
            }

            void elementsAddedLogic(ElementMembershipEventArgs<TSource> e)
            {
                if (selectorValues.Count == 0)
                {
                    var firstElement = e.Elements.First();
                    var currentMax = selector(firstElement);
                    selectorValues.Add(firstElement, currentMax);
                    foreach (var element in e.Elements.Skip(1))
                    {
                        var selectorValue = selector(element);
                        if (selectorValue.CompareTo(currentMax) > 0)
                            currentMax = selectorValue;
                        selectorValues.Add(element, selectorValue);
                    }
                    setValidity(true);
                    setValue(currentMax);
                }
                else
                {
                    var currentMax = result.Value;
                    foreach (var element in e.Elements)
                    {
                        var selectorValue = selector(element);
                        selectorValues.Add(element, selectorValue);
                        if (selectorValue.CompareTo(currentMax) > 0)
                            currentMax = selectorValue;
                    }
                    if (currentMax.CompareTo(result.Value) > 0)
                        setValue(currentMax);
                }
            }

            void elementsRemovedLogic(ElementMembershipEventArgs<TSource> e)
            {
                if (selectorValues.Count == e.Count)
                {
                    selectorValues = new Dictionary<object, TResult>();
                    setValidity(false);
                }
                else
                {
                    var currentMax = result.Value;
                    var maxRemoved = false;
                    foreach (var element in e.Elements)
                    {
                        if (selectorValues.TryGetValue(element, out var selectorValue))
                        {
                            if (selectorValue.CompareTo(currentMax) == 0)
                                maxRemoved = true;
                            selectorValues.Remove(element);
                        }
                    }
                    if (maxRemoved)
                        setValue(selectorValues.Values.Max());
                }
            }

            if (isThreadSafe)
            {
                elementPropertyChanged = (sender, e) =>
                {
                    lock (resultAccess)
                        elementPropertyChangedLogic(e);
                };
                elementsAdded = (sender, e) =>
                {
                    lock (resultAccess)
                        elementsAddedLogic(e);
                };
                elementsRemoved = (sender, e) =>
                {
                    lock (resultAccess)
                        elementsRemovedLogic(e);
                };
            }
            else
            {
                elementPropertyChanged = (sender, e) => elementPropertyChangedLogic(e);
                elementsAdded = (sender, e) => elementsAddedLogic(e);
                elementsRemoved = (sender, e) => elementsRemovedLogic(e);
            }

            monitor.ElementPropertyChanged += elementPropertyChanged;
            monitor.ElementsAdded += elementsAdded;
            monitor.ElementsRemoved += elementsRemoved;
            return result;
        }

        public static ActiveAggregateValue<TResult?> ActiveMax<TSource, TResult>(this IReadOnlyList<TSource> source, Func<TSource, TResult?> selector, bool isThreadSafe = false, params string[] selectorProperties) where TSource : class where TResult : struct, IComparable<TResult>
        {
            var selectorValues = new Dictionary<object, TResult?>();
            var firstIsValid = false;
            TResult? firstMax = default;
            if (source.Count > 0)
            {
                firstIsValid = true;
                var firstElement = source.First();
                firstMax = selector(firstElement);
                selectorValues.Add(firstElement, firstMax);
                foreach (var element in source.Skip(1))
                {
                    var selectorValue = selector(element);
                    if (selectorValue != null && (firstMax == null || selectorValue.Value.CompareTo(firstMax.Value) > 0))
                        firstMax = selectorValue;
                    selectorValues.Add(element, selectorValue);
                }
            }
            var monitor = ActiveListMonitor<TSource>.Monitor(source, selectorProperties);
            EventHandler<ElementPropertyChangeEventArgs<TSource>> elementPropertyChanged = null;
            EventHandler<ElementMembershipEventArgs<TSource>> elementsAdded = null;
            EventHandler<ElementMembershipEventArgs<TSource>> elementsRemoved = null;
            var resultAccess = isThreadSafe ? new object() : null;
            var result = new ActiveAggregateValue<TResult?>(firstIsValid, firstMax, out var setValidity, out var setValue, disposing =>
            {
                monitor.ElementPropertyChanged -= elementPropertyChanged;
                monitor.ElementsAdded -= elementsAdded;
                monitor.ElementsRemoved -= elementsRemoved;
                if (disposing)
                    monitor.Dispose();
            });

            void elementPropertyChangedLogic(ElementPropertyChangeEventArgs<TSource> e)
            {
                var element = e.Element;
                var previousSelectorValue = selectorValues[element];
                var newSelectorValue = selector(element);
                selectorValues[element] = newSelectorValue;
                var currentMax = result.Value;
                if (newSelectorValue != null && (currentMax == null || newSelectorValue.Value.CompareTo(currentMax.Value) > 0))
                    setValue(newSelectorValue);
                else if (previousSelectorValue != null && currentMax != null && previousSelectorValue.Value.CompareTo(currentMax.Value) == 0 && (newSelectorValue == null || newSelectorValue.Value.CompareTo(previousSelectorValue.Value) < 0))
                    setValue(selectorValues.Values.Max());
            }

            void elementsAddedLogic(ElementMembershipEventArgs<TSource> e)
            {
                if (selectorValues.Count == 0)
                {
                    var firstElement = source.First();
                    var currentMax = selector(firstElement);
                    selectorValues.Add(firstElement, currentMax);
                    foreach (var element in source.Skip(1))
                    {
                        var selectorValue = selector(element);
                        if (selectorValue != null && (currentMax == null || selectorValue.Value.CompareTo(currentMax.Value) > 0))
                            currentMax = selectorValue;
                        selectorValues.Add(element, selectorValue);
                    }
                    setValidity(true);
                    setValue(currentMax);
                }
                else
                {
                    var currentMax = result.Value;
                    foreach (var element in e.Elements)
                    {
                        var selectorValue = selector(element);
                        selectorValues.Add(element, selectorValue);
                        if (selectorValue != null && (currentMax == null || selectorValue.Value.CompareTo(currentMax.Value) > 0))
                            currentMax = selectorValue;
                    }
                    if (((currentMax == null) != (result.Value == null)) || (currentMax != null && result.Value != null && currentMax.Value.CompareTo(result.Value.Value) > 0))
                        setValue(currentMax);
                }
            }

            void elementsRemovedLogic(ElementMembershipEventArgs<TSource> e)
            {
                if (selectorValues.Count == e.Count)
                {
                    selectorValues = new Dictionary<object, TResult?>();
                    setValidity(false);
                }
                else
                {
                    var currentMax = result.Value;
                    var maxRemoved = false;
                    foreach (var element in e.Elements)
                    {
                        if (selectorValues.TryGetValue(element, out var selectorValue))
                        {
                            if (currentMax != null && selectorValue != null && selectorValue.Value.CompareTo(currentMax.Value) == 0)
                                maxRemoved = true;
                            selectorValues.Remove(element);
                        }
                    }
                    if (maxRemoved)
                        setValue(selectorValues.Values.Max());
                }
            }

            if (isThreadSafe)
            {
                elementPropertyChanged = (sender, e) =>
                {
                    lock (resultAccess)
                        elementPropertyChangedLogic(e);
                };
                elementsAdded = (sender, e) =>
                {
                    lock (resultAccess)
                        elementsAddedLogic(e);
                };
                elementsRemoved = (sender, e) =>
                {
                    lock (resultAccess)
                        elementsRemovedLogic(e);
                };
            }
            else
            {
                elementPropertyChanged = (sender, e) => elementPropertyChangedLogic(e);
                elementsAdded = (sender, e) => elementsAddedLogic(e);
                elementsRemoved = (sender, e) => elementsRemovedLogic(e);
            }

            monitor.ElementPropertyChanged += elementPropertyChanged;
            monitor.ElementsAdded += elementsAdded;
            monitor.ElementsRemoved += elementsRemoved;
            return result;
        }

        public static ActiveAggregateValue<TResult> ActiveMin<TKey, TValue, TResult>(this IReadOnlyDictionary<TKey, TValue> source, Func<TKey, TValue, TResult> selector, bool isThreadSafe = false, params string[] selectorProperties) where TResult : IComparable<TResult>
        {
            var selectorValues = source is SortedDictionary<TKey, TValue> || source is ObservableSortedDictionary<TKey, TValue> || source is SynchronizedObservableSortedDictionary<TKey, TValue> ? (IDictionary<TKey, TResult>)new SortedDictionary<TKey, TResult>() : new Dictionary<TKey, TResult>();
            var firstIsValid = false;
            TResult firstMin = default;
            if (source.Count > 0)
            {
                firstIsValid = true;
                var firstKeyValuePair = source.First();
                var firstKey = firstKeyValuePair.Key;
                firstMin = selector(firstKey, firstKeyValuePair.Value);
                selectorValues.Add(firstKey, firstMin);
                foreach (var keyValuePair in source.Skip(1))
                {
                    var key = keyValuePair.Key;
                    var selectorValue = selector(key, keyValuePair.Value);
                    if (selectorValue.CompareTo(firstMin) < 0)
                        firstMin = selectorValue;
                    selectorValues.Add(key, selectorValue);
                }
            }
            var monitor = ActiveDictionaryMonitor<TKey, TValue>.Monitor(source, selectorProperties);
            EventHandler<NotifyDictionaryValueEventArgs<TKey, TValue>> valueAdded = null;
            EventHandler<ValuePropertyChangeEventArgs<TKey, TValue>> valuePropertyChanged = null;
            EventHandler<NotifyDictionaryValueEventArgs<TKey, TValue>> valueRemoved = null;
            EventHandler<NotifyDictionaryValueReplacedEventArgs<TKey, TValue>> valueReplaced = null;
            EventHandler<NotifyDictionaryValuesEventArgs<TKey, TValue>> valuesAdded = null;
            EventHandler<NotifyDictionaryValuesEventArgs<TKey, TValue>> valuesRemoved = null;
            var resultAccess = isThreadSafe ? new object() : null;
            var result = new ActiveAggregateValue<TResult>(firstIsValid, firstMin, out var setValidity, out var setValue, disposing =>
            {
                monitor.ValueAdded -= valueAdded;
                monitor.ValuePropertyChanged -= valuePropertyChanged;
                monitor.ValueRemoved -= valueRemoved;
                monitor.ValueReplaced -= valueReplaced;
                monitor.ValuesAdded -= valuesAdded;
                monitor.ValuesRemoved -= valuesRemoved;
                if (disposing)
                    monitor.Dispose();
            });

            void valueAddedLogic(NotifyDictionaryValueEventArgs<TKey, TValue> e)
            {
                var key = e.Key;
                if (selectorValues.Count == 0)
                {
                    var currentMin = selector(key, e.Value);
                    selectorValues.Add(key, currentMin);
                    setValidity(true);
                    setValue(currentMin);
                }
                else
                {
                    var selectorValue = selector(key, e.Value);
                    selectorValues.Add(key, selectorValue);
                    if (selectorValue.CompareTo(result.Value) < 0)
                        setValue(selectorValue);
                }
            }

            void valuePropertyChangedLogic(ValuePropertyChangeEventArgs<TKey, TValue> e)
            {
                var key = e.Key;
                var previousSelectorValue = selectorValues[key];
                var newSelectorValue = selector(key, e.Value);
                selectorValues[key] = newSelectorValue;
                var currentMin = result.Value;
                var comparison = newSelectorValue.CompareTo(currentMin);
                if (comparison < 0)
                    setValue(newSelectorValue);
                else if (comparison > 0 && previousSelectorValue.CompareTo(currentMin) == 0)
                    setValue(selectorValues.Values.Min());
            }

            void valueRemovedLogic(NotifyDictionaryValueEventArgs<TKey, TValue> e)
            {
                if (selectorValues.Count == 1)
                {
                    selectorValues = source is SortedDictionary<TKey, TResult> || source is ObservableSortedDictionary<TKey, TValue> || source is SynchronizedObservableSortedDictionary<TKey, TValue> ? (IDictionary<TKey, TResult>)new SortedDictionary<TKey, TResult>() : new Dictionary<TKey, TResult>();
                    setValidity(false);
                }
                else
                {
                    var key = e.Key;
                    if (selectorValues.TryGetValue(key, out var selectorValue))
                    {
                        selectorValues.Remove(key);
                        if (selectorValue.CompareTo(result.Value) == 0)
                            setValue(selectorValues.Values.Max());
                    }
                }
            }

            void valueReplacedLogic(NotifyDictionaryValueReplacedEventArgs<TKey, TValue> e)
            {
                var key = e.Key;
                var previousSelectorValue = selectorValues[key];
                var currentSelectorValue = selector(key, e.NewValue);
                selectorValues[key] = currentSelectorValue;
                var previousToCurrent = previousSelectorValue.CompareTo(currentSelectorValue);
                if (previousToCurrent > 0 && currentSelectorValue.CompareTo(result.Value) < 0)
                    setValue(currentSelectorValue);
                else if (previousToCurrent < 0 && previousSelectorValue.CompareTo(result.Value) == 0)
                    setValue(selectorValues.Values.Min());
            }

            void valuesAddedLogic(NotifyDictionaryValuesEventArgs<TKey, TValue> e)
            {
                if (selectorValues.Count == 0)
                {
                    var firstKeyValuePair = e.KeyValuePairs.First();
                    var currentMin = selector(firstKeyValuePair.Key, firstKeyValuePair.Value);
                    selectorValues.Add(firstKeyValuePair.Key, currentMin);
                    foreach (var keyValuePair in e.KeyValuePairs.Skip(1))
                    {
                        var key = keyValuePair.Key;
                        var selectorValue = selector(key, keyValuePair.Value);
                        if (selectorValue.CompareTo(currentMin) < 0)
                            currentMin = selectorValue;
                        selectorValues.Add(key, selectorValue);
                    }
                    setValidity(true);
                    setValue(currentMin);
                }
                else
                {
                    var currentMin = result.Value;
                    foreach (var keyValuePair in e.KeyValuePairs)
                    {
                        var key = keyValuePair.Key;
                        var selectorValue = selector(key, keyValuePair.Value);
                        selectorValues.Add(key, selectorValue);
                        if (selectorValue.CompareTo(currentMin) < 0)
                            currentMin = selectorValue;
                    }
                    if (currentMin.CompareTo(result.Value) < 0)
                        setValue(currentMin);
                }
            }

            void valuesRemovedLogic(NotifyDictionaryValuesEventArgs<TKey, TValue> e)
            {
                if (selectorValues.Count == e.KeyValuePairs.Count)
                {
                    selectorValues = source is SortedDictionary<TKey, TValue> || source is ObservableSortedDictionary<TKey, TValue> || source is SynchronizedObservableSortedDictionary<TKey, TValue> ? (IDictionary<TKey, TResult>)new SortedDictionary<TKey, TResult>() : new Dictionary<TKey, TResult>();
                    setValidity(false);
                }
                else
                {
                    var currentMin = result.Value;
                    var minRemoved = false;
                    foreach (var keyValuePair in e.KeyValuePairs)
                    {
                        var key = keyValuePair.Key;
                        if (selectorValues.TryGetValue(key, out var selectorValue))
                        {
                            if (selectorValue.CompareTo(currentMin) == 0)
                                minRemoved = true;
                            selectorValues.Remove(key);
                        }
                    }
                    if (minRemoved)
                        setValue(selectorValues.Values.Min());
                }
            }

            if (isThreadSafe)
            {
                valueAdded = (sender, e) =>
                {
                    lock (resultAccess)
                        valueAddedLogic(e);
                };
                valuePropertyChanged = (sender, e) =>
                {
                    lock (resultAccess)
                        valuePropertyChangedLogic(e);
                };
                valueRemoved = (sender, e) =>
                {
                    lock (resultAccess)
                        valueRemovedLogic(e);
                };
                valueReplaced = (sender, e) =>
                {
                    lock (resultAccess)
                        valueReplacedLogic(e);
                };
                valuesAdded = (sender, e) =>
                {
                    lock (resultAccess)
                        valuesAddedLogic(e);
                };
                valuesRemoved = (sender, e) =>
                {
                    lock (resultAccess)
                        valuesRemovedLogic(e);
                };
            }
            else
            {
                valueAdded = (sender, e) => valueAddedLogic(e);
                valuePropertyChanged = (sender, e) => valuePropertyChangedLogic(e);
                valueRemoved = (sender, e) => valueRemovedLogic(e);
                valueReplaced = (sender, e) => valueReplacedLogic(e);
                valuesAdded = (sender, e) => valuesAddedLogic(e);
                valuesRemoved = (sender, e) => valuesRemovedLogic(e);
            }

            monitor.ValueAdded += valueAdded;
            monitor.ValuePropertyChanged += valuePropertyChanged;
            monitor.ValueRemoved += valueRemoved;
            monitor.ValueReplaced += valueReplaced;
            monitor.ValuesAdded += valuesAdded;
            monitor.ValuesRemoved += valuesRemoved;
            return result;
        }

        public static ActiveAggregateValue<TResult?> ActiveMin<TKey, TValue, TResult>(this IReadOnlyDictionary<TKey, TValue> source, Func<TKey, TValue, TResult?> selector, bool isThreadSafe = false, params string[] selectorProperties) where TResult : struct, IComparable<TResult>
        {
            var selectorValues = source is SortedDictionary<TKey, TValue> || source is ObservableSortedDictionary<TKey, TValue> || source is SynchronizedObservableSortedDictionary<TKey, TValue> ? (IDictionary<TKey, TResult?>)new SortedDictionary<TKey, TResult?>() : new Dictionary<TKey, TResult?>();
            var firstIsValid = false;
            TResult? firstMin = default;
            if (source.Count > 0)
            {
                firstIsValid = true;
                var firstKeyValuePair = source.First();
                var firstKey = firstKeyValuePair.Key;
                firstMin = selector(firstKey, firstKeyValuePair.Value);
                selectorValues.Add(firstKey, firstMin);
                foreach (var keyValuePair in source.Skip(1))
                {
                    var key = keyValuePair.Key;
                    var selectorValue = selector(key, keyValuePair.Value);
                    if (selectorValue != null && (firstMin == null || selectorValue.Value.CompareTo(firstMin.Value) < 0))
                        firstMin = selectorValue;
                    selectorValues.Add(key, selectorValue);
                }
            }
            var monitor = ActiveDictionaryMonitor<TKey, TValue>.Monitor(source, selectorProperties);
            EventHandler<NotifyDictionaryValueEventArgs<TKey, TValue>> valueAdded = null;
            EventHandler<ValuePropertyChangeEventArgs<TKey, TValue>> valuePropertyChanged = null;
            EventHandler<NotifyDictionaryValueEventArgs<TKey, TValue>> valueRemoved = null;
            EventHandler<NotifyDictionaryValueReplacedEventArgs<TKey, TValue>> valueReplaced = null;
            EventHandler<NotifyDictionaryValuesEventArgs<TKey, TValue>> valuesAdded = null;
            EventHandler<NotifyDictionaryValuesEventArgs<TKey, TValue>> valuesRemoved = null;
            var resultAccess = isThreadSafe ? new object() : null;
            var result = new ActiveAggregateValue<TResult?>(firstIsValid, firstMin, out var setValidity, out var setValue, disposing =>
            {
                monitor.ValueAdded -= valueAdded;
                monitor.ValuePropertyChanged -= valuePropertyChanged;
                monitor.ValueRemoved -= valueRemoved;
                monitor.ValueReplaced -= valueReplaced;
                monitor.ValuesAdded -= valuesAdded;
                monitor.ValuesRemoved -= valuesRemoved;
                if (disposing)
                    monitor.Dispose();
            });

            void valueAddedLogic(NotifyDictionaryValueEventArgs<TKey, TValue> e)
            {
                var key = e.Key;
                if (selectorValues.Count == 0)
                {
                    var selectorValue = selector(key, e.Value);
                    selectorValues.Add(key, selectorValue);
                    setValidity(true);
                    setValue(selectorValue);
                }
                else
                {
                    var currentMin = result.Value;
                    var selectorValue = selector(key, e.Value);
                    selectorValues.Add(key, selectorValue);
                    if (selectorValue != null && (currentMin == null || selectorValue.Value.CompareTo(currentMin.Value) < 0))
                        currentMin = selectorValue;
                    if (((currentMin == null) != (result.Value == null)) || (currentMin != null && result.Value != null && currentMin.Value.CompareTo(result.Value.Value) < 0))
                        setValue(currentMin);
                }
            }

            void valuePropertyChangedLogic(ValuePropertyChangeEventArgs<TKey, TValue> e)
            {
                var key = e.Key;
                var previousSelectorValue = selectorValues[key];
                var newSelectorValue = selector(key, e.Value);
                selectorValues[key] = newSelectorValue;
                var currentMin = result.Value;
                if (newSelectorValue != null && (currentMin == null || newSelectorValue.Value.CompareTo(currentMin.Value) < 0))
                    setValue(newSelectorValue);
                else if (previousSelectorValue != null && currentMin != null && previousSelectorValue.Value.CompareTo(currentMin.Value) == 0 && (newSelectorValue == null || newSelectorValue.Value.CompareTo(previousSelectorValue.Value) > 0))
                    setValue(selectorValues.Values.Min());
            }

            void valueRemovedLogic(NotifyDictionaryValueEventArgs<TKey, TValue> e)
            {
                if (selectorValues.Count == 1)
                {
                    selectorValues = source is SortedDictionary<TKey, TValue> || source is ObservableSortedDictionary<TKey, TValue> || source is SynchronizedObservableSortedDictionary<TKey, TValue> ? (IDictionary<TKey, TResult?>)new SortedDictionary<TKey, TResult?>() : new Dictionary<TKey, TResult?>();
                    setValidity(false);
                }
                else
                {
                    var currentMin = result.Value;
                    var minRemoved = false;
                    var key = e.Key;
                    if (selectorValues.TryGetValue(key, out var selectorValue))
                    {
                        if (currentMin != null && selectorValue != null && selectorValue.Value.CompareTo(currentMin.Value) == 0)
                            minRemoved = true;
                        selectorValues.Remove(key);
                    }
                    if (minRemoved)
                        setValue(selectorValues.Values.Min());
                }
            }

            void valueReplacedLogic(NotifyDictionaryValueReplacedEventArgs<TKey, TValue> e)
            {
                var key = e.Key;
                var previousSelectorValue = selectorValues[key];
                var currentSelectorValue = selector(key, e.NewValue);
                selectorValues[key] = currentSelectorValue;
                var previousToCurrent = (previousSelectorValue ?? default).CompareTo(currentSelectorValue ?? default);
                if (currentSelectorValue != null && (previousSelectorValue == null || previousToCurrent > 0) && (result.Value == null || currentSelectorValue.Value.CompareTo(result.Value.Value) < 0))
                    setValue(currentSelectorValue);
                else if (previousSelectorValue != null && (currentSelectorValue == null || (previousToCurrent < 0 && previousSelectorValue.Value.CompareTo(result.Value.Value) == 0)))
                    setValue(selectorValues.Values.Min());
            }

            void valuesAddedLogic(NotifyDictionaryValuesEventArgs<TKey, TValue> e)
            {
                if (selectorValues.Count == 0)
                {
                    var firstKeyValuePair = source.First();
                    var firstKey = firstKeyValuePair.Key;
                    var currentMin = selector(firstKey, firstKeyValuePair.Value);
                    selectorValues.Add(firstKey, currentMin);
                    foreach (var keyValuePair in source.Skip(1))
                    {
                        var key = keyValuePair.Key;
                        var selectorValue = selector(key, keyValuePair.Value);
                        if (selectorValue != null && (currentMin == null || selectorValue.Value.CompareTo(currentMin.Value) < 0))
                            currentMin = selectorValue;
                        selectorValues.Add(key, selectorValue);
                    }
                    setValidity(true);
                    setValue(currentMin);
                }
                else
                {
                    var currentMin = result.Value;
                    foreach (var keyValuePair in e.KeyValuePairs)
                    {
                        var key = keyValuePair.Key;
                        var selectorValue = selector(key, keyValuePair.Value);
                        selectorValues.Add(key, selectorValue);
                        if (selectorValue != null && (currentMin == null || selectorValue.Value.CompareTo(currentMin.Value) < 0))
                            currentMin = selectorValue;
                    }
                    if (((currentMin == null) != (result.Value == null)) || (currentMin != null && result.Value != null && currentMin.Value.CompareTo(result.Value.Value) < 0))
                        setValue(currentMin);
                }
            }

            void valuesRemovedLogic(NotifyDictionaryValuesEventArgs<TKey, TValue> e)
            {
                if (selectorValues.Count == e.KeyValuePairs.Count)
                {
                    selectorValues = source is SortedDictionary<TKey, TValue> || source is ObservableSortedDictionary<TKey, TValue> || source is SynchronizedObservableSortedDictionary<TKey, TValue> ? (IDictionary<TKey, TResult?>)new SortedDictionary<TKey, TResult?>() : new Dictionary<TKey, TResult?>();
                    setValidity(false);
                }
                else
                {
                    var currentMin = result.Value;
                    var minRemoved = false;
                    foreach (var keyValuePair in e.KeyValuePairs)
                    {
                        var key = keyValuePair.Key;
                        if (selectorValues.TryGetValue(key, out var selectorValue))
                        {
                            if (currentMin != null && selectorValue != null && selectorValue.Value.CompareTo(currentMin.Value) == 0)
                                minRemoved = true;
                            selectorValues.Remove(key);
                        }
                    }
                    if (minRemoved)
                        setValue(selectorValues.Values.Min());
                }
            }

            if (isThreadSafe)
            {
                valueAdded = (sender, e) =>
                {
                    lock (resultAccess)
                        valueAddedLogic(e);
                };
                valuePropertyChanged = (sender, e) =>
                {
                    lock (resultAccess)
                        valuePropertyChangedLogic(e);
                };
                valueRemoved = (sender, e) =>
                {
                    lock (resultAccess)
                        valueRemovedLogic(e);
                };
                valueReplaced = (sender, e) =>
                {
                    lock (resultAccess)
                        valueReplacedLogic(e);
                };
                valuesAdded = (sender, e) =>
                {
                    lock (resultAccess)
                        valuesAddedLogic(e);
                };
                valuesRemoved = (sender, e) =>
                {
                    lock (resultAccess)
                        valuesRemovedLogic(e);
                };
            }
            else
            {
                valueAdded = (sender, e) => valueAddedLogic(e);
                valuePropertyChanged = (sender, e) => valuePropertyChangedLogic(e);
                valueRemoved = (sender, e) => valueRemovedLogic(e);
                valueReplaced = (sender, e) => valueReplacedLogic(e);
                valuesAdded = (sender, e) => valuesAddedLogic(e);
                valuesRemoved = (sender, e) => valuesRemovedLogic(e);
            }

            monitor.ValueAdded += valueAdded;
            monitor.ValuePropertyChanged += valuePropertyChanged;
            monitor.ValueRemoved += valueRemoved;
            monitor.ValueReplaced += valueReplaced;
            monitor.ValuesAdded += valuesAdded;
            monitor.ValuesRemoved += valuesRemoved;
            return result;
        }

        public static ActiveAggregateValue<TResult> ActiveMin<TSource, TResult>(this IReadOnlyList<TSource> source, Func<TSource, TResult> selector, bool isThreadSafe = false, params string[] selectorProperties) where TSource : class where TResult : IComparable<TResult>
        {
            var selectorValues = new Dictionary<object, TResult>();
            var firstIsValid = false;
            TResult firstMin = default;
            if (source.Count > 0)
            {
                firstIsValid = true;
                var firstElement = source.First();
                firstMin = selector(firstElement);
                selectorValues.Add(firstElement, firstMin);
                foreach (var element in source.Skip(1))
                {
                    var selectorValue = selector(element);
                    if (selectorValue.CompareTo(firstMin) < 0)
                        firstMin = selectorValue;
                    selectorValues.Add(element, selectorValue);
                }
            }
            var monitor = ActiveListMonitor<TSource>.Monitor(source, selectorProperties);
            EventHandler<ElementPropertyChangeEventArgs<TSource>> elementPropertyChanged = null;
            EventHandler<ElementMembershipEventArgs<TSource>> elementsAdded = null;
            EventHandler<ElementMembershipEventArgs<TSource>> elementsRemoved = null;
            var resultAccess = isThreadSafe ? new object() : null;
            var result = new ActiveAggregateValue<TResult>(firstIsValid, firstMin, out var setValidity, out var setValue, disposing =>
            {
                monitor.ElementPropertyChanged -= elementPropertyChanged;
                monitor.ElementsAdded -= elementsAdded;
                monitor.ElementsRemoved -= elementsRemoved;
                if (disposing)
                    monitor.Dispose();
            });

            void elementPropertyChangedLogic(ElementPropertyChangeEventArgs<TSource> e)
            {
                var element = e.Element;
                var previousSelectorValue = selectorValues[element];
                var newSelectorValue = selector(element);
                selectorValues[element] = newSelectorValue;
                var currentMin = result.Value;
                var comparison = newSelectorValue.CompareTo(currentMin);
                if (comparison < 0)
                    setValue(newSelectorValue);
                else if (comparison > 0 && previousSelectorValue.CompareTo(currentMin) == 0)
                    setValue(selectorValues.Values.Min());
            }

            void elementsAddedLogic(ElementMembershipEventArgs<TSource> e)
            {
                if (selectorValues.Count == 0)
                {
                    var firstElement = e.Elements.First();
                    var currentMin = selector(firstElement);
                    selectorValues.Add(firstElement, currentMin);
                    foreach (var element in e.Elements.Skip(1))
                    {
                        var selectorValue = selector(element);
                        if (selectorValue.CompareTo(currentMin) < 0)
                            currentMin = selectorValue;
                        selectorValues.Add(element, selectorValue);
                    }
                    setValidity(true);
                    setValue(currentMin);
                }
                else
                {
                    var currentMin = result.Value;
                    foreach (var element in e.Elements)
                    {
                        var selectorValue = selector(element);
                        selectorValues.Add(element, selectorValue);
                        if (selectorValue.CompareTo(currentMin) < 0)
                            currentMin = selectorValue;
                    }
                    if (currentMin.CompareTo(result.Value) < 0)
                        setValue(currentMin);
                }
            }

            void elementsRemovedLogic(ElementMembershipEventArgs<TSource> e)
            {
                if (selectorValues.Count == e.Count)
                {
                    selectorValues = new Dictionary<object, TResult>();
                    setValidity(false);
                }
                else
                {
                    var currentMin = result.Value;
                    var minRemoved = false;
                    foreach (var element in e.Elements)
                    {
                        if (selectorValues.TryGetValue(element, out var selectorValue))
                        {
                            if (selectorValue.CompareTo(currentMin) == 0)
                                minRemoved = true;
                            selectorValues.Remove(element);
                        }
                    }
                    if (minRemoved)
                        setValue(selectorValues.Values.Min());
                }
            }

            if (isThreadSafe)
            {
                elementPropertyChanged = (sender, e) =>
                {
                    lock (resultAccess)
                        elementPropertyChangedLogic(e);
                };
                elementsAdded = (sender, e) =>
                {
                    lock (resultAccess)
                        elementsAddedLogic(e);
                };
                elementsRemoved = (sender, e) =>
                {
                    lock (resultAccess)
                        elementsRemovedLogic(e);
                };
            }
            else
            {
                elementPropertyChanged = (sender, e) => elementPropertyChangedLogic(e);
                elementsAdded = (sender, e) => elementsAddedLogic(e);
                elementsRemoved = (sender, e) => elementsRemovedLogic(e);
            }

            monitor.ElementPropertyChanged += elementPropertyChanged;
            monitor.ElementsAdded += elementsAdded;
            monitor.ElementsRemoved += elementsRemoved;
            return result;
        }

        public static ActiveAggregateValue<TResult?> ActiveMin<TSource, TResult>(this IReadOnlyList<TSource> source, Func<TSource, TResult?> selector, bool isThreadSafe = false, params string[] selectorProperties) where TSource : class where TResult : struct, IComparable<TResult>
        {
            var selectorValues = new Dictionary<object, TResult?>();
            var firstIsValid = false;
            TResult? firstMin = default;
            if (source.Count > 0)
            {
                firstIsValid = true;
                var firstElement = source.First();
                firstMin = selector(firstElement);
                selectorValues.Add(firstElement, firstMin);
                foreach (var element in source.Skip(1))
                {
                    var selectorValue = selector(element);
                    if (selectorValue != null && (firstMin == null || selectorValue.Value.CompareTo(firstMin.Value) < 0))
                        firstMin = selectorValue;
                    selectorValues.Add(element, selectorValue);
                }
            }
            var monitor = ActiveListMonitor<TSource>.Monitor(source, selectorProperties);
            EventHandler<ElementPropertyChangeEventArgs<TSource>> elementPropertyChanged = null;
            EventHandler<ElementMembershipEventArgs<TSource>> elementsAdded = null;
            EventHandler<ElementMembershipEventArgs<TSource>> elementsRemoved = null;
            var resultAccess = isThreadSafe ? new object() : null;
            var result = new ActiveAggregateValue<TResult?>(firstIsValid, firstMin, out var setValidity, out var setValue, disposing =>
            {
                monitor.ElementPropertyChanged -= elementPropertyChanged;
                monitor.ElementsAdded -= elementsAdded;
                monitor.ElementsRemoved -= elementsRemoved;
                if (disposing)
                    monitor.Dispose();
            });

            void elementPropertyChangedLogic(ElementPropertyChangeEventArgs<TSource> e)
            {
                var element = e.Element;
                var previousSelectorValue = selectorValues[element];
                var newSelectorValue = selector(element);
                selectorValues[element] = newSelectorValue;
                var currentMin = result.Value;
                if (newSelectorValue != null && (currentMin == null || newSelectorValue.Value.CompareTo(currentMin.Value) < 0))
                    setValue(newSelectorValue);
                else if (previousSelectorValue != null && currentMin != null && previousSelectorValue.Value.CompareTo(currentMin.Value) == 0 && (newSelectorValue == null || newSelectorValue.Value.CompareTo(previousSelectorValue.Value) > 0))
                    setValue(selectorValues.Values.Min());
            }

            void elementsAddedLogic(ElementMembershipEventArgs<TSource> e)
            {
                if (selectorValues.Count == 0)
                {
                    var firstElement = e.Elements.First();
                    var currentMin = selector(firstElement);
                    selectorValues.Add(firstElement, currentMin);
                    foreach (var element in e.Elements.Skip(1))
                    {
                        var selectorValue = selector(element);
                        if (selectorValue != null && (currentMin == null || selectorValue.Value.CompareTo(currentMin.Value) < 0))
                            currentMin = selectorValue;
                        selectorValues.Add(element, selectorValue);
                    }
                    setValidity(true);
                    setValue(currentMin);
                }
                else
                {
                    var currentMin = result.Value;
                    foreach (var element in e.Elements)
                    {
                        var selectorValue = selector(element);
                        selectorValues.Add(element, selectorValue);
                        if (selectorValue != null && (currentMin == null || selectorValue.Value.CompareTo(currentMin.Value) < 0))
                            currentMin = selectorValue;
                    }
                    if (((currentMin == null) != (result.Value == null)) || (currentMin != null && result.Value != null && currentMin.Value.CompareTo(result.Value.Value) != 0))
                        setValue(currentMin);
                }
            }

            void elementsRemovedLogic(ElementMembershipEventArgs<TSource> e)
            {
                if (selectorValues.Count == e.Count)
                {
                    selectorValues = new Dictionary<object, TResult?>();
                    setValidity(false);
                }
                else
                {
                    var currentMin = result.Value;
                    var minRemoved = false;
                    foreach (var element in e.Elements)
                    {
                        if (selectorValues.TryGetValue(element, out var selectorValue))
                        {
                            if (currentMin != null && selectorValue != null && selectorValue.Value.CompareTo(currentMin.Value) == 0)
                                minRemoved = true;
                            selectorValues.Remove(element);
                        }
                    }
                    if (minRemoved)
                        setValue(selectorValues.Values.Min());
                }
            }

            if (isThreadSafe)
            {
                elementPropertyChanged = (sender, e) =>
                {
                    lock (resultAccess)
                        elementPropertyChangedLogic(e);
                };
                elementsAdded = (sender, e) =>
                {
                    lock (resultAccess)
                        elementsAddedLogic(e);
                };
                elementsRemoved = (sender, e) =>
                {
                    lock (resultAccess)
                        elementsRemovedLogic(e);
                };
            }
            else
            {
                elementPropertyChanged = (sender, e) => elementPropertyChangedLogic(e);
                elementsAdded = (sender, e) => elementsAddedLogic(e);
                elementsRemoved = (sender, e) => elementsRemovedLogic(e);
            }

            monitor.ElementPropertyChanged += elementPropertyChanged;
            monitor.ElementsAdded += elementsAdded;
            monitor.ElementsRemoved += elementsRemoved;
            return result;
        }

        public static ActiveEnumerable<TSource> ActiveOrderBy<TSource>(this IReadOnlyList<TSource> source, Func<TSource, IComparable> ascendingOrderSelector, IEnumerable<string> ascendingSelectorProperties = null, bool indexed = false) where TSource : class =>
            ActiveOrderBy(source, (source as ISynchronizable)?.SynchronizationContext, ascendingOrderSelector, ascendingSelectorProperties, indexed);

        public static ActiveEnumerable<TSource> ActiveOrderBy<TSource>(this IReadOnlyList<TSource> source, SynchronizationContext synchronizationContext, Func<TSource, IComparable> ascendingOrderSelector, IEnumerable<string> ascendingSelectorProperties = null, bool indexed = false) where TSource : class =>
            ActiveOrderBy(source, synchronizationContext, new Func<TSource, IComparable>[] { ascendingOrderSelector }, ascendingSelectorProperties, indexed);

        public static ActiveEnumerable<TSource> ActiveOrderBy<TSource>(this IReadOnlyList<TSource> source, IEnumerable<Func<TSource, IComparable>> ascendingOrderSelectors, IEnumerable<string> ascendingSelectorsProperties = null, bool indexed = false) where TSource : class =>
            ActiveOrderBy(source, (source as ISynchronizable)?.SynchronizationContext, ascendingOrderSelectors, ascendingSelectorsProperties, indexed);

        public static ActiveEnumerable<TSource> ActiveOrderBy<TSource>(this IReadOnlyList<TSource> source, SynchronizationContext synchronizationContext, IEnumerable<Func<TSource, IComparable>> ascendingOrderSelectors, IEnumerable<string> ascendingSelectorsProperties = null, bool indexed = false) where TSource : class =>
            ActiveOrderBy(source, synchronizationContext, ascendingOrderSelectors.Select(aos => new ActiveOrderingDescriptor<TSource>(aos, false)), ascendingSelectorsProperties, indexed);

        public static ActiveEnumerable<TSource> ActiveOrderBy<TSource>(this IReadOnlyList<TSource> source, ActiveOrderingDescriptor<TSource> orderingDescriptor, IEnumerable<string> selectorProperties = null, bool indexed = false) where TSource : class =>
            ActiveOrderBy(source, (source as ISynchronizable)?.SynchronizationContext, orderingDescriptor, selectorProperties, indexed);

        public static ActiveEnumerable<TSource> ActiveOrderBy<TSource>(this IReadOnlyList<TSource> source, SynchronizationContext synchronizationContext, ActiveOrderingDescriptor<TSource> orderingDescriptor, IEnumerable<string> selectorProperties = null, bool indexed = false) where TSource : class =>
            ActiveOrderBy(source, synchronizationContext, new ActiveOrderingDescriptor<TSource>[] { orderingDescriptor }, selectorProperties, indexed);

        public static ActiveEnumerable<TSource> ActiveOrderBy<TSource>(this IReadOnlyList<TSource> source, IEnumerable<ActiveOrderingDescriptor<TSource>> orderingDescriptors, IEnumerable<string> selectorsProperties = null, bool indexed = false) where TSource : class =>
            ActiveOrderBy(source, (source as ISynchronizable)?.SynchronizationContext, orderingDescriptors, selectorsProperties, indexed);

        public static ActiveEnumerable<TSource> ActiveOrderBy<TSource>(this IReadOnlyList<TSource> source, SynchronizationContext synchronizationContext, IEnumerable<ActiveOrderingDescriptor<TSource>> orderingDescriptors, IEnumerable<string> selectorsProperties = null, bool indexed = false) where TSource : class
        {
            var comparer = new ActiveOrderingComparer<TSource>(orderingDescriptors);
            var sortedSource = source.ToList();
            sortedSource.Sort(comparer);
            Dictionary<TSource, int> sortingIndicies = null;

            void rebuildSortingIndicies(IReadOnlyList<TSource> fromSort)
            {
                sortingIndicies = new Dictionary<TSource, int>();
                for (var i = 0; i < fromSort.Count; ++i)
                    sortingIndicies.Add(fromSort[i], i);
            }

            if (indexed)
                rebuildSortingIndicies(sortedSource);
            var rangeObservableCollection = new SynchronizedRangeObservableCollection<TSource>(synchronizationContext, sortedSource, false);
            var rangeObservableCollectionAccess = new object();

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

            var monitor = ActiveListMonitor<TSource>.Monitor(source, selectorsProperties == null ? new string[0] : selectorsProperties.ToArray());

            void elementPropertyChangedHandler(object sender, ElementPropertyChangeEventArgs<TSource> e)
            {
                lock (rangeObservableCollectionAccess)
                    rangeObservableCollection.Execute(() => repositionElement(e.Element));
            }

            void elementsAddedHandler(object sender, ElementMembershipEventArgs<TSource> e)
            {
                lock (rangeObservableCollectionAccess)
                    rangeObservableCollection.Execute(() =>
                    {
                        {
                            if (rangeObservableCollection.Count == 0)
                            {
                                var sorted = e.Elements.ToList();
                                sorted.Sort(comparer);
                                if (indexed)
                                    rebuildSortingIndicies(sorted);
                                rangeObservableCollection.Reset(sorted);
                            }
                            else
                                foreach (var element in e.Elements)
                                {
                                    var position = 0;
                                    while (position < rangeObservableCollection.Count && comparer.Compare(element, rangeObservableCollection[position]) >= 0)
                                        ++position;
                                    var insertionPosition = position;
                                    if (indexed)
                                    {
                                        while (position < rangeObservableCollection.Count)
                                        {
                                            sortingIndicies[rangeObservableCollection[position]] = position + 1;
                                            ++position;
                                        }
                                        sortingIndicies.Add(element, insertionPosition);
                                    }
                                    rangeObservableCollection.Insert(insertionPosition, element);
                                }
                        }
                    });
            }

            void elementsRemovedHandler(object sender, ElementMembershipEventArgs<TSource> e)
            {
                lock (rangeObservableCollectionAccess)
                    rangeObservableCollection.Execute(() =>
                    {
                        if (rangeObservableCollection.Count == e.Count)
                        {
                            if (indexed)
                                sortingIndicies = new Dictionary<TSource, int>();
                            rangeObservableCollection.Clear();
                        }
                        else
                        {
                            var elementsRemovedByIndex = new List<(TSource element, int index)>();
                            if (indexed)
                                foreach (var element in e.Elements)
                                    elementsRemovedByIndex.Add((element, sortingIndicies.TryGetValue(element, out var index) ? index : -1));
                            else
                                foreach (var element in e.Elements)
                                    elementsRemovedByIndex.Add((element, rangeObservableCollection.IndexOf(element)));
                            var elementsRemovedByIndexSorted = elementsRemovedByIndex.Where(ie => ie.index >= 0).OrderByDescending(ie => ie.index).ToList();
                            if (indexed)
                                foreach (var (element, index) in elementsRemovedByIndexSorted)
                                {
                                    rangeObservableCollection.RemoveAt(index);
                                    sortingIndicies.Remove(element);
                                }
                            else
                                foreach (var (element, index) in elementsRemovedByIndexSorted)
                                    rangeObservableCollection.RemoveAt(index);
                            if (indexed && elementsRemovedByIndexSorted.Any())
                            {
                                for (int i = elementsRemovedByIndexSorted.Last().index, ii = rangeObservableCollection.Count; i < ii; ++i)
                                    sortingIndicies[rangeObservableCollection[i]] = i;
                            }
                        }
                    });
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

        public static ActiveEnumerable<TResult> ActiveSelect<TSource, TResult>(this IReadOnlyList<TSource> source, Func<TSource, TResult> selector, Action<TResult> releaser = null, Action<TSource, string, TResult> updater = null, IndexingStrategy indexingStrategy = IndexingStrategy.NoneOrInherit, params string[] selectorProperties) where TSource : class =>
            ActiveSelect(source, (source as ISynchronizable)?.SynchronizationContext, selector, releaser, updater, indexingStrategy, selectorProperties);

        public static ActiveEnumerable<TResult> ActiveSelect<TSource, TResult>(this IReadOnlyList<TSource> source, SynchronizationContext synchronizationContext, Func<TSource, TResult> selector, Action<TResult> releaser = null, Action<TSource, string, TResult> updater = null, IndexingStrategy indexingStrategy = IndexingStrategy.NoneOrInherit, params string[] selectorProperties) where TSource : class
        {
            IDictionary<TSource, int> sourceToIndex;
            switch (indexingStrategy)
            {
                case IndexingStrategy.HashTable:
                    sourceToIndex = new Dictionary<TSource, int>();
                    break;
                case IndexingStrategy.SelfBalancingBinarySearchTree:
                    sourceToIndex = new SortedDictionary<TSource, int>();
                    break;
                default:
                    sourceToIndex = null;
                    break;
            }
            var rangeObservableCollection = new SynchronizedRangeObservableCollection<TResult>(synchronizationContext, indexingStrategy != IndexingStrategy.NoneOrInherit ? source.Select((element, index) =>
            {
                sourceToIndex.Add(element, index);
                return selector(element);
            }) : source.Select(element => selector(element)), false);
            var rangeObservableCollectionAccess = new object();
            var monitor = ActiveListMonitor<TSource>.Monitor(source, selectorProperties);

            void elementPropertyChangedHandler(object sender, ElementPropertyChangeEventArgs<TSource> e)
            {
                var sourceElement = e.Element;
                TResult targetElement;
                lock (rangeObservableCollectionAccess)
                {
                    var index = indexingStrategy != IndexingStrategy.NoneOrInherit ? sourceToIndex[sourceElement] : source.IndexOf(sourceElement);
                    if (updater == null)
                        targetElement = rangeObservableCollection.Replace(index, selector(sourceElement));
                    else
                        targetElement = rangeObservableCollection[index];
                }
                if (updater == null)
                    releaser?.Invoke(targetElement);
                else
                    updater(e.Element, e.PropertyName, targetElement);
            }

            void elementsAddedHandler(object sender, ElementMembershipEventArgs<TSource> e)
            {
                lock (rangeObservableCollectionAccess)
                {
                    if (indexingStrategy != IndexingStrategy.NoneOrInherit)
                    {
                        var index = e.Index - 1;
                        foreach (var element in e.Elements)
                            sourceToIndex.Add(element, ++index);
                        for (var i = e.Index + e.Count; i < source.Count; ++i)
                            sourceToIndex[source[i]] = i;
                    }
                    rangeObservableCollection.InsertRange(e.Index, e.Elements.Select(selector));
                }
            }

            void elementsMovedHandler(object sender, ElementsMovedEventArgs<TSource> e)
            {
                lock (rangeObservableCollectionAccess)
                {
                    if (indexingStrategy != IndexingStrategy.NoneOrInherit)
                    {
                        var endIndex = (e.FromIndex > e.ToIndex ? e.FromIndex : e.ToIndex) + e.Count;
                        for (var i = e.FromIndex < e.ToIndex ? e.FromIndex : e.ToIndex; i < endIndex; ++i)
                            sourceToIndex[source[i]] = i;
                    }
                    rangeObservableCollection.MoveRange(e.FromIndex, e.ToIndex, e.Count);
                }
            }

            void elementsRemovedHandler(object sender, ElementMembershipEventArgs<TSource> e)
            {
                IReadOnlyList<TResult> removedItems;
                lock (rangeObservableCollectionAccess)
                {
                    removedItems = rangeObservableCollection.ReplaceRange(e.Index, e.Count);
                    if (indexingStrategy != IndexingStrategy.NoneOrInherit)
                        for (var i = e.Index; i < source.Count; ++i)
                            sourceToIndex[source[i]] = i;
                }
                if (releaser != null)
                    foreach (var removedItem in removedItems)
                        releaser(removedItem);
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

        public static ActiveEnumerable<TResult> ActiveSelectMany<TSource, TResult>(this IReadOnlyList<TSource> source, Func<TSource, IEnumerable<TResult>> selector, Action<TResult> releaser = null, Action<TSource, string, IList<TResult>> updater = null, params string[] selectorProperties) where TSource : class =>
            ActiveSelectMany(source, (source as ISynchronizable)?.SynchronizationContext, selector, releaser, updater, selectorProperties);

        public static ActiveEnumerable<TResult> ActiveSelectMany<TSource, TResult>(this IReadOnlyList<TSource> source, SynchronizationContext synchronizationContext, Func<TSource, IEnumerable<TResult>> selector, Action<TResult> releaser = null, Action<TSource, string, IList<TResult>> updater = null, params string[] selectorProperties) where TSource : class
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
            var rangeObservableCollectionAccess = new object();
            var monitor = ActiveListMonitor<TSource>.Monitor(source, selectorProperties);

            void elementPropertyChangedHandler(object sender, ElementPropertyChangeEventArgs<TSource> e)
            {
                IEnumerable<TResult> releasingItems = null;
                lock (rangeObservableCollectionAccess)
                {
                    var sourceElement = e.Element;
                    if (!sourceToSourceIndex.TryGetValue(sourceElement, out var sourceIndex))
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
                        rangeObservableCollection.ReplaceRange(sourceToResultsIndex[sourceElement], pastResultsList.Count, newResults);
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
                            rangeObservableCollection.ReplaceRange(sourceToResultsIndex[sourceElement], pastResults.Count, pastResultsList);
                            if (releaser != null)
                            {
                                var removedItems = pastResults.Except(pastResultsList);
                                if (removedItems.Any())
                                    releasingItems = removedItems;
                            }
                        }
                    }
                }
                if (releasingItems != null)
                    foreach (var releasingItem in releasingItems)
                        releaser(releasingItem);
            }

            void elementsAddedHandler(object sender, ElementMembershipEventArgs<TSource> e)
            {
                lock (rangeObservableCollectionAccess)
                {
                    var resultsIndex = 0;
                    if (sourceToResultsIndex.Count > 0)
                    {
                        if (e.Index == sourceToResultsIndex.Count)
                            resultsIndex = rangeObservableCollection.Count;
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
                    rangeObservableCollection.InsertRange(resultsIndex, resultsAdded);
                }
            }

            void elementsMovedHandler(object sender, ElementsMovedEventArgs<TSource> e)
            {
                lock (rangeObservableCollectionAccess)
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
                    rangeObservableCollection.MoveRange(resultFromIndex, resultToIndex, e.Elements.Sum(element => selectedResults[element].Count));
                }
            }

            void elementsRemovedHandler(object sender, ElementMembershipEventArgs<TSource> e)
            {
                IEnumerable<TResult> releasingItems = null;
                lock (rangeObservableCollectionAccess)
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
                    rangeObservableCollection.RemoveRange(resultIndex, removedItems.Count);
                    if (releaser != null && removedItems.Count > 0)
                        releasingItems = removedItems;
                }
                if (releasingItems != null)
                    foreach (var releasingItem in releasingItems)
                        releaser(releasingItem);
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

        public static ActiveAggregateValue<TResult> ActiveSum<TKey, TValue, TResult>(this IReadOnlyDictionary<TKey, TValue> source, Func<TKey, TValue, TResult> selector, bool isThreadSafe = false, params string[] selectorProperties) where TResult : IEquatable<TResult>
        {
            var operations = new GenericOperations<TResult>();
            var selectorValues = source is SortedDictionary<TKey, TValue> || source is ObservableSortedDictionary<TKey, TValue> || source is SynchronizedObservableSortedDictionary<TKey, TValue> ? (IDictionary<TKey, TResult>)new SortedDictionary<TKey, TResult>() : new Dictionary<TKey, TResult>();
            TResult firstSum = default;
            var monitor = ActiveDictionaryMonitor<TKey, TValue>.Monitor(source, selectorProperties);
            if (monitor.ValuesNotifyChanging)
                foreach (var keyValuePair in source)
                    firstSum = operations.Add(firstSum, selector(keyValuePair.Key, keyValuePair.Value));
            else
                foreach (var keyValuePair in source)
                {
                    var selectorValue = selector(keyValuePair.Key, keyValuePair.Value);
                    firstSum = operations.Add(firstSum, selectorValue);
                    selectorValues.Add(keyValuePair.Key, selectorValue);
                }
            EventHandler<NotifyDictionaryValueEventArgs<TKey, TValue>> valueAddedHandler = null;
            EventHandler<ValuePropertyChangeEventArgs<TKey, TValue>> valuePropertyChangedHandler = null;
            EventHandler<ValuePropertyChangeEventArgs<TKey, TValue>> valuePropertyChangingHandler = null;
            EventHandler<NotifyDictionaryValueEventArgs<TKey, TValue>> valueRemovedHandler = null;
            EventHandler<NotifyDictionaryValueReplacedEventArgs<TKey, TValue>> valueReplacedHandler = null;
            EventHandler<NotifyDictionaryValuesEventArgs<TKey, TValue>> valuesAddedHandler = null;
            EventHandler<NotifyDictionaryValuesEventArgs<TKey, TValue>> valuesRemovedHandler = null;
            var resultAccess = isThreadSafe ? new object() : null;
            var result = new ActiveAggregateValue<TResult>(true, firstSum, out var setValidity, out var setValue, disposing =>
            {
                monitor.ValueAdded -= valueAddedHandler;
                monitor.ValuePropertyChanged -= valuePropertyChangedHandler;
                monitor.ValuePropertyChanging -= valuePropertyChangingHandler;
                monitor.ValueRemoved -= valueRemovedHandler;
                monitor.ValueReplaced -= valueReplacedHandler;
                monitor.ValuesAdded -= valuesAddedHandler;
                monitor.ValuesRemoved -= valuesRemovedHandler;
                if (disposing)
                    monitor.Dispose();
            });

            Action<NotifyDictionaryValueEventArgs<TKey, TValue>> valueAddedLogic;
            if (monitor.ValuesNotifyChanging)
                valueAddedLogic = e => setValue(operations.Add(result.Value, selector(e.Key, e.Value)));
            else
                valueAddedLogic = e =>
                {
                    var key = e.Key;
                    var selectorValue = selector(key, e.Value);
                    selectorValues.Add(key, selectorValue);
                    setValue(operations.Add(result.Value, selectorValue));
                };

            Action<ValuePropertyChangeEventArgs<TKey, TValue>> valuePropertyChangedLogic;
            if (monitor.ValuesNotifyChanging)
                valuePropertyChangedLogic = e =>
                {
                    var key = e.Key;
                    var previousSelectorValue = selectorValues[key];
                    var newSelectorValue = selector(key, e.Value);
                    if (!previousSelectorValue.Equals(newSelectorValue))
                        setValue(operations.Add(result.Value, operations.Subtract(newSelectorValue, previousSelectorValue)));
                    selectorValues.Remove(key);
                };
            else
                valuePropertyChangedLogic = e =>
                {
                    var key = e.Key;
                    var previousSelectorValue = selectorValues[key];
                    var newSelectorValue = selector(key, e.Value);
                    if (!previousSelectorValue.Equals(newSelectorValue))
                    {
                        selectorValues[key] = newSelectorValue;
                        setValue(operations.Add(result.Value, operations.Subtract(newSelectorValue, previousSelectorValue)));
                    }
                };

            void valuePropertyChangingLogic(ValuePropertyChangeEventArgs<TKey, TValue> e)
            {
                var key = e.Key;
                var selectorValue = selector(key, e.Value);
                if (selectorValues.ContainsKey(key))
                    selectorValues[key] = selectorValue;
                else
                    selectorValues.Add(key, selectorValue);
            }

            Action<NotifyDictionaryValueEventArgs<TKey, TValue>> valueRemovedLogic;
            if (monitor.ValuesNotifyChanging)
                valueRemovedLogic = e => setValue(operations.Subtract(result.Value, selector(e.Key, e.Value)));
            else
                valueRemovedLogic = e =>
                {
                    var key = e.Key;
                    setValue(operations.Subtract(result.Value, selectorValues[key]));
                    selectorValues.Remove(key);
                };

            Action<NotifyDictionaryValueReplacedEventArgs<TKey, TValue>> valueReplacedLogic;
            if (monitor.ValuesNotifyChanging)
                valueReplacedLogic = e =>
                {
                    var key = e.Key;
                    selectorValues.Remove(key);
                    setValue(operations.Add(result.Value, operations.Subtract(selector(key, e.NewValue), selector(key, e.OldValue))));
                };
            else
                valueReplacedLogic = e =>
                {
                    var key = e.Key;
                    var newSelectorValue = selector(key, e.NewValue);
                    selectorValues[key] = newSelectorValue;
                    setValue(operations.Add(result.Value, operations.Subtract(newSelectorValue, selector(key, e.OldValue))));
                };

            Action<NotifyDictionaryValuesEventArgs<TKey, TValue>> valuesAddedLogic;
            if (monitor.ValuesNotifyChanging)
                valuesAddedLogic = e =>
                {
                    TResult delta = default;
                    foreach (var keyValuePair in e.KeyValuePairs)
                        delta = operations.Add(delta, selector(keyValuePair.Key, keyValuePair.Value));
                    setValue(operations.Add(result.Value, delta));
                };
            else
                valuesAddedLogic = e =>
                {
                    TResult delta = default;
                    foreach (var keyValuePair in e.KeyValuePairs)
                    {
                        var key = keyValuePair.Key;
                        var selectorValue = selector(key, keyValuePair.Value);
                        selectorValues.Add(key, selectorValue);
                        delta = operations.Add(delta, selectorValue);
                    }
                    setValue(operations.Add(result.Value, delta));
                };

            Action<NotifyDictionaryValuesEventArgs<TKey, TValue>> valuesRemovedLogic;
            if (monitor.ValuesNotifyChanging)
                valuesRemovedLogic = e =>
                {
                    TResult delta = default;
                    foreach (var keyValuePair in e.KeyValuePairs)
                        delta = operations.Subtract(delta, selector(keyValuePair.Key, keyValuePair.Value));
                    setValue(operations.Add(result.Value, delta));
                };
            else
                valuesRemovedLogic = e =>
                {
                    TResult delta = default;
                    foreach (var keyValuePair in e.KeyValuePairs)
                    {
                        var key = keyValuePair.Key;
                        delta = operations.Subtract(delta, selectorValues[key]);
                        selectorValues.Remove(key);
                    }
                    setValue(operations.Add(result.Value, delta));
                };

            if (isThreadSafe)
            {
                valueAddedHandler = (sender, e) =>
                {
                    lock (resultAccess)
                        valueAddedLogic(e);
                };
                valuePropertyChangedHandler = (sender, e) =>
                {
                    lock (resultAccess)
                        valuePropertyChangedLogic(e);
                };
                valuePropertyChangingHandler = (sender, e) =>
                {
                    lock (resultAccess)
                        valuePropertyChangingLogic(e);
                };
                valueRemovedHandler = (sender, e) =>
                {
                    lock (resultAccess)
                        valueRemovedLogic(e);
                };
                valueReplacedHandler = (sender, e) =>
                {
                    lock (resultAccess)
                        valueReplacedLogic(e);
                };
                valuesAddedHandler = (sender, e) =>
                {
                    lock (resultAccess)
                        valuesAddedLogic(e);
                };
                valuesRemovedHandler = (sender, e) =>
                {
                    lock (resultAccess)
                        valuesRemovedLogic(e);
                };
            }
            else
            {
                valueAddedHandler = (sender, e) => valueAddedLogic(e);
                valuePropertyChangedHandler = (sender, e) => valuePropertyChangedLogic(e);
                valuePropertyChangingHandler = (sender, e) => valuePropertyChangingLogic(e);
                valueRemovedHandler = (sender, e) => valueRemovedLogic(e);
                valueReplacedHandler = (sender, e) => valueReplacedLogic(e);
                valuesAddedHandler = (sender, e) => valuesAddedLogic(e);
                valuesRemovedHandler = (sender, e) => valuesRemovedLogic(e);
            }

            monitor.ValueAdded += valueAddedHandler;
            monitor.ValuePropertyChanged += valuePropertyChangedHandler;
            monitor.ValuePropertyChanging += valuePropertyChangingHandler;
            monitor.ValueRemoved += valueRemovedHandler;
            monitor.ValueReplaced += valueReplacedHandler;
            monitor.ValuesAdded += valuesAddedHandler;
            monitor.ValuesRemoved += valuesRemovedHandler;
            return result;
        }

        public static ActiveAggregateValue<TResult?> ActiveSum<TKey, TValue, TResult>(this IReadOnlyDictionary<TKey, TValue> source, Func<TKey, TValue, TResult?> selector, bool isThreadSafe = false, params string[] selectorProperties) where TResult : struct
        {
            var operations = new GenericOperations<TResult>();
            var selectorValues = source is SortedDictionary<TKey, TValue> || source is ObservableSortedDictionary<TKey, TValue> || source is SynchronizedObservableSortedDictionary<TKey, TValue> ? (IDictionary<TKey, TResult?>)new SortedDictionary<TKey, TResult?>() : new Dictionary<TKey, TResult?>();
            TResult firstSum = default;
            var monitor = ActiveDictionaryMonitor<TKey, TValue>.Monitor(source, selectorProperties);
            if (monitor.ValuesNotifyChanging)
                foreach (var keyValuePair in source)
                    firstSum = operations.Add(firstSum, selector(keyValuePair.Key, keyValuePair.Value) ?? default);
            else
                foreach (var keyValuePair in source)
                {
                    var selectorValue = selector(keyValuePair.Key, keyValuePair.Value);
                    firstSum = operations.Add(firstSum, selectorValue ?? default);
                    selectorValues.Add(keyValuePair.Key, selectorValue);
                }
            EventHandler<NotifyDictionaryValueEventArgs<TKey, TValue>> valueAddedHandler = null;
            EventHandler<ValuePropertyChangeEventArgs<TKey, TValue>> valuePropertyChangedHandler = null;
            EventHandler<ValuePropertyChangeEventArgs<TKey, TValue>> valuePropertyChangingHandler = null;
            EventHandler<NotifyDictionaryValueEventArgs<TKey, TValue>> valueRemovedHandler = null;
            EventHandler<NotifyDictionaryValueReplacedEventArgs<TKey, TValue>> valueReplacedHandler = null;
            EventHandler<NotifyDictionaryValuesEventArgs<TKey, TValue>> valuesAddedHandler = null;
            EventHandler<NotifyDictionaryValuesEventArgs<TKey, TValue>> valuesRemovedHandler = null;
            var resultAccess = isThreadSafe ? new object() : null;
            var result = new ActiveAggregateValue<TResult?>(true, firstSum, out var setValidity, out var setValue, disposing =>
            {
                monitor.ValueAdded -= valueAddedHandler;
                monitor.ValuePropertyChanged -= valuePropertyChangedHandler;
                monitor.ValuePropertyChanging -= valuePropertyChangingHandler;
                monitor.ValueRemoved -= valueRemovedHandler;
                monitor.ValueReplaced -= valueReplacedHandler;
                monitor.ValuesAdded -= valuesAddedHandler;
                monitor.ValuesRemoved -= valuesRemovedHandler;
                if (disposing)
                    monitor.Dispose();
            });

            Action<NotifyDictionaryValueEventArgs<TKey, TValue>> valueAddedLogic;
            if (monitor.ValuesNotifyChanging)
                valueAddedLogic = e => setValue(operations.Add(result.Value ?? default, selector(e.Key, e.Value) ?? default));
            else
                valueAddedLogic = e =>
                {
                    var key = e.Key;
                    var selectorValue = selector(key, e.Value);
                    selectorValues.Add(key, selectorValue);
                    setValue(operations.Add(result.Value ?? default, selectorValue ?? default));
                };

            Action<ValuePropertyChangeEventArgs<TKey, TValue>> valuePropertyChangedLogic;
            if (monitor.ValuesNotifyChanging)
                valuePropertyChangedLogic = e =>
                {
                    var key = e.Key;
                    var previousSelectorValue = selectorValues[key];
                    var newSelectorValue = selector(key, e.Value);
                    if (!previousSelectorValue.Equals(newSelectorValue))
                        setValue(operations.Add(result.Value ?? default, operations.Subtract(newSelectorValue ?? default, previousSelectorValue ?? default)));
                    selectorValues.Remove(key);
                };
            else
                valuePropertyChangedLogic = e =>
                {
                    var key = e.Key;
                    var previousSelectorValue = selectorValues[key];
                    var newSelectorValue = selector(key, e.Value);
                    if (!previousSelectorValue.Equals(newSelectorValue))
                    {
                        selectorValues[key] = newSelectorValue;
                        setValue(operations.Add(result.Value ?? default, operations.Subtract(newSelectorValue ?? default, previousSelectorValue ?? default)));
                    }
                };

            void valuePropertyChangingLogic(ValuePropertyChangeEventArgs<TKey, TValue> e)
            {
                var key = e.Key;
                var selectorValue = selector(key, e.Value);
                if (selectorValues.ContainsKey(key))
                    selectorValues[key] = selectorValue;
                else
                    selectorValues.Add(key, selectorValue);
            }

            Action<NotifyDictionaryValueEventArgs<TKey, TValue>> valueRemovedLogic;
            if (monitor.ValuesNotifyChanging)
                valueRemovedLogic = e => setValue(operations.Subtract(result.Value ?? default, selector(e.Key, e.Value) ?? default));
            else
                valueRemovedLogic = e =>
                {
                    var key = e.Key;
                    setValue(operations.Subtract(result.Value ?? default, selectorValues[key] ?? default));
                    selectorValues.Remove(key);
                };

            Action<NotifyDictionaryValueReplacedEventArgs<TKey, TValue>> valueReplacedLogic;
            if (monitor.ValuesNotifyChanging)
                valueReplacedLogic = e =>
                {
                    var key = e.Key;
                    selectorValues.Remove(key);
                    setValue(operations.Add(result.Value ?? default, operations.Subtract(selector(key, e.NewValue) ?? default, selector(key, e.OldValue) ?? default)));
                };
            else
                valueReplacedLogic = e =>
                {
                    var key = e.Key;
                    var newSelectorValue = selector(key, e.NewValue);
                    selectorValues[key] = newSelectorValue;
                    setValue(operations.Add(result.Value ?? default, operations.Subtract(newSelectorValue ?? default, selector(key, e.OldValue) ?? default)));
                };

            Action<NotifyDictionaryValuesEventArgs<TKey, TValue>> valuesAddedLogic;
            if (monitor.ValuesNotifyChanging)
                valuesAddedLogic = e =>
                {
                    TResult delta = default;
                    foreach (var keyValuePair in e.KeyValuePairs)
                        delta = operations.Add(delta, selector(keyValuePair.Key, keyValuePair.Value) ?? default);
                    setValue(operations.Add(result.Value ?? default, delta));
                };
            else
                valuesAddedLogic = e =>
                {
                    TResult delta = default;
                    foreach (var keyValuePair in e.KeyValuePairs)
                    {
                        var key = keyValuePair.Key;
                        var selectorValue = selector(key, keyValuePair.Value);
                        selectorValues.Add(key, selectorValue);
                        delta = operations.Add(delta, selectorValue ?? default);
                    }
                    setValue(operations.Add(result.Value ?? default, delta));
                };

            Action<NotifyDictionaryValuesEventArgs<TKey, TValue>> valuesRemovedLogic;
            if (monitor.ValuesNotifyChanging)
                valuesRemovedLogic = e =>
                {
                    TResult delta = default;
                    foreach (var keyValuePair in e.KeyValuePairs)
                        delta = operations.Subtract(delta, selector(keyValuePair.Key, keyValuePair.Value) ?? default);
                    setValue(operations.Add(result.Value ?? default, delta));
                };
            else
                valuesRemovedLogic = e =>
                {
                    TResult delta = default;
                    foreach (var keyValuePair in e.KeyValuePairs)
                    {
                        var key = keyValuePair.Key;
                        delta = operations.Subtract(delta, selectorValues[key] ?? default);
                        selectorValues.Remove(key);
                    }
                    setValue(operations.Add(result.Value ?? default, delta));
                };

            if (isThreadSafe)
            {
                valueAddedHandler = (sender, e) =>
                {
                    lock (resultAccess)
                        valueAddedLogic(e);
                };
                valuePropertyChangedHandler = (sender, e) =>
                {
                    lock (resultAccess)
                        valuePropertyChangedLogic(e);
                };
                valuePropertyChangingHandler = (sender, e) =>
                {
                    lock (resultAccess)
                        valuePropertyChangingLogic(e);
                };
                valueRemovedHandler = (sender, e) =>
                {
                    lock (resultAccess)
                        valueRemovedLogic(e);
                };
                valueReplacedHandler = (sender, e) =>
                {
                    lock (resultAccess)
                        valueReplacedLogic(e);
                };
                valuesAddedHandler = (sender, e) =>
                {
                    lock (resultAccess)
                        valuesAddedLogic(e);
                };
                valuesRemovedHandler = (sender, e) =>
                {
                    lock (resultAccess)
                        valuesRemovedLogic(e);
                };
            }
            else
            {
                valueAddedHandler = (sender, e) => valueAddedLogic(e);
                valuePropertyChangedHandler = (sender, e) => valuePropertyChangedLogic(e);
                valuePropertyChangingHandler = (sender, e) => valuePropertyChangingLogic(e);
                valueRemovedHandler = (sender, e) => valueRemovedLogic(e);
                valueReplacedHandler = (sender, e) => valueReplacedLogic(e);
                valuesAddedHandler = (sender, e) => valuesAddedLogic(e);
                valuesRemovedHandler = (sender, e) => valuesRemovedLogic(e);
            }

            monitor.ValueAdded += valueAddedHandler;
            monitor.ValuePropertyChanged += valuePropertyChangedHandler;
            monitor.ValuePropertyChanging += valuePropertyChangingHandler;
            monitor.ValueRemoved += valueRemovedHandler;
            monitor.ValueReplaced += valueReplacedHandler;
            monitor.ValuesAdded += valuesAddedHandler;
            monitor.ValuesRemoved += valuesRemovedHandler;
            return result;
        }

        public static ActiveAggregateValue<TResult> ActiveSum<TSource, TResult>(this IReadOnlyList<TSource> source, Func<TSource, TResult> selector, bool isThreadSafe = false, params string[] selectorProperties) where TSource : class where TResult : IEquatable<TResult>
        {
            var operations = new GenericOperations<TResult>();
            var selectorValues = new Dictionary<TSource, TResult>();
            TResult firstSum = default;
            var monitor = ActiveListMonitor<TSource>.Monitor(source, selectorProperties);
            if (monitor.ElementsNotifyChanging)
                foreach (var item in source)
                    firstSum = operations.Add(firstSum, selector(item));
            else
                foreach (var item in source)
                {
                    var selectorValue = selector(item);
                    firstSum = operations.Add(firstSum, selectorValue);
                    selectorValues.Add(item, selectorValue);
                }
            EventHandler<ElementPropertyChangeEventArgs<TSource>> elementPropertyChangedHandler = null;
            EventHandler<ElementPropertyChangeEventArgs<TSource>> elementPropertyChangingHandler = null;
            EventHandler<ElementMembershipEventArgs<TSource>> elementsAddedHandler = null;
            EventHandler<ElementMembershipEventArgs<TSource>> elementsRemovedHandler = null;
            var resultAccess = isThreadSafe ? new object() : null;
            var result = new ActiveAggregateValue<TResult>(true, firstSum, out var setValidity, out var setValue, disposing =>
            {
                monitor.ElementPropertyChanged -= elementPropertyChangedHandler;
                monitor.ElementPropertyChanging -= elementPropertyChangingHandler;
                monitor.ElementsAdded -= elementsAddedHandler;
                monitor.ElementsRemoved -= elementsRemovedHandler;
                if (disposing)
                    monitor.Dispose();
            });

            Action<ElementPropertyChangeEventArgs<TSource>> elementPropertyChangedLogic;
            if (monitor.ElementsNotifyChanging)
                elementPropertyChangedLogic = e =>
                {
                    var element = e.Element;
                    var previousSelectorValue = selectorValues[element];
                    var newSelectorValue = selector(element);
                    if (!previousSelectorValue.Equals(newSelectorValue))
                        setValue(operations.Add(result.Value, operations.Subtract(newSelectorValue, previousSelectorValue)));
                    selectorValues.Remove(element);
                };
            else
                elementPropertyChangedLogic = e =>
                {
                    var element = e.Element;
                    var previousSelectorValue = selectorValues[element];
                    var newSelectorValue = selector(element);
                    if (!previousSelectorValue.Equals(newSelectorValue))
                    {
                        selectorValues[element] = newSelectorValue;
                        setValue(operations.Add(result.Value, operations.Subtract(newSelectorValue, previousSelectorValue)));
                    }
                };

            void elementPropertyChangingLogic(ElementPropertyChangeEventArgs<TSource> e)
            {
                var element = e.Element;
                var selectorValue = selector(element);
                if (selectorValues.ContainsKey(element))
                    selectorValues[element] = selectorValue;
                else
                    selectorValues.Add(element, selectorValue);
            }

            Action<ElementMembershipEventArgs<TSource>> elementsAddedLogic;
            if (monitor.ElementsNotifyChanging)
                elementsAddedLogic = e =>
                {
                    TResult delta = default;
                    foreach (var element in e.Elements)
                        delta = operations.Add(delta, selector(element));
                    setValue(operations.Add(result.Value, delta));
                };
            else
                elementsAddedLogic = e =>
                {
                    TResult delta = default;
                    foreach (var element in e.Elements)
                    {
                        var selectorValue = selector(element);
                        selectorValues.Add(element, selectorValue);
                        delta = operations.Add(delta, selectorValue);
                    }
                    setValue(operations.Add(result.Value, delta));
                };

            Action<ElementMembershipEventArgs<TSource>> elementsRemovedLogic;
            if (monitor.ElementsNotifyChanging)
                elementsRemovedLogic = e =>
                {
                    TResult delta = default;
                    foreach (var element in e.Elements)
                        delta = operations.Subtract(delta, selector(element));
                    setValue(operations.Add(result.Value, delta));
                };
            else
                elementsRemovedLogic = e =>
                {
                    TResult delta = default;
                    foreach (var element in e.Elements)
                    {
                        delta = operations.Subtract(delta, selectorValues[element]);
                        selectorValues.Remove(element);
                    }
                    setValue(operations.Add(result.Value, delta));
                };

            if (isThreadSafe)
            {
                elementPropertyChangedHandler = (sender, e) =>
                {
                    lock (resultAccess)
                        elementPropertyChangedLogic(e);
                };
                elementPropertyChangingHandler = (sender, e) =>
                {
                    lock (resultAccess)
                        elementPropertyChangingLogic(e);
                };
                elementsAddedHandler = (sender, e) =>
                {
                    lock (resultAccess)
                        elementsAddedLogic(e);
                };
                elementsRemovedHandler = (sender, e) =>
                {
                    lock (resultAccess)
                        elementsRemovedLogic(e);
                };
            }
            else
            {
                elementPropertyChangedHandler = (sender, e) => elementPropertyChangedLogic(e);
                elementPropertyChangingHandler = (sender, e) => elementPropertyChangingLogic(e);
                elementsAddedHandler = (sender, e) => elementsAddedLogic(e);
                elementsRemovedHandler = (sender, e) => elementsRemovedLogic(e);
            }

            monitor.ElementPropertyChanged += elementPropertyChangedHandler;
            monitor.ElementPropertyChanging += elementPropertyChangingHandler;
            monitor.ElementsAdded += elementsAddedHandler;
            monitor.ElementsRemoved += elementsRemovedHandler;
            return result;
        }

        public static ActiveAggregateValue<TResult?> ActiveSum<TSource, TResult>(this IReadOnlyList<TSource> source, Func<TSource, TResult?> selector, bool isThreadSafe = false, params string[] selectorProperties) where TSource : class where TResult : struct
        {
            var operations = new GenericOperations<TResult>();
            var selectorValues = new Dictionary<TSource, TResult?>();
            TResult firstSum = default;
            var monitor = ActiveListMonitor<TSource>.Monitor(source, selectorProperties);
            if (monitor.ElementsNotifyChanging)
                foreach (var item in source)
                    firstSum = operations.Add(firstSum, selector(item) ?? default);
            else
                foreach (var item in source)
                {
                    var selectorValue = selector(item);
                    firstSum = operations.Add(firstSum, selectorValue ?? default);
                    selectorValues.Add(item, selectorValue);
                }
            EventHandler<ElementPropertyChangeEventArgs<TSource>> elementPropertyChangedHandler = null;
            EventHandler<ElementPropertyChangeEventArgs<TSource>> elementPropertyChangingHandler = null;
            EventHandler<ElementMembershipEventArgs<TSource>> elementsAddedHandler = null;
            EventHandler<ElementMembershipEventArgs<TSource>> elementsRemovedHandler = null;
            var resultAccess = isThreadSafe ? new object() : null;
            var result = new ActiveAggregateValue<TResult?>(true, firstSum, out var setValidity, out var setValue, disposing =>
            {
                monitor.ElementPropertyChanged -= elementPropertyChangedHandler;
                monitor.ElementPropertyChanging -= elementPropertyChangingHandler;
                monitor.ElementsAdded -= elementsAddedHandler;
                monitor.ElementsRemoved -= elementsRemovedHandler;
                if (disposing)
                    monitor.Dispose();
            });

            Action<ElementPropertyChangeEventArgs<TSource>> elementPropertyChangedLogic;
            if (monitor.ElementsNotifyChanging)
                elementPropertyChangedLogic = e =>
                {
                    var element = e.Element;
                    var previousSelectorValue = selectorValues[element];
                    var newSelectorValue = selector(element);
                    if (!previousSelectorValue.Equals(newSelectorValue))
                        setValue(operations.Add(result.Value ?? default, operations.Subtract(newSelectorValue ?? default, previousSelectorValue ?? default)));
                    selectorValues.Remove(element);
                };
            else
                elementPropertyChangedLogic = e =>
                {
                    var element = e.Element;
                    var previousSelectorValue = selectorValues[element];
                    var newSelectorValue = selector(element);
                    if (!previousSelectorValue.Equals(newSelectorValue))
                    {
                        selectorValues[element] = newSelectorValue;
                        setValue(operations.Add(result.Value ?? default, operations.Subtract(newSelectorValue ?? default, previousSelectorValue ?? default)));
                    }
                };

            void elementPropertyChangingLogic(ElementPropertyChangeEventArgs<TSource> e)
            {
                var element = e.Element;
                var selectorValue = selector(element);
                if (selectorValues.ContainsKey(element))
                    selectorValues[element] = selectorValue;
                else
                    selectorValues.Add(element, selectorValue);
            }

            Action<ElementMembershipEventArgs<TSource>> elementsAddedLogic;
            if (monitor.ElementsNotifyChanging)
                elementsAddedLogic = e =>
                {
                    TResult delta = default;
                    foreach (var element in e.Elements)
                        delta = operations.Add(delta, selector(element) ?? default);
                    setValue(operations.Add(result.Value ?? default, delta));
                };
            else
                elementsAddedLogic = e =>
                {
                    TResult delta = default;
                    foreach (var element in e.Elements)
                    {
                        var selectorValue = selector(element);
                        selectorValues.Add(element, selectorValue);
                        delta = operations.Add(delta, selectorValue ?? default);
                    }
                    setValue(operations.Add(result.Value ?? default, delta));
                };

            Action<ElementMembershipEventArgs<TSource>> elementsRemovedLogic;
            if (monitor.ElementsNotifyChanging)
                elementsRemovedLogic = e =>
                {
                    TResult delta = default;
                    foreach (var element in e.Elements)
                        delta = operations.Subtract(delta, selector(element) ?? default);
                    setValue(operations.Add(result.Value ?? default, delta));
                };
            else
                elementsRemovedLogic = e =>
                {
                    TResult delta = default;
                    foreach (var element in e.Elements)
                    {
                        delta = operations.Subtract(delta, selectorValues[element] ?? default);
                        selectorValues.Remove(element);
                    }
                    setValue(operations.Add(result.Value ?? default, delta));
                };

            if (isThreadSafe)
            {
                elementPropertyChangedHandler = (sender, e) =>
                {
                    lock (resultAccess)
                        elementPropertyChangedLogic(e);
                };
                elementPropertyChangingHandler = (sender, e) =>
                {
                    lock (resultAccess)
                        elementPropertyChangingLogic(e);
                };
                elementsAddedHandler = (sender, e) =>
                {
                    lock (resultAccess)
                        elementsAddedLogic(e);
                };
                elementsRemovedHandler = (sender, e) =>
                {
                    lock (resultAccess)
                        elementsRemovedLogic(e);
                };
            }
            else
            {
                elementPropertyChangedHandler = (sender, e) => elementPropertyChangedLogic(e);
                elementPropertyChangingHandler = (sender, e) => elementPropertyChangingLogic(e);
                elementsAddedHandler = (sender, e) => elementsAddedLogic(e);
                elementsRemovedHandler = (sender, e) => elementsRemovedLogic(e);
            }

            monitor.ElementPropertyChanged += elementPropertyChangedHandler;
            monitor.ElementPropertyChanging += elementPropertyChangingHandler;
            monitor.ElementsAdded += elementsAddedHandler;
            monitor.ElementsRemoved += elementsRemovedHandler;
            return result;
        }

        public static ActiveLookup<TKey, TValue> ActiveWhere<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, Func<TKey, TValue, bool> predicate, IndexingStrategy indexingStrategy = IndexingStrategy.NoneOrInherit, params string[] predicateProperties) =>
            ActiveWhere(source, (source as ISynchronizable)?.SynchronizationContext, predicate, indexingStrategy, predicateProperties);

        public static ActiveLookup<TKey, TValue> ActiveWhere<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, SynchronizationContext synchronizationContext, Func<TKey, TValue, bool> predicate, IndexingStrategy indexingStrategy = IndexingStrategy.NoneOrInherit, params string[] predicateProperties)
        {
            bool selfBalancingBinarySearchTree;
            switch (indexingStrategy)
            {
                case IndexingStrategy.HashTable:
                    selfBalancingBinarySearchTree = false;
                    break;
                case IndexingStrategy.SelfBalancingBinarySearchTree:
                    selfBalancingBinarySearchTree = true;
                    break;
                default:
                    selfBalancingBinarySearchTree = source is SortedDictionary<TKey, TValue> || source is ObservableSortedDictionary<TKey, TValue> || source is SynchronizedObservableSortedDictionary<TKey, TValue>;
                    break;
            }
            var rangeObservableDictionary = selfBalancingBinarySearchTree ? (ISynchronizableRangeDictionary<TKey, TValue>)new SynchronizedObservableSortedDictionary<TKey, TValue>(synchronizationContext, false) : new SynchronizedObservableDictionary<TKey, TValue>(synchronizationContext, false);
            rangeObservableDictionary.AddRange(source.Where(kvp => predicate(kvp.Key, kvp.Value)));
            var rangeDictionaryAccess = new object();
            var monitor = ActiveDictionaryMonitor<TKey, TValue>.Monitor(source, predicateProperties);

            void valueAddedHandler(object sender, NotifyDictionaryValueEventArgs<TKey, TValue> e)
            {
                var key = e.Key;
                var value = e.Value;
                var match = predicate(key, value);
                if (match)
                    lock (rangeDictionaryAccess)
                        rangeObservableDictionary.Add(key, value);
            }

            void valuePropertyChangedHandler(object sender, ValuePropertyChangeEventArgs<TKey, TValue> e)
            {
                var key = e.Key;
                var value = e.Value;
                var currentMatch = predicate(key, value);
                lock (rangeDictionaryAccess)
                    rangeObservableDictionary.Execute(() =>
                    {
                        var previousMatch = rangeObservableDictionary.ContainsKey(key);
                        if (previousMatch && !currentMatch)
                            rangeObservableDictionary.Remove(key);
                        else if (!previousMatch && currentMatch)
                            rangeObservableDictionary.Add(key, value);
                    });
            }

            void valueRemovedHandler(object sender, NotifyDictionaryValueEventArgs<TKey, TValue> e)
            {
                lock (rangeDictionaryAccess)
                    rangeObservableDictionary.Remove(e.Key);
            }

            void valueReplacedHandler(object sender, NotifyDictionaryValueReplacedEventArgs<TKey, TValue> e)
            {
                var key = e.Key;
                var value = e.NewValue;
                var currentMatch = predicate(key, value);
                lock (rangeDictionaryAccess)
                    rangeObservableDictionary.Execute(() =>
                    {
                        var previousMatch = rangeObservableDictionary.ContainsKey(key);
                        if (previousMatch && !currentMatch)
                            rangeObservableDictionary.Remove(key);
                        else if (!previousMatch && currentMatch)
                            rangeObservableDictionary.Add(key, value);
                    });
            }

            void valuesAddedHandler(object sender, NotifyDictionaryValuesEventArgs<TKey, TValue> e)
            {
                var matches = e.KeyValuePairs.Where(kvp => predicate(kvp.Key, kvp.Value)).ToList();
                lock (rangeDictionaryAccess)
                    rangeObservableDictionary.AddRange(matches);
            }

            void valuesRemovedHandler(object sender, NotifyDictionaryValuesEventArgs<TKey, TValue> e)
            {
                var keys = e.KeyValuePairs.Select(kvp => kvp.Key).ToList();
                lock (rangeDictionaryAccess)
                    rangeObservableDictionary.RemoveRange(keys);
            }

            monitor.ValueAdded += valueAddedHandler;
            monitor.ValuePropertyChanged += valuePropertyChangedHandler;
            monitor.ValueRemoved += valueRemovedHandler;
            monitor.ValueReplaced += valueReplacedHandler;
            monitor.ValuesAdded += valuesAddedHandler;
            monitor.ValuesRemoved += valuesRemovedHandler;
            var result = new ActiveLookup<TKey, TValue>(rangeObservableDictionary, disposing =>
            {
                monitor.ValueAdded -= valueAddedHandler;
                monitor.ValuePropertyChanged -= valuePropertyChangedHandler;
                monitor.ValueRemoved -= valueRemovedHandler;
                monitor.ValueReplaced -= valueReplacedHandler;
                monitor.ValuesAdded -= valuesAddedHandler;
                monitor.ValuesRemoved -= valuesRemovedHandler;
                if (disposing)
                    monitor.Dispose();
            });
            if (synchronizationContext != null)
                rangeObservableDictionary.IsSynchronized = true;
            return result;
        }

        public static ActiveEnumerable<TSource> ActiveWhere<TSource>(this IReadOnlyList<TSource> source, Func<TSource, bool> predicate, params string[] predicateProperties) where TSource : class =>
            ActiveWhere(source, (source as ISynchronizable)?.SynchronizationContext, predicate, predicateProperties);

        public static ActiveEnumerable<TSource> ActiveWhere<TSource>(this IReadOnlyList<TSource> source, SynchronizationContext synchronizationContext, Func<TSource, bool> predicate, params string[] predicateProperties) where TSource : class
        {
            var rangeObservableCollection = new SynchronizedRangeObservableCollection<TSource>(synchronizationContext, source.Where(predicate));
            var rangeObservableCollectionAccess = new object();
            var monitor = ActiveListMonitor<TSource>.Monitor(source, predicateProperties);
            HashSet<TSource> currentItems;
            if (monitor.ElementsNotifyChanging)
                currentItems = new HashSet<TSource>();
            else
                currentItems = new HashSet<TSource>(rangeObservableCollection);

            void elementPropertyChangedHandler(object sender, ElementPropertyChangeEventArgs<TSource> e)
            {
                lock (rangeObservableCollectionAccess)
                {
                    var element = e.Element;
                    if (predicate(element))
                    {
                        if (currentItems.Add(element))
                        {
                            rangeObservableCollection.Add(element);
                            if (monitor.ElementsNotifyChanging)
                                currentItems.Remove(element);
                        }
                    }
                    else if (currentItems.Remove(element))
                        rangeObservableCollection.Remove(element);
                }
            }

            void elementPropertyChangingHandler(object sender, ElementPropertyChangeEventArgs<TSource> e)
            {
                lock (rangeObservableCollectionAccess)
                {
                    var element = e.Element;
                    if (predicate(element))
                        currentItems.Add(element);
                }
            }

            void elementsAddedHandler(object sender, ElementMembershipEventArgs<TSource> e)
            {
                lock (rangeObservableCollectionAccess)
                {
                    var matchingElements = e.Elements.Where(predicate);
                    if (!monitor.ElementsNotifyChanging)
                        currentItems.UnionWith(matchingElements);
                    rangeObservableCollection.AddRange(matchingElements);
                }
            }

            void elementsRemovedHandler(object sender, ElementMembershipEventArgs<TSource> e)
            {
                lock (rangeObservableCollectionAccess)
                {
                    var matchingElements = new HashSet<TSource>(currentItems);
                    matchingElements.IntersectWith(e.Elements);
                    currentItems.ExceptWith(e.Elements);
                    rangeObservableCollection.RemoveRange(matchingElements);
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

        public static ActiveEnumerable<TValue> ToActiveEnumerable<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source) =>
            ToActiveEnumerable(source, (source as ISynchronizable)?.SynchronizationContext);

        public static ActiveEnumerable<TValue> ToActiveEnumerable<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, SynchronizationContext synchronizationContext) =>
            ToActiveEnumerable(source, synchronizationContext, (key, value) => value);

        public static ActiveEnumerable<TResult> ToActiveEnumerable<TKey, TValue, TResult>(this IReadOnlyDictionary<TKey, TValue> source, Func<TKey, TValue, TResult> selector, Action<TResult> releaser = null, Action<TKey, TValue, string, TResult> updater = null, params string[] selectorProperties) =>
            ToActiveEnumerable(source, (source as ISynchronizable)?.SynchronizationContext, selector, releaser, updater, selectorProperties);

        public static ActiveEnumerable<TResult> ToActiveEnumerable<TKey, TValue, TResult>(this IReadOnlyDictionary<TKey, TValue> source, SynchronizationContext synchronizationContext, Func<TKey, TValue, TResult> selector, Action<TResult> releaser = null, Action<TKey, TValue, string, TResult> updater = null, params string[] selectorProperties)
        {
            var keyToIndex = source is SortedDictionary<TKey, TValue> || source is ObservableSortedDictionary<TKey, TValue> || source is SynchronizedObservableSortedDictionary<TKey, TValue> ? (IDictionary<TKey, int>)new SortedDictionary<TKey, TValue>() : (IDictionary<TKey, int>)new Dictionary<TKey, TValue>();
            var rangeObservableCollection = new SynchronizedRangeObservableCollection<TResult>(synchronizationContext, source.Select((element, index) =>
            {
                keyToIndex.Add(element.Key, index);
                return selector(element.Key, element.Value);
            }), false);
            var rangeObservableCollectionAccess = new object();
            var monitor = ActiveDictionaryMonitor<TKey, TValue>.Monitor(source, selectorProperties);

            void valueAddedHandler(object sender, NotifyDictionaryValueEventArgs<TKey, TValue> e)
            {
                lock (rangeObservableCollectionAccess)
                {
                    keyToIndex.Add(e.Key, keyToIndex.Count);
                    rangeObservableCollection.Add(selector(e.Key, e.Value));
                }
            }

            void valuePropertyChangedHandler(object sender, ValuePropertyChangeEventArgs<TKey, TValue> e)
            {
                TResult element;
                lock (rangeObservableCollectionAccess)
                {
                    if (updater == null)
                        element = rangeObservableCollection.Replace(keyToIndex[e.Key], selector(e.Key, e.Value));
                    else
                        element = rangeObservableCollection[keyToIndex[e.Key]];
                }
                if (updater == null)
                    releaser?.Invoke(element);
                else
                    updater(e.Key, e.Value, e.PropertyName, element);
            }

            void valueRemovedHandler(object sender, NotifyDictionaryValueEventArgs<TKey, TValue> e)
            {
                TResult removedElement;
                lock (rangeObservableCollectionAccess)
                {
                    var removingIndex = keyToIndex[e.Key];
                    keyToIndex.Remove(e.Key);
                    foreach (var key in keyToIndex.Keys.ToList())
                    {
                        var index = keyToIndex[key];
                        if (index > removingIndex)
                            keyToIndex[key] = index - 1;
                    }
                    removedElement = rangeObservableCollection.GetAndRemoveAt(removingIndex);
                }
                releaser?.Invoke(removedElement);
            }

            void valueReplacedHandler(object sender, NotifyDictionaryValueReplacedEventArgs<TKey, TValue> e)
            {
                TResult replacedElement;
                lock (rangeObservableCollectionAccess)
                    replacedElement = rangeObservableCollection.Replace(keyToIndex[e.Key], selector(e.Key, e.NewValue));
                releaser?.Invoke(replacedElement);
            }

            void valuesAddedHandler(object sender, NotifyDictionaryValuesEventArgs<TKey, TValue> e)
            {
                if (e.KeyValuePairs.Any())
                    lock (rangeObservableCollectionAccess)
                    {
                        var lastIndex = keyToIndex.Count - 1;
                        foreach (var keyValuePair in e.KeyValuePairs)
                            keyToIndex.Add(keyValuePair.Key, ++lastIndex);
                        rangeObservableCollection.AddRange(e.KeyValuePairs.Select(kvp => selector(kvp.Key, kvp.Value)));
                    }
            }

            void valuesRemovedHandler(object sender, NotifyDictionaryValuesEventArgs<TKey, TValue> e)
            {
                if (e.KeyValuePairs.Any())
                    lock (rangeObservableCollectionAccess)
                    {
                        var removingIndicies = new List<int>();
                        foreach (var kvp in e.KeyValuePairs)
                        {
                            removingIndicies.Add(keyToIndex[kvp.Key]);
                            keyToIndex.Remove(kvp.Key);
                        }
                        var rangeStart = -1;
                        var rangeCount = 0;
                        rangeObservableCollection.Execute(() =>
                        {
                            foreach (var removingIndex in removingIndicies.OrderByDescending(i => i))
                            {
                                if (removingIndex != rangeStart - 1 && rangeStart != -1)
                                {
                                    if (rangeCount == 1)
                                        rangeObservableCollection.RemoveAt(rangeStart);
                                    else
                                        rangeObservableCollection.RemoveRange(rangeStart, rangeCount);
                                    rangeCount = 0;
                                }
                                rangeStart = removingIndex;
                                ++rangeCount;
                            }
                            if (rangeStart != -1)
                            {
                                if (rangeCount == 1)
                                    rangeObservableCollection.RemoveAt(rangeStart);
                                else
                                    rangeObservableCollection.RemoveRange(rangeStart, rangeCount);
                            }
                        });
                        var revisedKeyedIndicies = keyToIndex.OrderBy(kvp => kvp.Value).Select((element, index) => (element.Key, index));
                        keyToIndex = source is SortedDictionary<TKey, TValue> || source is ObservableSortedDictionary<TKey, TValue> || source is SynchronizedObservableSortedDictionary<TKey, TValue> ? (IDictionary<TKey, int>)new SortedDictionary<TKey, TValue>() : (IDictionary<TKey, int>)new Dictionary<TKey, TValue>();
                        foreach (var (key, index) in revisedKeyedIndicies)
                            keyToIndex.Add(key, index);
                    }
            }

            monitor.ValueAdded += valueAddedHandler;
            monitor.ValuePropertyChanged += valuePropertyChangedHandler;
            monitor.ValueRemoved += valueRemovedHandler;
            monitor.ValueReplaced += valueReplacedHandler;
            monitor.ValuesAdded += valuesAddedHandler;
            monitor.ValuesRemoved += valuesRemovedHandler;
            var result = new ActiveEnumerable<TResult>(rangeObservableCollection, disposing =>
            {
                monitor.ValueAdded -= valueAddedHandler;
                monitor.ValuePropertyChanged -= valuePropertyChangedHandler;
                monitor.ValueRemoved -= valueRemovedHandler;
                monitor.ValueReplaced -= valueReplacedHandler;
                monitor.ValuesAdded -= valuesAddedHandler;
                monitor.ValuesRemoved -= valuesRemovedHandler;
                if (disposing)
                    monitor.Dispose();
            });
            if (synchronizationContext != null)
                rangeObservableCollection.IsSynchronized = true;
            return result;
        }

        public static ActiveEnumerable<TSource> ToActiveEnumerable<TSource>(this IReadOnlyList<TSource> source) => new ActiveEnumerable<TSource>(source);
    }
}
