using Gear.ActiveExpressions;
using Gear.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace Gear.ActiveQuery
{
    public static class ActiveEnumerableExtensions
    {
        #region All

        public static ActiveValue<bool> ActiveAll<TSource>(this IEnumerable<TSource> source, Expression<Func<TSource, bool>> predicate, ActiveExpressionOptions predicateOptions = null)
        {
            ActiveQueryOptions.Optimize(ref predicate);

            var readOnlySource = source as IReadOnlyCollection<TSource>;
            var changeNotifyingSource = source as INotifyCollectionChanged;
            ActiveEnumerable<TSource> where;
            Action<bool> setValue = null;

            void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) => setValue(where.Count == (readOnlySource?.Count ?? source.Count()));

            return (source as ISynchronized).SequentialExecute(() =>
            {
                where = ActiveWhere(source, predicate, predicateOptions);
                where.CollectionChanged += collectionChanged;
                if (changeNotifyingSource != null)
                    changeNotifyingSource.CollectionChanged += collectionChanged;

                return new ActiveValue<bool>(where.Count == (readOnlySource?.Count ?? source.Count()), out setValue, elementFaultChangeNotifier: where, onDispose: () =>
                {
                    where.CollectionChanged -= collectionChanged;
                    if (changeNotifyingSource != null)
                        changeNotifyingSource.CollectionChanged -= collectionChanged;
                    where.Dispose();
                });
            });
        }

        #endregion All

        #region Any

        public static ActiveValue<bool> ActiveAny(this IEnumerable source)
        {
            var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
            if (source is INotifyCollectionChanged changeNotifyingSource)
            {
                var synchronizedSource = source as ISynchronized;
                Action<bool> setValue = null;

                void sourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => synchronizedSource.SequentialExecute(() => setValue(source.Cast<object>().Any()));

                return synchronizedSource.SequentialExecute(() =>
                {
                    changeNotifyingSource.CollectionChanged += sourceCollectionChanged;
                    return new ActiveValue<bool>(source.Cast<object>().Any(), out setValue, elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: () => changeNotifyingSource.CollectionChanged -= sourceCollectionChanged);
                });
            }
            try
            {
                return new ActiveValue<bool>(source.Cast<object>().Any(), elementFaultChangeNotifier: elementFaultChangeNotifier);
            }
            catch (Exception ex)
            {
                return new ActiveValue<bool>(default, ex, elementFaultChangeNotifier);
            }
        }

        public static ActiveValue<bool> ActiveAny<TSource>(this IEnumerable<TSource> source, Expression<Func<TSource, bool>> predicate, ActiveExpressionOptions predicateOptions = null)
        {
            ActiveQueryOptions.Optimize(ref predicate);

            ActiveEnumerable<TSource> where;
            Action<bool> setValue = null;

            void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) => setValue(where.Count > 0);

            return (source as ISynchronized).SequentialExecute(() =>
            {
                where = ActiveWhere(source, predicate, predicateOptions);
                where.CollectionChanged += collectionChanged;

                return new ActiveValue<bool>(where.Count > 0, out setValue, elementFaultChangeNotifier: where, onDispose: () =>
                {
                    where.CollectionChanged -= collectionChanged;
                    where.Dispose();
                });
            });
        }

        #endregion Any

        #region Average

        public static ActiveValue<TSource> ActiveAverage<TSource>(this IEnumerable<TSource> source) =>
            ActiveAverage(source, element => element);

        public static ActiveValue<TResult> ActiveAverage<TSource, TResult>(this IEnumerable<TSource> source, Expression<Func<TSource, TResult>> selector, ActiveExpressionOptions selectorOptions = null)
        {
            ActiveQueryOptions.Optimize(ref selector);

            var readOnlyCollection = source as IReadOnlyCollection<TSource>;
            var synchronizedSource = source as ISynchronized;
            var convertCount = CountConversion.GetConverter(typeof(TResult));
            var operations = new GenericOperations<TResult>();
            ActiveValue<TResult> sum;
            Action<TResult> setValue = null;
            Action<Exception> setOperationFault = null;

            int count() => readOnlyCollection?.Count ?? source.Count();

            void propertyChanged(object sender, PropertyChangedEventArgs e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    if (e.PropertyName == nameof(ActiveValue<TResult>.Value))
                    {
                        var currentCount = count();
                        if (currentCount == 0)
                        {
                            setValue(default);
                            setOperationFault(ExceptionHelper.SequenceContainsNoElements);
                        }
                        else
                        {
                            setOperationFault(null);
                            setValue(operations.Divide(sum.Value, (TResult)convertCount(currentCount)));
                        }
                    }
                });

            return synchronizedSource.SequentialExecute(() =>
            {
                sum = ActiveSum(source, selector, selectorOptions);
                sum.PropertyChanged += propertyChanged;

                var currentCount = count();
                return new ActiveValue<TResult>(currentCount > 0 ? operations.Divide(sum.Value, (TResult)convertCount(currentCount)) : default, out setValue, currentCount == 0 ? ExceptionHelper.SequenceContainsNoElements : null, out setOperationFault, sum, () =>
                {
                    sum.PropertyChanged -= propertyChanged;
                    sum.Dispose();
                });
            });
        }

        #endregion Average

        #region Cast

        public static ActiveEnumerable<TResult> ActiveCast<TResult>(this IEnumerable source, ActiveExpressionOptions castOptions = null, IndexingStrategy indexingStrategy = IndexingStrategy.HashTable) =>
            ActiveSelect(source, element => (TResult)element, castOptions, indexingStrategy);

        #endregion

        #region Concat

        public static ActiveEnumerable<TSource> ActiveConcat<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
        {
            var synchronizedFirst = first as ISynchronized;
            var synchronizedSecond = second as ISynchronized;

            if (synchronizedFirst != null && synchronizedSecond != null && synchronizedFirst.SynchronizationContext != synchronizedSecond.SynchronizationContext)
                throw new InvalidOperationException($"{nameof(first)} and {nameof(second)} are both synchronizable but using different synchronization contexts; select a different overload of {nameof(ActiveConcat)} to specify the synchronization context to use");

            var synchronizationContext = synchronizedFirst?.SynchronizationContext ?? synchronizedSecond?.SynchronizationContext ?? Synchronization.DefaultSynchronizationContext;

            SynchronizedRangeObservableCollection<TSource> rangeObservableCollection = null;
            ActiveEnumerable<TSource> firstEnumerable;
            ActiveEnumerable<TSource> secondEnumerable;

            void firstCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
                synchronizationContext.Execute(() =>
                {
                    if (e.Action == NotifyCollectionChangedAction.Reset)
                        rangeObservableCollection.ReplaceRange(0, rangeObservableCollection.Count - secondEnumerable.Count, first);
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
                });

            void secondCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
                synchronizationContext.Execute(() =>
                {
                    if (e.Action == NotifyCollectionChangedAction.Reset)
                        rangeObservableCollection.ReplaceRange(firstEnumerable.Count, rangeObservableCollection.Count - firstEnumerable.Count, second);
                    else
                    {
                        if (e.OldItems != null && e.NewItems != null && e.OldStartingIndex >= 0 && e.OldStartingIndex == e.NewStartingIndex)
                        {
                            if (e.OldItems.Count == 1 && e.NewItems.Count == 1)
                                rangeObservableCollection.Replace(firstEnumerable.Count + e.OldStartingIndex, (TSource)e.NewItems[0]);
                            else
                                rangeObservableCollection.ReplaceRange(firstEnumerable.Count + e.OldStartingIndex, e.OldItems.Count, e.NewItems.Cast<TSource>());
                        }
                        else
                        {
                            if (e.OldItems != null && e.OldStartingIndex >= 0)
                                rangeObservableCollection.RemoveRange(firstEnumerable.Count + e.OldStartingIndex, e.OldItems.Count);
                            if (e.NewItems != null && e.NewStartingIndex >= 0)
                                rangeObservableCollection.InsertRange(firstEnumerable.Count + e.NewStartingIndex, e.NewItems.Cast<TSource>());
                        }
                    }
                });

            return synchronizationContext.Execute(() =>
            {
                firstEnumerable = ToActiveEnumerable(first);
                secondEnumerable = ToActiveEnumerable(second);

                firstEnumerable.CollectionChanged += firstCollectionChanged;
                secondEnumerable.CollectionChanged += secondCollectionChanged;

                rangeObservableCollection = new SynchronizedRangeObservableCollection<TSource>(synchronizationContext, first.Concat(second));
                var mergedElementFaultChangeNotifier = new MergedElementFaultChangeNotifier(firstEnumerable, secondEnumerable);

                return new ActiveEnumerable<TSource>(rangeObservableCollection, mergedElementFaultChangeNotifier, () =>
                {
                    firstEnumerable.CollectionChanged -= firstCollectionChanged;
                    secondEnumerable.CollectionChanged -= secondCollectionChanged;
                    mergedElementFaultChangeNotifier.Dispose();
                });
            });
        }

        public static ActiveEnumerable<TSource> ActiveConcat<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second, SynchronizationContext synchronizationContext)
        {
            if (synchronizationContext == null)
                throw new ArgumentNullException(nameof(synchronizationContext));

            SynchronizedRangeObservableCollection<TSource> rangeObservableCollection = null;
            ActiveEnumerable<TSource> firstEnumerable;
            ActiveEnumerable<TSource> secondEnumerable;

            void firstCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
                synchronizationContext.Execute(() =>
                {
                    if (e.Action == NotifyCollectionChangedAction.Reset)
                        rangeObservableCollection.ReplaceRange(0, rangeObservableCollection.Count - secondEnumerable.Count, first);
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
                });

            void secondCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
                synchronizationContext.Execute(() =>
                {
                    if (e.Action == NotifyCollectionChangedAction.Reset)
                        rangeObservableCollection.ReplaceRange(firstEnumerable.Count, rangeObservableCollection.Count - firstEnumerable.Count, second);
                    else
                    {
                        if (e.OldItems != null && e.NewItems != null && e.OldStartingIndex >= 0 && e.OldStartingIndex == e.NewStartingIndex)
                        {
                            if (e.OldItems.Count == 1 && e.NewItems.Count == 1)
                                rangeObservableCollection.Replace(firstEnumerable.Count + e.OldStartingIndex, (TSource)e.NewItems[0]);
                            else
                                rangeObservableCollection.ReplaceRange(firstEnumerable.Count + e.OldStartingIndex, e.OldItems.Count, e.NewItems.Cast<TSource>());
                        }
                        else
                        {
                            if (e.OldItems != null && e.OldStartingIndex >= 0)
                                rangeObservableCollection.RemoveRange(firstEnumerable.Count + e.OldStartingIndex, e.OldItems.Count);
                            if (e.NewItems != null && e.NewStartingIndex >= 0)
                                rangeObservableCollection.InsertRange(firstEnumerable.Count + e.NewStartingIndex, e.NewItems.Cast<TSource>());
                        }
                    }
                });

            return synchronizationContext.Execute(() =>
            {
                firstEnumerable = ToActiveEnumerable(first);
                secondEnumerable = ToActiveEnumerable(second);

                firstEnumerable.CollectionChanged += firstCollectionChanged;
                secondEnumerable.CollectionChanged += secondCollectionChanged;

                rangeObservableCollection = new SynchronizedRangeObservableCollection<TSource>(synchronizationContext, first.Concat(second));
                var mergedElementFaultChangeNotifier = new MergedElementFaultChangeNotifier(firstEnumerable, secondEnumerable);

                return new ActiveEnumerable<TSource>(rangeObservableCollection, mergedElementFaultChangeNotifier, () =>
                {
                    firstEnumerable.CollectionChanged -= firstCollectionChanged;
                    secondEnumerable.CollectionChanged -= secondCollectionChanged;
                    mergedElementFaultChangeNotifier.Dispose();
                });
            });
        }

        #endregion Concat

        #region Distinct

        public static ActiveEnumerable<TSource> ActiveDistinct<TSource>(this IReadOnlyList<TSource> source)
        {
            var changingSource = source as INotifyCollectionChanged;
            var synchronizedSource = source as ISynchronized;
            SynchronizedRangeObservableCollection<TSource> rangeObservableCollection;
            Dictionary<TSource, int> distinctCounts = null;

            void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    if (e.Action == NotifyCollectionChangedAction.Reset)
                    {
                        distinctCounts = new Dictionary<TSource, int>();
                        var distinctValues = new List<TSource>();
                        foreach (var element in source)
                        {
                            if (distinctCounts.TryGetValue(element, out var distinctCount))
                                distinctCounts[element] = ++distinctCount;
                            else
                            {
                                distinctCounts.Add(element, 1);
                                distinctValues.Add(element);
                            }
                        }
                        rangeObservableCollection.ReplaceAll(distinctValues);
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
                });

            return synchronizedSource.SequentialExecute(() =>
            {
                rangeObservableCollection = new SynchronizedRangeObservableCollection<TSource>();

                if (changingSource != null)
                    changingSource.CollectionChanged += collectionChanged;

                distinctCounts = new Dictionary<TSource, int>();
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

                return new ActiveEnumerable<TSource>(rangeObservableCollection, () =>
                {
                    if (changingSource != null)
                        changingSource.CollectionChanged -= collectionChanged;
                });
            });
        }

        #endregion Distinct

        #region ElementAt

        public static ActiveValue<TSource> ActiveElementAt<TSource>(this IEnumerable<TSource> source, int index)
        {
            var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
            if (source is INotifyCollectionChanged changingSource)
            {
                var synchronizedSource = source as ISynchronized;
                Action<TSource> setValue = null;
                Action<Exception> setOperationFault = null;

                void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
                    synchronizedSource.SequentialExecute(() =>
                    {
                        try
                        {
                            var value = source.ElementAt(index);
                            setOperationFault(null);
                            setValue(value);
                        }
                        catch (Exception ex)
                        {
                            setOperationFault(ex);
                            setValue(default);
                        }
                    });

                return synchronizedSource.SequentialExecute(() =>
                {
                    changingSource.CollectionChanged += collectionChanged;
                    try
                    {
                        return new ActiveValue<TSource>(source.ElementAt(index), out setValue, out setOperationFault, elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: () => changingSource.CollectionChanged -= collectionChanged);
                    }
                    catch (Exception ex)
                    {
                        return new ActiveValue<TSource>(default, out setValue, ex, out setOperationFault, elementFaultChangeNotifier, () => changingSource.CollectionChanged -= collectionChanged);
                    }
                });
            }
            try
            {
                return new ActiveValue<TSource>(source.ElementAt(index), elementFaultChangeNotifier: elementFaultChangeNotifier);
            }
            catch (Exception ex)
            {
                return new ActiveValue<TSource>(default, ex, elementFaultChangeNotifier);
            }
        }

        public static ActiveValue<TSource> ActiveElementAt<TSource>(this IReadOnlyList<TSource> source, int index)
        {
            ActiveEnumerable<TSource> activeEnumerable;
            Action<TSource> setValue = null;
            Action<Exception> setOperationFault = null;
            var indexOutOfRange = false;

            void collectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                if (indexOutOfRange && index >= 0 && index < activeEnumerable.Count)
                {
                    setOperationFault(null);
                    indexOutOfRange = false;
                }
                else if (!indexOutOfRange && (index < 0 || index >= activeEnumerable.Count))
                {
                    setOperationFault(ExceptionHelper.IndexArgumentWasOutOfRange);
                    indexOutOfRange = true;
                }
                setValue(index >= 0 && index < activeEnumerable.Count ? activeEnumerable[index] : default);
            }

            return (source as ISynchronized).SequentialExecute(() =>
            {
                activeEnumerable = ToActiveEnumerable(source);
                activeEnumerable.CollectionChanged += collectionChanged;

                indexOutOfRange = index < 0 || index >= activeEnumerable.Count;
                return new ActiveValue<TSource>(!indexOutOfRange ? activeEnumerable[index] : default, out setValue, indexOutOfRange ? ExceptionHelper.IndexArgumentWasOutOfRange : null, out setOperationFault, activeEnumerable, () =>
                {
                    activeEnumerable.CollectionChanged -= collectionChanged;
                    activeEnumerable.Dispose();
                });
            });
        }

        #endregion ElementAt

        #region ElementAtOrDefault

        public static ActiveValue<TSource> ActiveElementAtOrDefault<TSource>(this IEnumerable<TSource> source, int index)
        {
            var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
            if (source is INotifyCollectionChanged changingSource)
            {
                var synchronizedSource = source as ISynchronized;
                Action<TSource> setValue = null;

                void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) => synchronizedSource.SequentialExecute(() => setValue(source.ElementAtOrDefault(index)));

                return synchronizedSource.SequentialExecute(() =>
                {
                    changingSource.CollectionChanged += collectionChanged;
                    return new ActiveValue<TSource>(source.ElementAtOrDefault(index), out setValue, elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: () => changingSource.CollectionChanged -= collectionChanged);
                });
            }
            else
                return new ActiveValue<TSource>(source.ElementAtOrDefault(index), elementFaultChangeNotifier: elementFaultChangeNotifier);
        }

        public static ActiveValue<TSource> ActiveElementAtOrDefault<TSource>(this IReadOnlyList<TSource> source, int index)
        {
            ActiveEnumerable<TSource> activeEnumerable;
            Action<TSource> setValue = null;

            void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) => setValue(index >= 0 && index < activeEnumerable.Count ? activeEnumerable[index] : default);

            return (source as ISynchronized).SequentialExecute(() =>
            {
                activeEnumerable = ToActiveEnumerable(source);
                activeEnumerable.CollectionChanged += collectionChanged;

                return new ActiveValue<TSource>(index >= 0 && index < activeEnumerable.Count ? activeEnumerable[index] : default, out setValue, elementFaultChangeNotifier: activeEnumerable, onDispose: () =>
                {
                    activeEnumerable.CollectionChanged -= collectionChanged;
                    activeEnumerable.Dispose();
                });
            });
        }

        #endregion ElementAtOrDefault

        #region First

        public static ActiveValue<TSource> ActiveFirst<TSource>(this IEnumerable<TSource> source)
        {
            var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
            if (source is INotifyCollectionChanged changingSource)
            {
                var synchronizedSource = source as ISynchronized;
                Action<TSource> setValue = null;
                Action<Exception> setOperationFault = null;

                void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
                    synchronizedSource.SequentialExecute(() =>
                    {
                        try
                        {
                            var value = source.First();
                            setOperationFault(null);
                            setValue(value);
                        }
                        catch (Exception ex)
                        {
                            setOperationFault(ex);
                            setValue(default);
                        }
                    });

                return synchronizedSource.SequentialExecute(() =>
                {
                    changingSource.CollectionChanged += collectionChanged;
                    try
                    {
                        return new ActiveValue<TSource>(source.First(), out setValue, out setOperationFault, elementFaultChangeNotifier, () => changingSource.CollectionChanged -= collectionChanged);
                    }
                    catch (Exception ex)
                    {
                        return new ActiveValue<TSource>(default, out setValue, ex, out setOperationFault, elementFaultChangeNotifier, () => changingSource.CollectionChanged -= collectionChanged);
                    }
                });
            }
            try
            {
                return new ActiveValue<TSource>(source.First(), elementFaultChangeNotifier: elementFaultChangeNotifier);
            }
            catch (Exception ex)
            {
                return new ActiveValue<TSource>(default, ex, elementFaultChangeNotifier);
            }
        }

        public static ActiveValue<TSource> ActiveFirst<TSource>(this IReadOnlyList<TSource> source, Expression<Func<TSource, bool>> predicate, ActiveExpressionOptions predicateOptions = null)
        {
            ActiveQueryOptions.Optimize(ref predicate);

            ActiveEnumerable<TSource> where;
            Action<TSource> setValue = null;
            Action<Exception> setOperationFault = null;
            var none = false;

            void collectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                if (none && where.Count > 0)
                {
                    setOperationFault(null);
                    none = false;
                }
                else if (!none && where.Count == 0)
                {
                    setOperationFault(ExceptionHelper.SequenceContainsNoElements);
                    none = true;
                }
                setValue(where.Count > 0 ? where[0] : default);
            }

            return (source as ISynchronized).SequentialExecute(() =>
            {
                where = ActiveWhere(source, predicate, predicateOptions);
                where.CollectionChanged += collectionChanged;

                none = where.Count == 0;
                return new ActiveValue<TSource>(!none ? where[0] : default, out setValue, none ? ExceptionHelper.SequenceContainsNoElements : null, out setOperationFault, where, () =>
                {
                    where.CollectionChanged -= collectionChanged;
                    where.Dispose();
                });
            });
        }

        #endregion First

        #region FirstOrDefault

        public static ActiveValue<TSource> ActiveFirstOrDefault<TSource>(this IEnumerable<TSource> source)
        {
            var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
            if (source is INotifyCollectionChanged changingSource)
            {
                var synchronizedSource = source as ISynchronized;
                Action<TSource> setValue = null;

                void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) => synchronizedSource.SequentialExecute(() => setValue(source.FirstOrDefault()));

                return synchronizedSource.SequentialExecute(() =>
                {
                    changingSource.CollectionChanged += collectionChanged;
                    return new ActiveValue<TSource>(source.FirstOrDefault(), out setValue, elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: () => changingSource.CollectionChanged -= collectionChanged);
                });
            }
            else
                return new ActiveValue<TSource>(source.FirstOrDefault(), elementFaultChangeNotifier: elementFaultChangeNotifier);
        }

        public static ActiveValue<TSource> ActiveFirstOrDefault<TSource>(this IReadOnlyList<TSource> source, Expression<Func<TSource, bool>> predicate, ActiveExpressionOptions predicateOptions = null)
        {
            ActiveEnumerable<TSource> where;
            Action<TSource> setValue = null;

            void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) => setValue(where.Count > 0 ? where[0] : default);

            return (source as ISynchronized).SequentialExecute(() =>
            {
                where = ActiveWhere(source, predicate, predicateOptions);
                where.CollectionChanged += collectionChanged;

                return new ActiveValue<TSource>(where.Count > 0 ? where[0] : default, out setValue, elementFaultChangeNotifier: where, onDispose: () =>
                {
                    where.CollectionChanged -= collectionChanged;
                    where.Dispose();
                });
            });
        }

        #endregion FirstOrDefault

        #region GroupBy

        public static ActiveEnumerable<ActiveGrouping<TKey, TSource>> ActiveGroupBy<TKey, TSource>(this IReadOnlyList<TSource> source, Expression<Func<TSource, TKey>> keySelector, ActiveExpressionOptions keySelectorOptions = null, IndexingStrategy indexingStrategy = IndexingStrategy.HashTable)
        {
            ActiveQueryOptions.Optimize(ref keySelector);

            var synchronizedSource = source as ISynchronized;
            IDictionary<TKey, (SynchronizedRangeObservableCollection<TSource> groupingObservableCollection, ActiveGrouping<TKey, TSource> grouping)> collectionAndGroupingDictionary;
            SynchronizedRangeObservableCollection<ActiveGrouping<TKey, TSource>> rangeObservableCollection = null;

            switch (indexingStrategy)
            {
                case IndexingStrategy.HashTable:
                    collectionAndGroupingDictionary = new Dictionary<TKey, (SynchronizedRangeObservableCollection<TSource> groupingObservableCollection, ActiveGrouping<TKey, TSource> grouping)>();
                    break;
                case IndexingStrategy.SelfBalancingBinarySearchTree:
                    collectionAndGroupingDictionary = new SortedDictionary<TKey, (SynchronizedRangeObservableCollection<TSource> groupingObservableCollection, ActiveGrouping<TKey, TSource> grouping)>();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(indexingStrategy), $"{nameof(indexingStrategy)} must be {IndexingStrategy.HashTable} or {IndexingStrategy.SelfBalancingBinarySearchTree}");
            }

            void addElement(TSource element, TKey key, int count = 1)
            {
                SynchronizedRangeObservableCollection<TSource> groupingObservableCollection;
                if (!collectionAndGroupingDictionary.TryGetValue(key, out var collectionAndGrouping))
                {
                    groupingObservableCollection = new SynchronizedRangeObservableCollection<TSource>();
                    var grouping = new ActiveGrouping<TKey, TSource>(key, groupingObservableCollection);
                    collectionAndGroupingDictionary.Add(key, (groupingObservableCollection, grouping));
                    rangeObservableCollection.Add(grouping);
                }
                else
                    groupingObservableCollection = collectionAndGrouping.groupingObservableCollection;
                groupingObservableCollection.AddRange(element.Repeat(count));
            }

            void elementResultChanged(object sender, RangeActiveExpressionResultChangeEventArgs<TSource, TKey> e) => synchronizedSource.SequentialExecute(() => addElement(e.Element, e.Result, e.Count));

            void elementResultChanging(object sender, RangeActiveExpressionResultChangeEventArgs<TSource, TKey> e) => synchronizedSource.SequentialExecute(() => removeElement(e.Element, e.Result, e.Count));

            void elementsAdded(object sender, RangeActiveExpressionMembershipEventArgs<TSource, TKey> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    foreach (var (element, result) in e.ElementResults)
                        addElement(element, result);
                });

            void elementsRemoved(object sender, RangeActiveExpressionMembershipEventArgs<TSource, TKey> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    foreach (var (element, result) in e.ElementResults)
                        removeElement(element, result);
                });

            void removeElement(TSource element, TKey key, int count = 1)
            {
                var (groupingObservableCollection, grouping) = collectionAndGroupingDictionary[key];
                while (--count >= 0)
                    groupingObservableCollection.Remove(element);
                if (groupingObservableCollection.Count == 0)
                {
                    rangeObservableCollection.Remove(grouping);
                    grouping.Dispose();
                    collectionAndGroupingDictionary.Remove(key);
                }
            }

            return synchronizedSource.SequentialExecute(() =>
            {
                var rangeActiveExpression = RangeActiveExpression.Create(source, keySelector, keySelectorOptions);
                rangeActiveExpression.ElementResultChanged += elementResultChanged;
                rangeActiveExpression.ElementResultChanging += elementResultChanging;
                rangeActiveExpression.ElementsAdded += elementsAdded;
                rangeActiveExpression.ElementsRemoved += elementsRemoved;

                rangeObservableCollection = new SynchronizedRangeObservableCollection<ActiveGrouping<TKey, TSource>>();
                foreach (var (element, result) in rangeActiveExpression.GetResults())
                    addElement(element, result);

                return new ActiveEnumerable<ActiveGrouping<TKey, TSource>>(rangeObservableCollection, rangeActiveExpression, () =>
                {
                    rangeActiveExpression.ElementResultChanged -= elementResultChanged;
                    rangeActiveExpression.ElementResultChanging -= elementResultChanging;
                    rangeActiveExpression.ElementsAdded -= elementsAdded;
                    rangeActiveExpression.ElementsRemoved -= elementsRemoved;
                });
            });
        }

        #endregion GroupBy

        #region Last

        public static ActiveValue<TSource> ActiveLast<TSource>(this IEnumerable<TSource> source)
        {
            var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
            if (source is INotifyCollectionChanged changingSource)
            {
                var synchronizedSource = source as ISynchronized;
                Action<TSource> setValue = null;
                Action<Exception> setOperationFault = null;

                void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
                    synchronizedSource.SequentialExecute(() =>
                    {
                        try
                        {
                            var value = source.Last();
                            setOperationFault(null);
                            setValue(value);
                        }
                        catch (Exception ex)
                        {
                            setOperationFault(ex);
                            setValue(default);
                        }
                    });

                return synchronizedSource.SequentialExecute(() =>
                {
                    changingSource.CollectionChanged += collectionChanged;
                    try
                    {
                        return new ActiveValue<TSource>(source.Last(), out setValue, out setOperationFault, elementFaultChangeNotifier, () => changingSource.CollectionChanged -= collectionChanged);
                    }
                    catch (Exception ex)
                    {
                        return new ActiveValue<TSource>(default, out setValue, ex, out setOperationFault, elementFaultChangeNotifier, () => changingSource.CollectionChanged -= collectionChanged);
                    }
                });
            }
            else
            {
                try
                {
                    return new ActiveValue<TSource>(source.Last(), elementFaultChangeNotifier: elementFaultChangeNotifier);
                }
                catch (Exception ex)
                {
                    return new ActiveValue<TSource>(default, ex, elementFaultChangeNotifier);
                }
            }
        }

        public static ActiveValue<TSource> ActiveLast<TSource>(this IReadOnlyList<TSource> source, Expression<Func<TSource, bool>> predicate, ActiveExpressionOptions predicateOptions = null)
        {
            ActiveQueryOptions.Optimize(ref predicate);

            ActiveEnumerable<TSource> where;
            Action<TSource> setValue = null;
            Action<Exception> setOperationFault = null;
            var none = false;

            void collectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                if (none && where.Count > 0)
                {
                    setOperationFault(null);
                    none = false;
                }
                else if (!none && where.Count == 0)
                {
                    setOperationFault(ExceptionHelper.SequenceContainsNoElements);
                    none = true;
                }
                setValue(where.Count > 0 ? where[where.Count - 1] : default);
            }

            return (source as ISynchronized).SequentialExecute(() =>
            {
                where = ActiveWhere(source, predicate, predicateOptions);
                where.CollectionChanged += collectionChanged;

                none = where.Count == 0;
                return new ActiveValue<TSource>(!none ? where[where.Count - 1] : default, out setValue, none ? ExceptionHelper.SequenceContainsNoElements : null, out setOperationFault, where, () =>
                {
                    where.CollectionChanged -= collectionChanged;
                    where.Dispose();
                });
            });
        }

        #endregion Last

        #region LastOrDefault

        public static ActiveValue<TSource> ActiveLastOrDefault<TSource>(this IEnumerable<TSource> source)
        {
            var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
            if (source is INotifyCollectionChanged changingSource)
            {
                var synchronizedSource = source as ISynchronized;
                Action<TSource> setValue = null;

                void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) => synchronizedSource.SequentialExecute(() => setValue(source.LastOrDefault()));

                return synchronizedSource.SequentialExecute(() =>
                {
                    changingSource.CollectionChanged += collectionChanged;
                    return new ActiveValue<TSource>(source.LastOrDefault(), out setValue, elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: () => changingSource.CollectionChanged -= collectionChanged);
                });
            }
            else
                return new ActiveValue<TSource>(source.LastOrDefault(), elementFaultChangeNotifier: elementFaultChangeNotifier);
        }

        public static ActiveValue<TSource> ActiveLastOrDefault<TSource>(this IReadOnlyList<TSource> source, Expression<Func<TSource, bool>> predicate, ActiveExpressionOptions predicateOptions = null)
        {
            ActiveEnumerable<TSource> where;
            Action<TSource> setValue = null;

            void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) => setValue(where.Count > 0 ? where[where.Count - 1] : default);

            return (source as ISynchronized).SequentialExecute(() =>
            {
                where = ActiveWhere(source, predicate, predicateOptions);
                where.CollectionChanged += collectionChanged;

                return new ActiveValue<TSource>(where.Count > 0 ? where[where.Count - 1] : default, out setValue, elementFaultChangeNotifier: where, onDispose: () =>
                {
                    where.CollectionChanged -= collectionChanged;
                    where.Dispose();
                });
            });
        }

        #endregion LastOrDefault

        #region Max

        public static ActiveValue<TSource> ActiveMax<TSource>(this IEnumerable<TSource> source) =>
            ActiveMax(source, element => element);

        public static ActiveValue<TResult> ActiveMax<TSource, TResult>(this IEnumerable<TSource> source, Expression<Func<TSource, TResult>> selector, ActiveExpressionOptions selectorOptions = null)
        {
            ActiveQueryOptions.Optimize(ref selector);

            var comparer = Comparer<TResult>.Default;
            var synchronizedSource = source as ISynchronized;
            EnumerableRangeActiveExpression<TSource, TResult> rangeActiveExpression;
            ActiveValue<TResult> activeValue = null;
            Action<TResult> setValue = null;
            Action<Exception> setOperationFault = null;

            void dispose()
            {
                rangeActiveExpression.ElementResultChanged -= elementResultChanged;
                rangeActiveExpression.ElementsAdded -= elementsAdded;
                rangeActiveExpression.ElementsRemoved -= elementsRemoved;
                rangeActiveExpression.Dispose();
            }

            void elementResultChanged(object sender, RangeActiveExpressionResultChangeEventArgs<TSource, TResult> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    var comparison = comparer.Compare(activeValue.Value, e.Result);
                    if (comparison < 0)
                        setValue(e.Result);
                    else if (comparison > 0)
                        setValue(rangeActiveExpression.GetResultsUnderLock().Select(er => er.result).Max());
                });

            void elementsAdded(object sender, RangeActiveExpressionMembershipEventArgs<TSource, TResult> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    if (e.ElementResults.Count > 0)
                    {
                        var addedMax = e.ElementResults.Select(er => er.result).Max();
                        if (activeValue.OperationFault != null || comparer.Compare(activeValue.Value, addedMax) < 0)
                        {
                            setOperationFault(null);
                            setValue(addedMax);
                        }
                    }
                });

            void elementsRemoved(object sender, RangeActiveExpressionMembershipEventArgs<TSource, TResult> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    if (e.ElementResults.Count > 0)
                    {
                        var removedMax = e.ElementResults.Select(er => er.result).Max();
                        if (comparer.Compare(activeValue.Value, removedMax) == 0)
                        {
                            try
                            {
                                var value = rangeActiveExpression.GetResultsUnderLock().Select(er => er.result).Max();
                                setOperationFault(null);
                                setValue(value);
                            }
                            catch (Exception ex)
                            {
                                setOperationFault(ex);
                            }
                        }
                    }
                });

            return synchronizedSource.SequentialExecute(() =>
            {
                rangeActiveExpression = RangeActiveExpression.Create(source, selector, selectorOptions);
                rangeActiveExpression.ElementResultChanged += elementResultChanged;
                rangeActiveExpression.ElementsAdded += elementsAdded;
                rangeActiveExpression.ElementsRemoved += elementsRemoved;

                try
                {
                    return activeValue = new ActiveValue<TResult>(rangeActiveExpression.GetResults().Select(er => er.result).Max(), out setValue, null, out setOperationFault, rangeActiveExpression, dispose);
                }
                catch (Exception ex)
                {
                    return activeValue = new ActiveValue<TResult>(default, out setValue, ex, out setOperationFault, rangeActiveExpression, dispose);
                }
            });
        }

        #endregion Max

        #region Min

        public static ActiveValue<TSource> ActiveMin<TSource>(this IEnumerable<TSource> source) =>
            ActiveMin(source, element => element);

        public static ActiveValue<TResult> ActiveMin<TSource, TResult>(this IEnumerable<TSource> source, Expression<Func<TSource, TResult>> selector, ActiveExpressionOptions selectorOptions = null)
        {
            ActiveQueryOptions.Optimize(ref selector);

            var comparer = Comparer<TResult>.Default;
            var synchronizedSource = source as ISynchronized;
            EnumerableRangeActiveExpression<TSource, TResult> rangeActiveExpression;
            ActiveValue<TResult> activeValue = null;
            Action<TResult> setValue = null;
            Action<Exception> setOperationFault = null;

            void dispose()
            {
                rangeActiveExpression.ElementResultChanged -= elementResultChanged;
                rangeActiveExpression.ElementsAdded -= elementsAdded;
                rangeActiveExpression.ElementsRemoved -= elementsRemoved;
                rangeActiveExpression.Dispose();
            }

            void elementResultChanged(object sender, RangeActiveExpressionResultChangeEventArgs<TSource, TResult> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    var comparison = comparer.Compare(activeValue.Value, e.Result);
                    if (comparison > 0)
                        setValue(e.Result);
                    else if (comparison < 0)
                        setValue(rangeActiveExpression.GetResultsUnderLock().Select(er => er.result).Min());
                });

            void elementsAdded(object sender, RangeActiveExpressionMembershipEventArgs<TSource, TResult> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    if (e.ElementResults.Count > 0)
                    {
                        var addedMin = e.ElementResults.Select(er => er.result).Min();
                        if (activeValue.OperationFault != null || comparer.Compare(activeValue.Value, addedMin) > 0)
                        {
                            setOperationFault(null);
                            setValue(addedMin);
                        }
                    }
                });

            void elementsRemoved(object sender, RangeActiveExpressionMembershipEventArgs<TSource, TResult> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    if (e.ElementResults.Count > 0)
                    {
                        var removedMin = e.ElementResults.Select(er => er.result).Min();
                        if (comparer.Compare(activeValue.Value, removedMin) == 0)
                        {
                            try
                            {
                                var value = rangeActiveExpression.GetResultsUnderLock().Select(er => er.result).Min();
                                setOperationFault(null);
                                setValue(value);
                            }
                            catch (Exception ex)
                            {
                                setOperationFault(ex);
                            }
                        }
                    }
                });

            return synchronizedSource.SequentialExecute(() =>
            {
                rangeActiveExpression = RangeActiveExpression.Create(source, selector, selectorOptions);
                rangeActiveExpression.ElementResultChanged += elementResultChanged;
                rangeActiveExpression.ElementsAdded += elementsAdded;
                rangeActiveExpression.ElementsRemoved += elementsRemoved;

                try
                {
                    return activeValue = new ActiveValue<TResult>(rangeActiveExpression.GetResults().Select(er => er.result).Min(), out setValue, null, out setOperationFault, rangeActiveExpression, dispose);
                }
                catch (Exception ex)
                {
                    return activeValue = new ActiveValue<TResult>(default, out setValue, ex, out setOperationFault, rangeActiveExpression, dispose);
                }
            });
        }

        #endregion Min

        #region OrderBy

        public static ActiveEnumerable<TSource> ActiveOrderBy<TSource>(this IEnumerable<TSource> source, Expression<Func<TSource, IComparable>> expression, ActiveExpressionOptions expressionOptions = null, bool isDescending = false) =>
            ActiveOrderBy(source, (expression, expressionOptions, isDescending));

        public static ActiveEnumerable<TSource> ActiveOrderBy<TSource>(this IEnumerable<TSource> source, params Expression<Func<TSource, IComparable>>[] expressions) =>
            ActiveOrderBy(source, expressions.Select(expression => (expression, (ActiveExpressionOptions)null, false)).ToArray());

        public static ActiveEnumerable<TSource> ActiveOrderBy<TSource>(this IEnumerable<TSource> source, params (Expression<Func<TSource, IComparable>> expression, ActiveExpressionOptions expressionOptions)[] selectors) =>
            ActiveOrderBy(source, selectors.Select(selector => (selector.expression, selector.expressionOptions, false)).ToArray());

        public static ActiveEnumerable<TSource> ActiveOrderBy<TSource>(this IEnumerable<TSource> source, params (Expression<Func<TSource, IComparable>> expression, bool isDescending)[] selectors) =>
            ActiveOrderBy(source, selectors.Select(selector => (selector.expression, (ActiveExpressionOptions)null, selector.isDescending)).ToArray());

        public static ActiveEnumerable<TSource> ActiveOrderBy<TSource>(this IEnumerable<TSource> source, params (Expression<Func<TSource, IComparable>> expression, ActiveExpressionOptions expressionOptions, bool isDescending)[] selectors) =>
            ActiveOrderBy(source, IndexingStrategy.HashTable, selectors);

        public static ActiveEnumerable<TSource> ActiveOrderBy<TSource>(this IEnumerable<TSource> source, IndexingStrategy indexingStrategy, Expression<Func<TSource, IComparable>> expression, ActiveExpressionOptions expressionOptions = null, bool isDescending = false) =>
            ActiveOrderBy(source, indexingStrategy, (expression, expressionOptions, isDescending));

        public static ActiveEnumerable<TSource> ActiveOrderBy<TSource>(this IEnumerable<TSource> source, IndexingStrategy indexingStrategy, params Expression<Func<TSource, IComparable>>[] expressions) =>
            ActiveOrderBy(source, indexingStrategy, expressions.Select(expression => (expression, (ActiveExpressionOptions)null, false)).ToArray());

        public static ActiveEnumerable<TSource> ActiveOrderBy<TSource>(this IEnumerable<TSource> source, IndexingStrategy indexingStrategy, params (Expression<Func<TSource, IComparable>> expression, ActiveExpressionOptions expressionOptions)[] selectors) =>
            ActiveOrderBy(source, indexingStrategy, selectors.Select(selector => (selector.expression, selector.expressionOptions, false)).ToArray());

        public static ActiveEnumerable<TSource> ActiveOrderBy<TSource>(this IEnumerable<TSource> source, IndexingStrategy indexingStrategy, params (Expression<Func<TSource, IComparable>> expression, bool isDescending)[] selectors) =>
            ActiveOrderBy(source, indexingStrategy, selectors.Select(selector => (selector.expression, (ActiveExpressionOptions)null, selector.isDescending)).ToArray());

        public static ActiveEnumerable<TSource> ActiveOrderBy<TSource>(this IEnumerable<TSource> source, IndexingStrategy indexingStrategy, params (Expression<Func<TSource, IComparable>> expression, ActiveExpressionOptions expressionOptions, bool isDescending)[] selectors)
        {
            if (selectors.Length == 0)
                return ToActiveEnumerable(source);

            selectors = selectors.Select(s => (expression: ActiveQueryOptions.Optimize(s.expression), s.expressionOptions, s.isDescending)).ToArray();

            ActiveOrderingComparer<TSource> comparer = null;
            var equalityComparer = EqualityComparer<TSource>.Default;
            SynchronizedRangeObservableCollection<TSource> rangeObservableCollection = null;
            IDictionary<TSource, (int startingIndex, int count)> startingIndiciesAndCounts = null;
            var synchronizedSource = source as ISynchronized;

            void rebuildStartingIndiciesAndCounts(IReadOnlyList<TSource> fromSort)
            {
                switch (indexingStrategy)
                {
                    case IndexingStrategy.HashTable:
                        startingIndiciesAndCounts = new Dictionary<TSource, (int startingIndex, int count)>();
                        break;
                    case IndexingStrategy.SelfBalancingBinarySearchTree:
                        startingIndiciesAndCounts = new SortedDictionary<TSource, (int startingIndex, int count)>();
                        break;
                }
                for (var i = 0; i < fromSort.Count; ++i)
                {
                    var element = fromSort[i];
                    if (startingIndiciesAndCounts.TryGetValue(element, out var startingIndexAndCount))
                        startingIndiciesAndCounts[element] = (startingIndexAndCount.startingIndex, startingIndexAndCount.count + 1);
                    else
                        startingIndiciesAndCounts.Add(element, (i, 1));
                }
            }

            void repositionElement(TSource element)
            {
                int startingIndex, count;
                if (indexingStrategy == IndexingStrategy.NoneOrInherit)
                {
                    var indicies = rangeObservableCollection.IndiciesOf(element);
                    count = indicies.Count();
                    if (count == 0)
                        return;
                    startingIndex = indicies.First();
                }
                else if (startingIndiciesAndCounts.TryGetValue(element, out var startingIndexAndCount))
                    (startingIndex, count) = startingIndexAndCount;
                else
                    return;
                var index = startingIndex;

                bool performMove()
                {
                    if (startingIndex != index)
                    {
                        if (indexingStrategy != IndexingStrategy.NoneOrInherit)
                            startingIndiciesAndCounts[element] = (index, count);
                        rangeObservableCollection.MoveRange(startingIndex, index, count);
                        return true;
                    }
                    return false;
                }

                if (indexingStrategy == IndexingStrategy.NoneOrInherit)
                {
                    while (index > 0 && comparer.Compare(element, rangeObservableCollection[index - 1]) < 0)
                        --index;
                    while (index < rangeObservableCollection.Count - count && comparer.Compare(element, rangeObservableCollection[index + count]) > 0)
                        ++index;
                    performMove();
                }
                else
                {
                    while (index > 0)
                    {
                        var otherElement = rangeObservableCollection[index - 1];
                        if (comparer.Compare(element, otherElement) >= 0)
                            break;
                        var (otherStartingIndex, otherCount) = startingIndiciesAndCounts[otherElement];
                        startingIndiciesAndCounts[otherElement] = (otherStartingIndex + count, otherCount);
                        index -= otherCount;
                    }
                    if (!performMove())
                    {
                        while (index < rangeObservableCollection.Count - count)
                        {
                            var otherElement = rangeObservableCollection[index + count];
                            if (comparer.Compare(element, otherElement) <= 0)
                                break;
                            var (otherStartingIndex, otherCount) = startingIndiciesAndCounts[otherElement];
                            startingIndiciesAndCounts[otherElement] = (otherStartingIndex - count, otherCount);
                            index += otherCount;
                        }
                        performMove();
                    }
                }
            }

            void elementResultChanged(object sender, RangeActiveExpressionResultChangeEventArgs<TSource, IComparable> e) => synchronizedSource.SequentialExecute(() => repositionElement(e.Element));

            void elementsAdded(object sender, RangeActiveExpressionMembershipEventArgs<TSource, IComparable> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    if (rangeObservableCollection.Count == 0)
                    {
                        var sorted = e.ElementResults.Select(er => er.element).ToList();
                        sorted.Sort(comparer);
                        if (indexingStrategy != IndexingStrategy.NoneOrInherit)
                            rebuildStartingIndiciesAndCounts(sorted);
                        rangeObservableCollection.Reset(sorted);
                    }
                    else
                        foreach (var elementAndResults in e.ElementResults.GroupBy(er => er.element, er => er.result))
                        {
                            var element = elementAndResults.Key;
                            var count = elementAndResults.Count();
                            var index = 0;
                            while (index < rangeObservableCollection.Count && comparer.Compare(element, rangeObservableCollection[index]) >= 0)
                                ++index;
                            if (indexingStrategy != IndexingStrategy.NoneOrInherit)
                                foreach (var startingIndexAndCountKv in startingIndiciesAndCounts.ToList())
                                {
                                    var otherElement = startingIndexAndCountKv.Key;
                                    if (!equalityComparer.Equals(otherElement, element))
                                    {
                                        var (otherStartingIndex, otherCount) = startingIndexAndCountKv.Value;
                                        if (otherStartingIndex >= index)
                                            startingIndiciesAndCounts[otherElement] = (otherStartingIndex + count, otherCount);
                                    }
                                }
                            rangeObservableCollection.InsertRange(index, Enumerable.Range(0, count).Select(i => element));
                            if (indexingStrategy != IndexingStrategy.NoneOrInherit)
                            {
                                if (startingIndiciesAndCounts.TryGetValue(element, out var startingIndexAndCount))
                                    startingIndiciesAndCounts[element] = (startingIndexAndCount.startingIndex, startingIndexAndCount.count + count);
                                else
                                    startingIndiciesAndCounts.Add(element, (index, count));
                            }
                        }
                });

            void elementsRemoved(object sender, RangeActiveExpressionMembershipEventArgs<TSource, IComparable> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    var elementResults = e.ElementResults;
                    if (elementResults.Count == rangeObservableCollection.Count)
                    {
                        switch (indexingStrategy)
                        {
                            case IndexingStrategy.HashTable:
                                startingIndiciesAndCounts = new Dictionary<TSource, (int startingIndex, int count)>();
                                break;
                            case IndexingStrategy.SelfBalancingBinarySearchTree:
                                startingIndiciesAndCounts = new SortedDictionary<TSource, (int startingIndex, int count)>();
                                break;
                        }
                        rangeObservableCollection.Clear();
                    }
                    else if (indexingStrategy == IndexingStrategy.NoneOrInherit)
                        foreach (var elementAndResults in elementResults.GroupBy(er => er.element, er => er.result))
                            rangeObservableCollection.RemoveRange(rangeObservableCollection.IndexOf(elementAndResults.Key), elementAndResults.Count());
                    else
                        foreach (var elementAndResults in elementResults.GroupBy(er => er.element, er => er.result))
                        {
                            var element = elementAndResults.Key;
                            var (startingIndex, currentCount) = startingIndiciesAndCounts[element];
                            var removedCount = elementAndResults.Count();
                            rangeObservableCollection.RemoveRange(startingIndex, removedCount);
                            if (removedCount == currentCount)
                                startingIndiciesAndCounts.Remove(element);
                            else
                                startingIndiciesAndCounts[element] = (startingIndex, currentCount - removedCount);
                        }
                });

            return synchronizedSource.SequentialExecute(() =>
            {
                var selections = selectors.Select(selector => (rangeActiveExpression: EnumerableRangeActiveExpression<TSource, IComparable>.Create(source, selector.expression, selector.expressionOptions), selector.isDescending)).ToList();
                comparer = new ActiveOrderingComparer<TSource>(selections.Select(selection => (selection.rangeActiveExpression, selection.isDescending)).ToList(), indexingStrategy);
                var (lastRangeActiveExpression, lastIsDescending) = selections[selections.Count - 1];
                lastRangeActiveExpression.ElementsAdded += elementsAdded;
                lastRangeActiveExpression.ElementsRemoved += elementsRemoved;
                foreach (var (rangeActiveExpression, isDescending) in selections)
                    rangeActiveExpression.ElementResultChanged += elementResultChanged;
                var sortedSource = source.ToList();
                sortedSource.Sort(comparer);

                if (indexingStrategy != IndexingStrategy.NoneOrInherit)
                    rebuildStartingIndiciesAndCounts(sortedSource);

                rangeObservableCollection = new SynchronizedRangeObservableCollection<TSource>(sortedSource);
                var mergedElementFaultChangeNotifier = new MergedElementFaultChangeNotifier(selections.Select(selection => selection.rangeActiveExpression));
                return new ActiveEnumerable<TSource>(rangeObservableCollection, mergedElementFaultChangeNotifier, () =>
                {
                    lastRangeActiveExpression.ElementsAdded -= elementsAdded;
                    lastRangeActiveExpression.ElementsRemoved -= elementsRemoved;
                    foreach (var (rangeActiveExpression, isDescending) in selections)
                    {
                        rangeActiveExpression.ElementResultChanged -= elementResultChanged;
                        rangeActiveExpression.Dispose();
                    }
                    mergedElementFaultChangeNotifier.Dispose();
                });
            });
        }

        #endregion OrderBy

        #region Select

        public static ActiveEnumerable<TResult> ActiveSelect<TResult>(this IEnumerable source, Expression<Func<object, TResult>> selector, ActiveExpressionOptions selectorOptions = null, IndexingStrategy indexingStrategy = IndexingStrategy.HashTable)
        {
            ActiveQueryOptions.Optimize(ref selector);

            IDictionary<object, List<int>> sourceToIndicies;
            switch (indexingStrategy)
            {
                case IndexingStrategy.NoneOrInherit:
                    sourceToIndicies = null;
                    break;
                case IndexingStrategy.SelfBalancingBinarySearchTree:
                    throw new ArgumentOutOfRangeException(nameof(indexingStrategy), $"{nameof(indexingStrategy)} must be {IndexingStrategy.HashTable} or {IndexingStrategy.NoneOrInherit}");
                default:
                    sourceToIndicies = new Dictionary<object, List<int>>();
                    break;
            }

            var synchronizedSource = source as ISynchronized;
            SynchronizedRangeObservableCollection<TResult> rangeObservableCollection = null;

            void elementResultChanged(object sender, RangeActiveExpressionResultChangeEventArgs<object, TResult> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    var sourceElement = e.Element;
                    var newResultElement = e.Result;
                    var indicies = indexingStrategy != IndexingStrategy.NoneOrInherit ? sourceToIndicies[sourceElement] : source.Cast<object>().IndiciesOf(sourceElement).ToList();
                    rangeObservableCollection.Replace(indicies[0], newResultElement);
                    foreach (var remainingIndex in indicies.Skip(1))
                        rangeObservableCollection[remainingIndex] = newResultElement;
                });

            void elementsAdded(object sender, RangeActiveExpressionMembershipEventArgs<object, TResult> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    var elementResults = e.ElementResults;
                    var count = elementResults.Count;
                    if (count > 0)
                    {
                        var index = e.Index;
                        if (indexingStrategy == IndexingStrategy.NoneOrInherit)
                            rangeObservableCollection.InsertRange(index, elementResults.Select(er => er.result));
                        else
                        {
                            foreach (var indiciesList in sourceToIndicies.Values)
                                for (int i = 0, ii = indiciesList.Count; i < ii; ++i)
                                {
                                    var listIndex = indiciesList[i];
                                    if (listIndex >= index)
                                        indiciesList[i] = listIndex + count;
                                }
                            rangeObservableCollection.InsertRange(index, elementResults.Select((er, sIndex) =>
                            {
                                var (element, result) = er;
                                if (!sourceToIndicies.TryGetValue(element, out var indiciesList))
                                {
                                    indiciesList = new List<int>();
                                    sourceToIndicies.Add(element, indiciesList);
                                }
                                indiciesList.Add(index + sIndex);
                                return result;
                            }));
                        }
                    }
                });

            void elementsMoved(object sender, RangeActiveExpressionMovedEventArgs<object, TResult> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    var count = e.ElementResults.Count;
                    var fromIndex = e.FromIndex;
                    var toIndex = e.ToIndex;
                    if (count > 0 && fromIndex != toIndex)
                    {
                        if (indexingStrategy != IndexingStrategy.NoneOrInherit)
                        {
                            int movementEnd = fromIndex + count, move = toIndex - fromIndex, displacementStart, displacementEnd, displace;
                            if (fromIndex < toIndex)
                            {
                                displacementStart = movementEnd;
                                displacementEnd = toIndex + count;
                                displace = count * -1;
                            }
                            else
                            {
                                displacementStart = toIndex;
                                displacementEnd = fromIndex;
                                displace = count;
                            }
                            foreach (var element in sourceToIndicies.Keys.ToList())
                            {
                                var indiciesList = sourceToIndicies[element];
                                for (int i = 0, ii = indiciesList.Count; i < ii; ++i)
                                {
                                    var index = indiciesList[i];
                                    if (index >= fromIndex && index < movementEnd)
                                        indiciesList[i] = index + move;
                                    else if (index >= displacementStart && index < displacementEnd)
                                        indiciesList[i] = index + displace;
                                }
                            }
                        }
                        rangeObservableCollection.MoveRange(fromIndex, toIndex, count);
                    }
                });

            void elementsRemoved(object sender, RangeActiveExpressionMembershipEventArgs<object, TResult> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    var count = e.ElementResults.Count;
                    if (count > 0)
                    {
                        var startIndex = e.Index;
                        rangeObservableCollection.RemoveRange(startIndex, count);
                        if (indexingStrategy != IndexingStrategy.NoneOrInherit)
                        {
                            var endIndex = startIndex + count;
                            foreach (var element in sourceToIndicies.Keys.ToList())
                            {
                                var indiciesList = sourceToIndicies[element];
                                for (var i = 0; i < indiciesList.Count;)
                                {
                                    var listIndex = indiciesList[i];
                                    if (listIndex >= startIndex)
                                    {
                                        if (listIndex >= endIndex)
                                        {
                                            indiciesList[i] = listIndex - count;
                                            ++i;
                                        }
                                        else
                                            indiciesList.RemoveAt(i);
                                    }
                                    else
                                        ++i;
                                }
                                if (indiciesList.Count == 0)
                                    sourceToIndicies.Remove(element);
                            }
                        }
                    }
                });

            TResult indexedInitializer((object element, TResult result) er, int index)
            {
                var (element, result) = er;
                if (!sourceToIndicies.TryGetValue(element, out var indicies))
                {
                    indicies = new List<int>();
                    sourceToIndicies.Add(element, indicies);
                }
                indicies.Add(index);
                return result;
            }

            return synchronizedSource.SequentialExecute(() =>
            {
                var rangeActiveExpression = RangeActiveExpression.Create(source, selector, selectorOptions);
                rangeActiveExpression.ElementResultChanged += elementResultChanged;
                rangeActiveExpression.ElementsAdded += elementsAdded;
                rangeActiveExpression.ElementsMoved += elementsMoved;
                rangeActiveExpression.ElementsRemoved += elementsRemoved;

                rangeObservableCollection = new SynchronizedRangeObservableCollection<TResult>(indexingStrategy != IndexingStrategy.NoneOrInherit ? rangeActiveExpression.GetResults().Select(indexedInitializer) : rangeActiveExpression.GetResults().Select(er => er.result));
                return new ActiveEnumerable<TResult>(rangeObservableCollection, rangeActiveExpression, () =>
                {
                    rangeActiveExpression.ElementResultChanged -= elementResultChanged;
                    rangeActiveExpression.ElementsAdded -= elementsAdded;
                    rangeActiveExpression.ElementsMoved -= elementsMoved;
                    rangeActiveExpression.ElementsRemoved -= elementsRemoved;

                    rangeActiveExpression.Dispose();
                });
            });
        }

        public static ActiveEnumerable<TResult> ActiveSelect<TSource, TResult>(this IEnumerable<TSource> source, Expression<Func<TSource, TResult>> selector, ActiveExpressionOptions predicateOptions = null, IndexingStrategy indexingStrategy = IndexingStrategy.HashTable)
        {
            ActiveQueryOptions.Optimize(ref selector);

            IDictionary<TSource, List<int>> sourceToIndicies;
            switch (indexingStrategy)
            {
                case IndexingStrategy.NoneOrInherit:
                    sourceToIndicies = null;
                    break;
                case IndexingStrategy.SelfBalancingBinarySearchTree:
                    sourceToIndicies = new SortedDictionary<TSource, List<int>>();
                    break;
                default:
                    sourceToIndicies = new Dictionary<TSource, List<int>>();
                    break;
            }

            var synchronizedSource = source as ISynchronized;
            SynchronizedRangeObservableCollection<TResult> rangeObservableCollection = null;

            void elementResultChanged(object sender, RangeActiveExpressionResultChangeEventArgs<TSource, TResult> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    var sourceElement = e.Element;
                    var newResultElement = e.Result;
                    var indicies = indexingStrategy != IndexingStrategy.NoneOrInherit ? (IReadOnlyList<int>)sourceToIndicies[sourceElement] : source.IndiciesOf(sourceElement).ToImmutableArray();
                    rangeObservableCollection.Replace(indicies[0], newResultElement);
                    foreach (var remainingIndex in indicies.Skip(1))
                        rangeObservableCollection[remainingIndex] = newResultElement;
                });

            void elementsAdded(object sender, RangeActiveExpressionMembershipEventArgs<TSource, TResult> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    var elementResults = e.ElementResults;
                    var count = elementResults.Count;
                    if (count > 0)
                    {
                        var index = e.Index;
                        if (indexingStrategy == IndexingStrategy.NoneOrInherit)
                            rangeObservableCollection.InsertRange(index, elementResults.Select(er => er.result));
                        else
                        {
                            foreach (var indiciesList in sourceToIndicies.Values)
                                for (int i = 0, ii = indiciesList.Count; i < ii; ++i)
                                {
                                    var listIndex = indiciesList[i];
                                    if (listIndex >= index)
                                        indiciesList[i] = listIndex + count;
                                }
                            rangeObservableCollection.InsertRange(index, elementResults.Select((er, sIndex) =>
                            {
                                var (element, result) = er;
                                if (!sourceToIndicies.TryGetValue(element, out var indiciesList))
                                {
                                    indiciesList = new List<int>();
                                    sourceToIndicies.Add(element, indiciesList);
                                }
                                indiciesList.Add(index + sIndex);
                                return result;
                            }));
                        }
                    }
                });

            void elementsMoved(object sender, RangeActiveExpressionMovedEventArgs<TSource, TResult> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    var count = e.ElementResults.Count;
                    var fromIndex = e.FromIndex;
                    var toIndex = e.ToIndex;
                    if (count > 0 && fromIndex != toIndex)
                    {
                        if (indexingStrategy != IndexingStrategy.NoneOrInherit)
                        {
                            int movementEnd = fromIndex + count, move = toIndex - fromIndex, displacementStart, displacementEnd, displace;
                            if (fromIndex < toIndex)
                            {
                                displacementStart = movementEnd;
                                displacementEnd = toIndex + count;
                                displace = count * -1;
                            }
                            else
                            {
                                displacementStart = toIndex;
                                displacementEnd = fromIndex;
                                displace = count;
                            }
                            foreach (var element in sourceToIndicies.Keys.ToList())
                            {
                                var indiciesList = sourceToIndicies[element];
                                for (int i = 0, ii = indiciesList.Count; i < ii; ++i)
                                {
                                    var index = indiciesList[i];
                                    if (index >= fromIndex && index < movementEnd)
                                        indiciesList[i] = index + move;
                                    else if (index >= displacementStart && index < displacementEnd)
                                        indiciesList[i] = index + displace;
                                }
                            }
                        }
                        rangeObservableCollection.MoveRange(fromIndex, toIndex, count);
                    }
                });

            void elementsRemoved(object sender, RangeActiveExpressionMembershipEventArgs<TSource, TResult> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    var count = e.ElementResults.Count;
                    if (count > 0)
                    {
                        var startIndex = e.Index;
                        rangeObservableCollection.RemoveRange(startIndex, count);
                        if (indexingStrategy != IndexingStrategy.NoneOrInherit)
                        {
                            var endIndex = startIndex + count;
                            foreach (var element in sourceToIndicies.Keys.ToList())
                            {
                                var indiciesList = sourceToIndicies[element];
                                for (var i = 0; i < indiciesList.Count;)
                                {
                                    var listIndex = indiciesList[i];
                                    if (listIndex >= startIndex)
                                    {
                                        if (listIndex >= endIndex)
                                        {
                                            indiciesList[i] = listIndex - count;
                                            ++i;
                                        }
                                        else
                                            indiciesList.RemoveAt(i);
                                    }
                                    else
                                        ++i;
                                }
                                if (indiciesList.Count == 0)
                                    sourceToIndicies.Remove(element);
                            }
                        }
                    }
                });

            TResult indexedInitializer((TSource element, TResult result) er, int index)
            {
                var (element, result) = er;
                if (!sourceToIndicies.TryGetValue(element, out var indicies))
                {
                    indicies = new List<int>();
                    sourceToIndicies.Add(element, indicies);
                }
                indicies.Add(index);
                return result;
            }

            return synchronizedSource.SequentialExecute(() =>
            {
                var rangeActiveExpression = RangeActiveExpression.Create(source, selector, predicateOptions);
                rangeActiveExpression.ElementResultChanged += elementResultChanged;
                rangeActiveExpression.ElementsAdded += elementsAdded;
                rangeActiveExpression.ElementsMoved += elementsMoved;
                rangeActiveExpression.ElementsRemoved += elementsRemoved;

                rangeObservableCollection = new SynchronizedRangeObservableCollection<TResult>(indexingStrategy != IndexingStrategy.NoneOrInherit ? rangeActiveExpression.GetResults().Select(indexedInitializer) : rangeActiveExpression.GetResults().Select(er => er.result));
                return new ActiveEnumerable<TResult>(rangeObservableCollection, rangeActiveExpression, () =>
                {
                    rangeActiveExpression.ElementResultChanged -= elementResultChanged;
                    rangeActiveExpression.ElementsAdded -= elementsAdded;
                    rangeActiveExpression.ElementsMoved -= elementsMoved;
                    rangeActiveExpression.ElementsRemoved -= elementsRemoved;

                    rangeActiveExpression.Dispose();
                });
            });
        }

        #endregion Select

        #region SelectMany

        public static ActiveEnumerable<TResult> ActiveSelectMany<TSource, TResult>(this IEnumerable<TSource> source, Expression<Func<TSource, IEnumerable<TResult>>> selector, ActiveExpressionOptions selectorOptions = null, IndexingStrategy indexingStrategy = IndexingStrategy.HashTable)
        {
            ActiveQueryOptions.Optimize(ref selector);

            var sourceEqualityComparer = EqualityComparer<TSource>.Default;
            var changingResultToSource = new Dictionary<INotifyCollectionChanged, TSource>();
            IDictionary<TSource, INotifyCollectionChanged> sourceToChangingResult;
            IDictionary<TSource, int> sourceToCount;
            IDictionary<TSource, List<int>> sourceToStartingIndicies;
            switch (indexingStrategy)
            {
                case IndexingStrategy.HashTable:
                    sourceToChangingResult = new Dictionary<TSource, INotifyCollectionChanged>();
                    sourceToCount = new Dictionary<TSource, int>();
                    sourceToStartingIndicies = new Dictionary<TSource, List<int>>();
                    break;
                case IndexingStrategy.SelfBalancingBinarySearchTree:
                    sourceToChangingResult = new SortedDictionary<TSource, INotifyCollectionChanged>();
                    sourceToCount = new SortedDictionary<TSource, int>();
                    sourceToStartingIndicies = new SortedDictionary<TSource, List<int>>();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(indexingStrategy), $"{nameof(indexingStrategy)} must be {IndexingStrategy.HashTable} or {IndexingStrategy.SelfBalancingBinarySearchTree}");
            }

            var synchronizableSource = source as ISynchronized;
            SynchronizedRangeObservableCollection<TResult> rangeObservableCollection = null;

            void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
                synchronizableSource.SequentialExecute(() =>
                {
                    var oldItems = e.OldItems != null ? e.OldItems.Cast<TResult>() : Enumerable.Empty<TResult>();
                    var oldItemsCount = e.OldItems != null ? e.OldItems.Count : 0;
                    var oldStartingIndex = e.OldStartingIndex;
                    var newItems = e.NewItems != null ? e.NewItems.Cast<TResult>() : Enumerable.Empty<TResult>();
                    var newItemsCount = e.NewItems != null ? e.NewItems.Count : 0;
                    var newStartingIndex = e.NewStartingIndex;
                    var element = changingResultToSource[(INotifyCollectionChanged)sender];
                    var previousCount = sourceToCount[element];
                    var action = e.Action;
                    var result = (IEnumerable<TResult>)sender;
                    var countDifference = action == NotifyCollectionChangedAction.Reset ? result.Count() - previousCount : newItemsCount - oldItemsCount;
                    if (countDifference != 0)
                        sourceToCount[element] = previousCount + countDifference;
                    var startingIndiciesList = sourceToStartingIndicies[element];
                    for (int i = 0, ii = startingIndiciesList.Count; i < ii; ++i)
                    {
                        var startingIndex = startingIndiciesList[i];
                        switch (action)
                        {
                            case NotifyCollectionChangedAction.Reset:
                                if (previousCount > 0)
                                    rangeObservableCollection.ReplaceRange(startingIndex, previousCount, result);
                                else
                                    rangeObservableCollection.InsertRange(startingIndex, result);
                                break;
                            case NotifyCollectionChangedAction.Replace when oldStartingIndex == newStartingIndex:
                                rangeObservableCollection.ReplaceRange(startingIndex + oldStartingIndex, oldItemsCount, newItems);
                                break;
                            case NotifyCollectionChangedAction.Move when oldItems.SequenceEqual(newItems):
                                rangeObservableCollection.MoveRange(startingIndex + oldStartingIndex, startingIndex + newStartingIndex, oldItemsCount);
                                break;
                            default:
                                rangeObservableCollection.RemoveRange(startingIndex + oldStartingIndex, oldItemsCount);
                                rangeObservableCollection.InsertRange(startingIndex + newStartingIndex, newItems);
                                break;
                        }
                        if (countDifference != 0)
                            foreach (var adjustingStartingIndiciesKv in sourceToStartingIndicies)
                            {
                                var adjustingElement = adjustingStartingIndiciesKv.Key;
                                var adjustingStartingIndicies = adjustingStartingIndiciesKv.Value;
                                if (sourceEqualityComparer.Equals(element, adjustingElement))
                                    for (int j = 0, jj = adjustingStartingIndicies.Count; j < jj; ++j)
                                    {
                                        var adjustingStartingIndex = adjustingStartingIndicies[j];
                                        if (adjustingStartingIndex > startingIndex)
                                            adjustingStartingIndicies[j] = adjustingStartingIndex + countDifference;
                                    }
                                else
                                    for (int j = 0, jj = adjustingStartingIndicies.Count; j < jj; ++j)
                                    {
                                        var adjustingStartingIndex = adjustingStartingIndicies[j];
                                        if (adjustingStartingIndex >= startingIndex)
                                            adjustingStartingIndicies[j] = adjustingStartingIndex + countDifference;
                                    }
                            }
                    }
                });

            void elementResultChanged(object sender, RangeActiveExpressionResultChangeEventArgs<TSource, IEnumerable<TResult>> e) =>
                synchronizableSource.SequentialExecute(() =>
                {
                    var element = e.Element;
                    if (sourceToChangingResult.TryGetValue(element, out var previousChangingResult))
                    {
                        changingResultToSource.Remove(previousChangingResult);
                        sourceToChangingResult.Remove(element);
                        previousChangingResult.CollectionChanged -= collectionChanged;
                    }
                    var previousCount = sourceToCount[element];
                    var result = e.Result;
                    var newCount = result.Count();
                    sourceToCount[element] = newCount;
                    var countDifference = newCount - previousCount;
                    var startingIndiciesList = sourceToStartingIndicies[element];
                    for (int i = 0, ii = startingIndiciesList.Count; i < ii; ++i)
                    {
                        var startingIndex = startingIndiciesList[i];
                        rangeObservableCollection.ReplaceRange(startingIndex, previousCount, result);
                        foreach (var adjustingStartingIndiciesKv in sourceToStartingIndicies)
                        {
                            var adjustingElement = adjustingStartingIndiciesKv.Key;
                            var adjustingStartingIndicies = adjustingStartingIndiciesKv.Value;
                            if (sourceEqualityComparer.Equals(element, adjustingElement))
                                for (int j = 0, jj = adjustingStartingIndicies.Count; j < jj; ++j)
                                {
                                    var adjustingStartingIndex = adjustingStartingIndicies[j];
                                    if (adjustingStartingIndex > startingIndex)
                                        adjustingStartingIndicies[j] = adjustingStartingIndex + countDifference;
                                }
                            else
                                for (int j = 0, jj = adjustingStartingIndicies.Count; j < jj; ++j)
                                {
                                    var adjustingStartingIndex = adjustingStartingIndicies[j];
                                    if (adjustingStartingIndex >= startingIndex)
                                        adjustingStartingIndicies[j] = adjustingStartingIndex + countDifference;
                                }
                        }
                    }
                    if (result is INotifyCollectionChanged newChangingResult)
                    {
                        newChangingResult.CollectionChanged += collectionChanged;
                        changingResultToSource.Add(newChangingResult, element);
                        sourceToChangingResult.Add(element, newChangingResult);
                    }
                });

            void elementsAdded(object sender, RangeActiveExpressionMembershipEventArgs<TSource, IEnumerable<TResult>> e) =>
                synchronizableSource.SequentialExecute(() =>
                {
                    int resultsIndex;
                    if (e.Index == 0)
                        resultsIndex = 0;
                    else
                    {
                        var indexTranslation = sourceToStartingIndicies.SelectMany(kv => kv.Value).OrderBy(resultIndex => resultIndex).ToImmutableArray();
                        resultsIndex = e.Index < indexTranslation.Length ? indexTranslation[e.Index] : rangeObservableCollection.Count;
                    }

                    var iterativeResultsIndex = resultsIndex;
                    IDictionary<TSource, List<int>> newSourceToStartingIndicies;
                    switch (indexingStrategy)
                    {
                        case IndexingStrategy.SelfBalancingBinarySearchTree:
                            newSourceToStartingIndicies = new SortedDictionary<TSource, List<int>>();
                            break;
                        default:
                            newSourceToStartingIndicies = new Dictionary<TSource, List<int>>();
                            break;
                    }

                    IEnumerable<TResult> indexingSelector((TSource element, IEnumerable<TResult> result) er)
                    {
                        var (element, result) = er;
                        if (!newSourceToStartingIndicies.TryGetValue(element, out var newStartingIndicies))
                        {
                            newStartingIndicies = new List<int>();
                            newSourceToStartingIndicies.Add(element, newStartingIndicies);
                        }
                        newStartingIndicies.Add(iterativeResultsIndex);
                        var resultCount = result.Count();
                        iterativeResultsIndex += resultCount;
                        if (!sourceToCount.ContainsKey(element))
                            sourceToCount.Add(element, resultCount);
                        if (result is INotifyCollectionChanged changingResult && !changingResultToSource.ContainsKey(changingResult))
                        {
                            changingResult.CollectionChanged += collectionChanged;
                            changingResultToSource.Add(changingResult, element);
                            sourceToChangingResult.Add(element, changingResult);
                        }
                        return er.result;
                    }

                    var results = e.ElementResults.SelectMany(indexingSelector).ToList();
                    var count = results.Count;
                    if (count > 0)
                    {
                        foreach (var startingIndiciesList in sourceToStartingIndicies.Values)
                            for (int i = 0, ii = startingIndiciesList.Count; i < ii; ++i)
                            {
                                var startingIndex = startingIndiciesList[i];
                                if (startingIndex >= resultsIndex)
                                    startingIndiciesList[i] = startingIndex + count;
                            }
                        rangeObservableCollection.InsertRange(resultsIndex, results);
                    }
                    foreach (var kv in newSourceToStartingIndicies)
                    {
                        var key = kv.Key;
                        if (sourceToStartingIndicies.TryGetValue(key, out var startingIndicies))
                        {
                            startingIndicies.AddRange(kv.Value);
                            startingIndicies.Sort();
                        }
                        else
                            sourceToStartingIndicies.Add(key, kv.Value);
                    }
                });

            void elementsMoved(object sender, RangeActiveExpressionMovedEventArgs<TSource, IEnumerable<TResult>> e) =>
                synchronizableSource.SequentialExecute(() =>
                {
                    var elementResults = e.ElementResults;
                    var count = elementResults.SelectMany(er => er.result).Count();
                    if (count > 0 && e.FromIndex != e.ToIndex)
                    {
                        var indexTranslation = sourceToStartingIndicies.SelectMany(kv => kv.Value).OrderBy(resultIndex => resultIndex).ToImmutableArray();
                        var fromIndex = indexTranslation[e.FromIndex];
                        var toIndex = e.FromIndex > e.ToIndex ? indexTranslation[e.ToIndex] : (e.ToIndex == indexTranslation.Length - 1 ? rangeObservableCollection.Count : indexTranslation[e.ToIndex + 1]) - count;
                        int movementEnd = fromIndex + count, move = toIndex - fromIndex, displacementStart, displacementEnd, displace;
                        if (fromIndex < toIndex)
                        {
                            displacementStart = movementEnd;
                            displacementEnd = toIndex + count;
                            displace = count * -1;
                        }
                        else
                        {
                            displacementStart = toIndex;
                            displacementEnd = fromIndex;
                            displace = count;
                        }
                        foreach (var element in sourceToStartingIndicies.Keys.ToList())
                        {
                            var startingIndiciesList = sourceToStartingIndicies[element];
                            for (int i = 0, ii = startingIndiciesList.Count; i < ii; ++i)
                            {
                                var startingIndex = startingIndiciesList[i];
                                if (startingIndex >= fromIndex && startingIndex < movementEnd)
                                    startingIndiciesList[i] = startingIndex + move;
                                else if (startingIndex >= displacementStart && startingIndex < displacementEnd)
                                    startingIndiciesList[i] = startingIndex + displace;
                            }
                        }
                        rangeObservableCollection.MoveRange(fromIndex, toIndex, count);
                    }
                });

            void elementsRemoved(object sender, RangeActiveExpressionMembershipEventArgs<TSource, IEnumerable<TResult>> e) =>
                synchronizableSource.SequentialExecute(() =>
                {
                    var count = e.ElementResults.SelectMany(er => er.result).Count();
                    if (count > 0)
                    {
                        var startIndex = e.Index == 0 ? 0 : sourceToStartingIndicies.SelectMany(kv => kv.Value).OrderBy(resultIndex => resultIndex).ElementAt(e.Index);
                        rangeObservableCollection.RemoveRange(startIndex, count);
                        var endIndex = startIndex + count;
                        foreach (var element in sourceToStartingIndicies.Keys.ToList())
                        {
                            var startingIndiciesList = sourceToStartingIndicies[element];
                            for (var i = 0; i < startingIndiciesList.Count;)
                            {
                                var startingListIndex = startingIndiciesList[i];
                                if (startingListIndex >= startIndex)
                                {
                                    if (startingListIndex >= endIndex)
                                    {
                                        startingIndiciesList[i] = startingListIndex - count;
                                        ++i;
                                    }
                                    else
                                        startingIndiciesList.RemoveAt(i);
                                }
                                else
                                    ++i;
                            }
                            if (startingIndiciesList.Count == 0)
                            {
                                sourceToCount.Remove(element);
                                sourceToStartingIndicies.Remove(element);
                                if (sourceToChangingResult.TryGetValue(element, out var changingResult))
                                {
                                    changingResultToSource.Remove(changingResult);
                                    sourceToChangingResult.Remove(element);
                                    changingResult.CollectionChanged -= collectionChanged;
                                }
                            }
                        }
                    }
                });

            var initializationCount = 0;

            IEnumerable<TResult> initializer((TSource element, IEnumerable<TResult> result) er)
            {
                var (element, result) = er;
                if (!sourceToStartingIndicies.TryGetValue(element, out var startingIndicies))
                {
                    startingIndicies = new List<int>();
                    sourceToStartingIndicies.Add(element, startingIndicies);
                }
                startingIndicies.Add(initializationCount);
                var resultCount = result.Count();
                initializationCount += resultCount;
                if (!sourceToCount.ContainsKey(element))
                    sourceToCount.Add(element, result.Count());
                if (result is INotifyCollectionChanged changingResult)
                {
                    changingResult.CollectionChanged += collectionChanged;
                    changingResultToSource.Add(changingResult, element);
                    sourceToChangingResult.Add(element, changingResult);
                }
                return result;
            }

            return synchronizableSource.SequentialExecute(() =>
            {
                var rangeActiveExpression = RangeActiveExpression.Create(source, selector, selectorOptions);
                rangeActiveExpression.ElementResultChanged += elementResultChanged;
                rangeActiveExpression.ElementsAdded += elementsAdded;
                rangeActiveExpression.ElementsMoved += elementsMoved;
                rangeActiveExpression.ElementsRemoved += elementsRemoved;

                rangeObservableCollection = new SynchronizedRangeObservableCollection<TResult>(rangeActiveExpression.GetResults().SelectMany(initializer));
                return new ActiveEnumerable<TResult>(rangeObservableCollection, onDispose: () =>
                {
                    foreach (var changingResult in changingResultToSource.Keys)
                        changingResult.CollectionChanged -= collectionChanged;
                    rangeActiveExpression.ElementResultChanged -= elementResultChanged;
                    rangeActiveExpression.ElementsAdded -= elementsAdded;
                    rangeActiveExpression.ElementsMoved -= elementsMoved;
                    rangeActiveExpression.ElementsRemoved -= elementsRemoved;
                });
            });
        }

        #endregion SelectMany

        #region Single

        public static ActiveValue<TSource> ActiveSingle<TSource>(this IEnumerable<TSource> source)
        {
            var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
            if (source is INotifyCollectionChanged changingSource)
            {
                var synchronizedSource = source as ISynchronized;
                Action<TSource> setValue = null;
                Action<Exception> setOperationFault = null;

                void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
                    synchronizedSource.SequentialExecute(() =>
                    {
                        try
                        {
                            var value = source.Single();
                            setOperationFault(null);
                            setValue(value);
                        }
                        catch (Exception ex)
                        {
                            setOperationFault(ex);
                            setValue(default);
                        }
                    });

                return synchronizedSource.SequentialExecute(() =>
                {
                    changingSource.CollectionChanged += collectionChanged;
                    try
                    {
                        return new ActiveValue<TSource>(source.Single(), out setValue, out setOperationFault, elementFaultChangeNotifier, () => changingSource.CollectionChanged -= collectionChanged);
                    }
                    catch (Exception ex)
                    {
                        return new ActiveValue<TSource>(default, out setValue, ex, out setOperationFault, elementFaultChangeNotifier, () => changingSource.CollectionChanged -= collectionChanged);
                    }
                });
            }
            else
            {
                try
                {
                    return new ActiveValue<TSource>(source.Single(), elementFaultChangeNotifier: elementFaultChangeNotifier);
                }
                catch (Exception ex)
                {
                    return new ActiveValue<TSource>(default, ex, elementFaultChangeNotifier);
                }
            }
        }

        public static ActiveValue<TSource> ActiveSingle<TSource>(this IReadOnlyList<TSource> source, Expression<Func<TSource, bool>> predicate, ActiveExpressionOptions predicateOptions = null)
        {
            ActiveQueryOptions.Optimize(ref predicate);

            ActiveEnumerable<TSource> where;
            Action<TSource> setValue = null;
            Action<Exception> setOperationFault = null;
            var none = false;
            var moreThanOne = false;

            void collectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                if (none && where.Count > 0)
                {
                    setOperationFault(null);
                    none = false;
                }
                else if (!none && where.Count == 0)
                {
                    setOperationFault(ExceptionHelper.SequenceContainsNoElements);
                    none = true;
                }
                if (moreThanOne && where.Count <= 1)
                {
                    setOperationFault(null);
                    moreThanOne = false;
                }
                else if (!moreThanOne && where.Count > 1)
                {
                    setOperationFault(ExceptionHelper.SequenceContainsMoreThanOneElement);
                    moreThanOne = true;
                }
                setValue(where.Count == 1 ? where[0] : default);
            }

            return (source as ISynchronized).SequentialExecute(() =>
            {
                where = ActiveWhere(source, predicate, predicateOptions);
                where.CollectionChanged += collectionChanged;

                Exception operationFault = null;
                if (none = where.Count == 0)
                    operationFault = ExceptionHelper.SequenceContainsNoElements;
                else if (moreThanOne = where.Count > 1)
                    operationFault = ExceptionHelper.SequenceContainsMoreThanOneElement;
                return new ActiveValue<TSource>(operationFault == null ? where[0] : default, out setValue, operationFault, out setOperationFault, where, () =>
                {
                    where.CollectionChanged -= collectionChanged;
                    where.Dispose();
                });
            });
        }

        #endregion Single

        #region SingleOrDefault

        public static ActiveValue<TSource> ActiveSingleOrDefault<TSource>(this IEnumerable<TSource> source)
        {
            var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
            if (source is INotifyCollectionChanged changingSource)
            {
                var synchronizedSource = source as ISynchronized;
                Action<TSource> setValue = null;
                Action<Exception> setOperationFault = null;

                void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
                    synchronizedSource.SequentialExecute(() =>
                    {
                        try
                        {
                            var value = source.SingleOrDefault();
                            setOperationFault(null);
                            setValue(value);
                        }
                        catch (Exception ex)
                        {
                            setOperationFault(ex);
                            setValue(default);
                        }
                    });

                return synchronizedSource.SequentialExecute(() =>
                {
                    changingSource.CollectionChanged += collectionChanged;
                    try
                    {
                        return new ActiveValue<TSource>(source.SingleOrDefault(), out setValue, null, out setOperationFault, elementFaultChangeNotifier, () => changingSource.CollectionChanged -= collectionChanged);
                    }
                    catch (Exception ex)
                    {
                        return new ActiveValue<TSource>(default, out setValue, ex, out setOperationFault, elementFaultChangeNotifier, () => changingSource.CollectionChanged -= collectionChanged);
                    }
                });
            }
            else
            {
                try
                {
                    return new ActiveValue<TSource>(source.SingleOrDefault(), elementFaultChangeNotifier: elementFaultChangeNotifier);
                }
                catch (Exception ex)
                {
                    return new ActiveValue<TSource>(default, ex, elementFaultChangeNotifier);
                }
            }
        }

        public static ActiveValue<TSource> ActiveSingleOrDefault<TSource>(this IReadOnlyList<TSource> source, Expression<Func<TSource, bool>> predicate, ActiveExpressionOptions predicateOptions = null)
        {
            ActiveQueryOptions.Optimize(ref predicate);

            ActiveEnumerable<TSource> where;
            Action<TSource> setValue = null;
            Action<Exception> setOperationFault = null;
            var moreThanOne = false;

            void collectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                if (moreThanOne && where.Count <= 1)
                {
                    setOperationFault(null);
                    moreThanOne = false;
                }
                else if (!moreThanOne && where.Count > 1)
                {
                    setOperationFault(ExceptionHelper.SequenceContainsMoreThanOneElement);
                    moreThanOne = true;
                }
                setValue(where.Count == 1 ? where[0] : default);
            }

            return (source as ISynchronized).SequentialExecute(() =>
            {
                where = ActiveWhere(source, predicate, predicateOptions);
                where.CollectionChanged += collectionChanged;

                var operationFault = (moreThanOne = where.Count > 1) ? ExceptionHelper.SequenceContainsMoreThanOneElement : null;
                return new ActiveValue<TSource>(!moreThanOne && where.Count == 1 ? where[0] : default, out setValue, operationFault, out setOperationFault, where, () =>
                {
                    where.CollectionChanged -= collectionChanged;
                    where.Dispose();
                });
            });
        }

        #endregion SingleOrDefault

        #region Sum

        public static ActiveValue<TSource> ActiveSum<TSource>(this IEnumerable<TSource> source) =>
            ActiveSum(source, element => element);

        public static ActiveValue<TResult> ActiveSum<TSource, TResult>(this IEnumerable<TSource> source, Expression<Func<TSource, TResult>> selector, ActiveExpressionOptions selectorOptions = null)
        {
            ActiveQueryOptions.Optimize(ref selector);

            var operations = new GenericOperations<TResult>();
            var synchronizedSource = source as ISynchronized;
            ActiveValue<TResult> activeValue = null;
            Action<TResult> setValue = null;
            var resultsChanging = new Dictionary<TSource, (TResult result, int instances)>();

            void elementResultChanged(object sender, RangeActiveExpressionResultChangeEventArgs<TSource, TResult> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    var (result, instances) = resultsChanging[e.Element];
                    resultsChanging.Remove(e.Element);
                    setValue(operations.Add(activeValue.Value, operations.Subtract(e.Result, result).Repeat(instances).Aggregate(operations.Add)));
                });

            void elementResultChanging(object sender, RangeActiveExpressionResultChangeEventArgs<TSource, TResult> e) => synchronizedSource.SequentialExecute(() => resultsChanging.Add(e.Element, (e.Result, e.Count)));

            void elementsAdded(object sender, RangeActiveExpressionMembershipEventArgs<TSource, TResult> e) => synchronizedSource.SequentialExecute(() => setValue(new TResult[] { activeValue.Value }.Concat(e.ElementResults.Select(er => er.result)).Aggregate(operations.Add)));

            void elementsRemoved(object sender, RangeActiveExpressionMembershipEventArgs<TSource, TResult> e) => synchronizedSource.SequentialExecute(() => setValue(new TResult[] { activeValue.Value }.Concat(e.ElementResults.Select(er => er.result)).Aggregate(operations.Subtract)));

            return synchronizedSource.SequentialExecute(() =>
            {
                var rangeActiveExpression = RangeActiveExpression.Create(source, selector, selectorOptions);
                rangeActiveExpression.ElementResultChanged += elementResultChanged;
                rangeActiveExpression.ElementResultChanging += elementResultChanging;
                rangeActiveExpression.ElementsAdded += elementsAdded;
                rangeActiveExpression.ElementsRemoved += elementsRemoved;

                void dispose()
                {
                    rangeActiveExpression.ElementResultChanged -= elementResultChanged;
                    rangeActiveExpression.ElementResultChanging -= elementResultChanging;
                    rangeActiveExpression.ElementsAdded -= elementsAdded;
                    rangeActiveExpression.ElementsRemoved -= elementsRemoved;

                    rangeActiveExpression.Dispose();
                }

                try
                {
                    return activeValue = new ActiveValue<TResult>(rangeActiveExpression.GetResults().Select(er => er.result).Aggregate(operations.Add), out setValue, null, rangeActiveExpression, dispose);
                }
                catch (InvalidOperationException)
                {
                    return activeValue = new ActiveValue<TResult>(default, out setValue, null, rangeActiveExpression, dispose);
                }
            });
        }

        #endregion Sum

        #region ToActiveEnumerable

        public static ActiveEnumerable<TSource> ToActiveEnumerable<TSource>(this IEnumerable<TSource> source)
        {
            if (source is IReadOnlyList<TSource> readOnlyList && source is ISynchronized)
                return new ActiveEnumerable<TSource>(readOnlyList);

            var changingSource = source as INotifyCollectionChanged;
            var synchronizedSource = source as ISynchronized;
            SynchronizedRangeObservableCollection<TSource> rangeObservableCollection = null;

            void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    var oldItems = e.OldItems != null ? e.OldItems.Cast<TSource>() : Enumerable.Empty<TSource>();
                    var oldItemsCount = e.OldItems != null ? e.OldItems.Count : 0;
                    var newItems = e.NewItems != null ? e.NewItems.Cast<TSource>() : Enumerable.Empty<TSource>();
                    var newItemsCount = e.NewItems != null ? e.NewItems.Count : 0;
                    switch (e.Action)
                    {
                        case NotifyCollectionChangedAction.Reset:
                            rangeObservableCollection.Reset(source);
                            break;
                        case NotifyCollectionChangedAction.Replace:
                            rangeObservableCollection.ReplaceRange(e.OldStartingIndex, oldItemsCount, newItems);
                            break;
                        default:
                            if (oldItemsCount > 0)
                                rangeObservableCollection.RemoveRange(e.OldStartingIndex, oldItemsCount);
                            if (newItemsCount > 0)
                                rangeObservableCollection.InsertRange(e.NewStartingIndex, newItems);
                            break;
                    }
                });

            return synchronizedSource.SequentialExecute(() =>
            {
                if (changingSource != null)
                    changingSource.CollectionChanged += collectionChanged;

                rangeObservableCollection = new SynchronizedRangeObservableCollection<TSource>(source);
                return new ActiveEnumerable<TSource>(rangeObservableCollection, source as INotifyElementFaultChanges, () =>
                {
                    if (changingSource != null)
                        changingSource.CollectionChanged -= collectionChanged;
                });
            });
        }

        #endregion ToActiveEnumerable

        #region ToActiveLookup

        public static ActiveLookup<TKey, TValue> ToActiveLookup<TSource, TKey, TValue>(this IEnumerable<TSource> source, Expression<Func<TSource, KeyValuePair<TKey, TValue>>> selector, ActiveExpressionOptions selectorOptions = null, IndexingStrategy indexingStategy = IndexingStrategy.HashTable, IComparer<TKey> keyComparer = null, IEqualityComparer<TKey> keyEqualityComparer = null)
        {
            ActiveQueryOptions.Optimize(ref selector);

            var synchronizedSource = source as ISynchronized;
            IDictionary<TKey, int> duplicateKeys;
            var isFaultedDuplicateKeys = false;
            var isFaultedNullKey = false;
            var nullKeys = 0;
            ISynchronizedObservableRangeDictionary<TKey, TValue> rangeObservableDictionary;
            Action<Exception> setOperationFault = null;

            void checkOperationFault()
            {
                if (nullKeys > 0 && !isFaultedNullKey)
                {
                    isFaultedNullKey = true;
                    setOperationFault(ExceptionHelper.KeyNull);
                }
                else if (nullKeys == 0 && isFaultedNullKey)
                {
                    isFaultedNullKey = false;
                    setOperationFault(null);
                }

                if (!isFaultedNullKey)
                {
                    if (duplicateKeys.Count > 0 && !isFaultedDuplicateKeys)
                    {
                        isFaultedDuplicateKeys = true;
                        setOperationFault(ExceptionHelper.SameKeyAlreadyAdded);
                    }
                    else if (duplicateKeys.Count == 0 && isFaultedDuplicateKeys)
                    {
                        isFaultedDuplicateKeys = false;
                        setOperationFault(null);
                    }
                }
            }

            void elementResultChanged(object sender, RangeActiveExpressionResultChangeEventArgs<TSource, KeyValuePair<TKey, TValue>> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    var result = e.Result;
                    var key = result.Key;
                    var count = e.Count;
                    if (key == null)
                        nullKeys += count;
                    else
                    {
                        var value = result.Value;
                        if (rangeObservableDictionary.TryGetValue(key, out var existingValue))
                        {
                            if (duplicateKeys.TryGetValue(key, out var duplicates))
                                duplicateKeys[key] = duplicates + count;
                            else
                                duplicateKeys.Add(key, count);
                        }
                        else
                        {
                            rangeObservableDictionary.Add(key, value);
                            if (count > 1)
                                duplicateKeys.Add(key, count - 1);
                        }
                    }
                    checkOperationFault();
                });

            void elementResultChanging(object sender, RangeActiveExpressionResultChangeEventArgs<TSource, KeyValuePair<TKey, TValue>> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    var result = e.Result;
                    var key = result.Key;
                    var count = e.Count;
                    if (key == null)
                        nullKeys -= count;
                    else
                    {
                        if (duplicateKeys.TryGetValue(key, out var duplicates))
                        {
                            if (duplicates <= count)
                                duplicateKeys.Remove(key);
                            else
                                duplicateKeys[key] = duplicates - count;
                        }
                        else
                            rangeObservableDictionary.Remove(key);
                    }
                    checkOperationFault();
                });

            void elementsAdded(object sender, RangeActiveExpressionMembershipEventArgs<TSource, KeyValuePair<TKey, TValue>> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    foreach (var (element, result) in e.ElementResults)
                    {
                        var key = result.Key;
                        if (key == null)
                            ++nullKeys;
                        else
                        {
                            var value = result.Value;
                            if (rangeObservableDictionary.TryGetValue(key, out var existingValue))
                            {
                                if (duplicateKeys.TryGetValue(key, out var duplicates))
                                    duplicateKeys[key] = duplicates + 1;
                                else
                                    duplicateKeys.Add(key, 1);
                            }
                            else
                                rangeObservableDictionary.Add(key, value);
                        }
                    }
                    checkOperationFault();
                });

            void elementsRemoved(object sender, RangeActiveExpressionMembershipEventArgs<TSource, KeyValuePair<TKey, TValue>> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    foreach (var (element, result) in e.ElementResults)
                    {
                        var key = result.Key;
                        if (key == null)
                            --nullKeys;
                        else
                        {
                            if (duplicateKeys.TryGetValue(key, out var duplicates))
                            {
                                if (duplicates == 1)
                                    duplicateKeys.Remove(key);
                                else
                                    duplicateKeys[key] = duplicates - 1;
                            }
                            else
                                rangeObservableDictionary.Remove(key);
                        }
                    }
                    checkOperationFault();
                });

            return synchronizedSource.SequentialExecute(() =>
            {
                switch (indexingStategy)
                {
                    case IndexingStrategy.SelfBalancingBinarySearchTree:
                        duplicateKeys = keyComparer == null ? new SortedDictionary<TKey, int>() : new SortedDictionary<TKey, int>(keyComparer);
                        rangeObservableDictionary = keyComparer == null ? new SynchronizedObservableSortedDictionary<TKey, TValue>() : new SynchronizedObservableSortedDictionary<TKey, TValue>(keyComparer);
                        break;
                    default:
                        duplicateKeys = keyEqualityComparer == null ? new Dictionary<TKey, int>() : new Dictionary<TKey, int>(keyEqualityComparer);
                        rangeObservableDictionary = keyEqualityComparer == null ? new SynchronizedObservableDictionary<TKey, TValue>() : new SynchronizedObservableDictionary<TKey, TValue>(keyEqualityComparer);
                        break;
                }

                var rangeActiveExpression = RangeActiveExpression.Create(source, selector, selectorOptions);
                rangeActiveExpression.ElementResultChanged += elementResultChanged;
                rangeActiveExpression.ElementResultChanging += elementResultChanging;
                rangeActiveExpression.ElementsAdded += elementsAdded;
                rangeActiveExpression.ElementsRemoved += elementsRemoved;

                ActiveLookup<TKey, TValue> activeLookup;
                var resultsFaultsAndCounts = rangeActiveExpression.GetResultsFaultsAndCounts();
                nullKeys = resultsFaultsAndCounts.Count(rfc => rfc.result.Key == null);
                var distinctResultsFaultsAndCounts = resultsFaultsAndCounts.Where(rfc => rfc.result.Key != null).GroupBy(rfc => rfc.result.Key).ToList();
                rangeObservableDictionary.AddRange(distinctResultsFaultsAndCounts.Select(g => g.First().result));
                foreach (var (key, duplicateCount) in distinctResultsFaultsAndCounts.Select(g => (key: g.Key, duplicateCount: g.Sum(rfc => rfc.count) - 1)).Where(kc => kc.duplicateCount > 0))
                    duplicateKeys.Add(key, duplicateCount);
                activeLookup = new ActiveLookup<TKey, TValue>(rangeObservableDictionary, out setOperationFault, rangeActiveExpression, () =>
                {
                    rangeActiveExpression.ElementResultChanged -= elementResultChanged;
                    rangeActiveExpression.ElementResultChanging -= elementResultChanging;
                    rangeActiveExpression.ElementsAdded -= elementsAdded;
                    rangeActiveExpression.ElementsRemoved -= elementsRemoved;
                    rangeActiveExpression.Dispose();
                });
                checkOperationFault();

                return activeLookup;
            });
        }

        #endregion ToActiveLookup

        #region Where

        public static ActiveEnumerable<TSource> ActiveWhere<TSource>(this IEnumerable<TSource> source, Expression<Func<TSource, bool>> predicate, ActiveExpressionOptions predicateOptions = null)
        {
            ActiveQueryOptions.Optimize(ref predicate);

            var synchronizedSource = source as ISynchronized;
            var rangeObservableCollectionAccess = new object();
            SynchronizedRangeObservableCollection<TSource> rangeObservableCollection = null;

            void elementResultChanged(object sender, RangeActiveExpressionResultChangeEventArgs<TSource, bool> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    if (e.Result)
                        rangeObservableCollection.AddRange(Enumerable.Range(0, e.Count).Select(i => e.Element));
                    else
                    {
                        var equalityComparer = EqualityComparer<TSource>.Default;
                        rangeObservableCollection.RemoveAll(element => equalityComparer.Equals(element, e.Element));
                    }
                });

            void elementsAdded(object sender, RangeActiveExpressionMembershipEventArgs<TSource, bool> e) => synchronizedSource.SequentialExecute(() => rangeObservableCollection.AddRange(e.ElementResults.Where(er => er.result).Select(er => er.element)));

            void elementsRemoved(object sender, RangeActiveExpressionMembershipEventArgs<TSource, bool> e) => synchronizedSource.SequentialExecute(() => rangeObservableCollection.RemoveRange(e.ElementResults.Where(er => er.result).Select(er => er.element)));

            return synchronizedSource.SequentialExecute(() =>
            {
                var rangeActiveExpression = RangeActiveExpression.Create(source, predicate, predicateOptions);
                rangeActiveExpression.ElementResultChanged += elementResultChanged;
                rangeActiveExpression.ElementsAdded += elementsAdded;
                rangeActiveExpression.ElementsRemoved += elementsRemoved;

                rangeObservableCollection = new SynchronizedRangeObservableCollection<TSource>(rangeActiveExpression.GetResults().Where(er => er.result).Select(er => er.element));
                return new ActiveEnumerable<TSource>(rangeObservableCollection, rangeActiveExpression, () =>
                {
                    rangeActiveExpression.ElementResultChanged -= elementResultChanged;
                    rangeActiveExpression.ElementsAdded -= elementsAdded;
                    rangeActiveExpression.ElementsRemoved -= elementsRemoved;

                    rangeActiveExpression.Dispose();
                });
            });
        }

        #endregion Where
    }
}
