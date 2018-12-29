using Gear.ActiveExpressions;
using Gear.Components;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;

namespace Gear.ActiveQuery
{
    public static class ActiveLookupExtensions
    {
        #region All

        public static ActiveValue<bool> ActiveAll<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, bool>> predicate, ActiveExpressionOptions predicateOptions = null)
        {
            ActiveQueryOptions.Optimize(ref predicate);

            var changeNotifyingSource = source as INotifyDictionaryChanged<TKey, TValue>;
            ActiveLookup<TKey, TValue> where;
            Action<bool> setValue = null;

            void dictionaryChanged(object sender, EventArgs e) => setValue(where.Count == source.Count);

            return (source as ISynchronized).SequentialExecute(() =>
            {
                where = ActiveWhere(source, predicate, predicateOptions);
                where.DictionaryChanged += dictionaryChanged;
                if (changeNotifyingSource != null)
                    changeNotifyingSource.DictionaryChanged += dictionaryChanged;

                return new ActiveValue<bool>(where.Count == source.Count, out setValue, elementFaultChangeNotifier: where, onDispose: () =>
                {
                    where.DictionaryChanged -= dictionaryChanged;
                    where.Dispose();
                    if (changeNotifyingSource != null)
                        changeNotifyingSource.DictionaryChanged -= dictionaryChanged;
                });
            });
        }

        #endregion All

        #region Any

        public static ActiveValue<bool> ActiveAny<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source)
        {
            var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
            if (source is INotifyDictionaryChanged<TKey, TValue> changeNotifyingSource)
            {
                var synchronizedSource = source as ISynchronized;
                Action<bool> setValue = null;

                void sourceChanged(object sender, EventArgs e) => synchronizedSource.SequentialExecute(() => setValue(source.Count > 0));

                return synchronizedSource.SequentialExecute(() =>
                {
                    changeNotifyingSource.DictionaryChanged += sourceChanged;
                    return new ActiveValue<bool>(source.Any(), out setValue, elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: () => changeNotifyingSource.DictionaryChanged -= sourceChanged);
                });
            }
            try
            {
                return new ActiveValue<bool>(source.Any(), elementFaultChangeNotifier: elementFaultChangeNotifier);
            }
            catch (Exception ex)
            {
                return new ActiveValue<bool>(default, ex, elementFaultChangeNotifier);
            }
        }

        public static ActiveValue<bool> ActiveAny<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, bool>> predicate, ActiveExpressionOptions predicateOptions = null)
        {
            ActiveQueryOptions.Optimize(ref predicate);

            var changeNotifyingSource = source as INotifyDictionaryChanged<TKey, TValue>;
            ActiveLookup<TKey, TValue> where;
            Action<bool> setValue = null;

            void dictionaryChanged(object sender, EventArgs e) => setValue(where.Count > 0);

            return (source as ISynchronized).SequentialExecute(() =>
            {
                where = ActiveWhere(source, predicate, predicateOptions);
                where.DictionaryChanged += dictionaryChanged;

                return new ActiveValue<bool>(where.Count > 0, out setValue, elementFaultChangeNotifier: where, onDispose: () =>
                {
                    where.DictionaryChanged -= dictionaryChanged;
                    where.Dispose();
                });
            });
        }

        #endregion Any

        #region Average

        public static ActiveValue<TValue> ActiveAverage<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source) =>
            ActiveAverage(source, (key, value) => value);

        public static ActiveValue<TResult> ActiveAverage<TKey, TValue, TResult>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, TResult>> selector, ActiveExpressionOptions selectorOptions = null)
        {
            ActiveQueryOptions.Optimize(ref selector);

            var convertCount = CountConversion.GetConverter(typeof(TResult));
            var operations = new GenericOperations<TResult>();
            var synchronizedSource = source as ISynchronized;
            ActiveValue<TResult> sum;
            Action<TResult> setValue = null;
            Action<Exception> setOperationFault = null;

            void propertyChanged(object sender, PropertyChangedEventArgs e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    if (e.PropertyName == nameof(ActiveValue<TResult>.Value))
                    {
                        var currentCount = source.Count;
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

                var currentCount = source.Count;
                return new ActiveValue<TResult>(currentCount > 0 ? operations.Divide(sum.Value, (TResult)convertCount(currentCount)) : default, out setValue, currentCount == 0 ? ExceptionHelper.SequenceContainsNoElements : null, out setOperationFault, sum, () =>
                {
                    sum.PropertyChanged -= propertyChanged;
                    sum.Dispose();
                });
            });
        }

        #endregion Average

        #region First

        public static ActiveValue<KeyValuePair<TKey, TValue>> ActiveFirst<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source)
        {
            var keyComparer = source.GetKeyComparer() ?? Comparer<TKey>.Default;
            var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
            if (source is INotifyDictionaryChanged<TKey, TValue> changingSource)
            {
                var synchronizedSource = source as ISynchronized;
                ActiveValue<KeyValuePair<TKey, TValue>> activeValue = null;
                Action<KeyValuePair<TKey, TValue>> setValue = null;
                Action<Exception> setOperationFault = null;

                void dispose() => changingSource.DictionaryChanged -= sourceChanged;

                void sourceChanged(object sender, NotifyDictionaryChangedEventArgs<TKey, TValue> e) =>
                    synchronizedSource.SequentialExecute(() =>
                    {
                        if (e.Action == NotifyDictionaryChangedAction.Reset)
                        {
                            try
                            {
                                setOperationFault(null);
                                setValue(source.OrderBy(kv => kv.Key, keyComparer).First());
                            }
                            catch (Exception ex)
                            {
                                setOperationFault(ex);
                                setValue(default);
                            }
                        }
                        else
                        {
                            if (e.OldItems?.Any(kv => keyComparer.Compare(kv.Key, activeValue.Value.Key) == 0) ?? false)
                            {
                                try
                                {
                                    setValue(source.OrderBy(kv => kv.Key, keyComparer).First());
                                }
                                catch (Exception ex)
                                {
                                    setOperationFault(ex);
                                    setValue(default);
                                }
                            }
                            if ((e.NewItems?.Count ?? 0) > 0)
                            {
                                var firstKv = e.NewItems.OrderBy(kv => kv.Key, keyComparer).First();
                                if (activeValue.OperationFault != null || keyComparer.Compare(firstKv.Key, activeValue.Value.Key) < 0)
                                {
                                    setOperationFault(null);
                                    setValue(firstKv);
                                }
                            }
                        }
                    });

                return synchronizedSource.SequentialExecute(() =>
                {
                    changingSource.DictionaryChanged += sourceChanged;
                    try
                    {
                        return activeValue = new ActiveValue<KeyValuePair<TKey, TValue>>(source.OrderBy(kv => kv.Key, keyComparer).First(), out setValue, out setOperationFault, elementFaultChangeNotifier, dispose);
                    }
                    catch (Exception ex)
                    {
                        return new ActiveValue<KeyValuePair<TKey, TValue>>(default, out setValue, ex, out setOperationFault, elementFaultChangeNotifier, dispose);
                    }
                });
            }
            try
            {
                return new ActiveValue<KeyValuePair<TKey, TValue>>(source.OrderBy(kv => kv.Key, keyComparer).First(), elementFaultChangeNotifier: elementFaultChangeNotifier);
            }
            catch (Exception ex)
            {
                return new ActiveValue<KeyValuePair<TKey, TValue>>(default, ex, elementFaultChangeNotifier);
            }
        }

        #endregion First

        #region FirstOrDefault

        public static ActiveValue<KeyValuePair<TKey, TValue>> ActiveFirstOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source)
        {
            var keyComparer = source.GetKeyComparer() ?? Comparer<TKey>.Default;
            var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
            if (source is INotifyDictionaryChanged<TKey, TValue> changingSource)
            {
                var synchronizedSource = source as ISynchronized;
                ActiveValue<KeyValuePair<TKey, TValue>> activeValue = null;
                Action<KeyValuePair<TKey, TValue>> setValue = null;
                var defaulted = false;

                void dispose() => changingSource.DictionaryChanged += sourceChanged;

                void sourceChanged(object sender, NotifyDictionaryChangedEventArgs<TKey, TValue> e) =>
                    synchronizedSource.SequentialExecute(() =>
                    {
                        if (e.Action == NotifyDictionaryChangedAction.Reset)
                        {
                            try
                            {
                                defaulted = false;
                                setValue(source.OrderBy(kv => kv.Key, keyComparer).First());
                            }
                            catch
                            {
                                defaulted = true;
                                setValue(default);
                            }
                        }
                        else
                        {
                            if (e.OldItems?.Any(kv => keyComparer.Compare(kv.Key, activeValue.Value.Key) == 0) ?? false)
                            {
                                try
                                {
                                    setValue(source.OrderBy(kv => kv.Key, keyComparer).First());
                                }
                                catch
                                {
                                    defaulted = true;
                                    setValue(default);
                                }
                            }
                            if ((e.NewItems?.Count ?? 0) > 0)
                            {
                                var firstKv = e.NewItems.OrderBy(kv => kv.Key, keyComparer).First();
                                if (defaulted || keyComparer.Compare(firstKv.Key, activeValue.Value.Key) < 0)
                                    setValue(firstKv);
                            }
                        }
                    });

                return synchronizedSource.SequentialExecute(() =>
                {
                    changingSource.DictionaryChanged += sourceChanged;
                    try
                    {
                        return activeValue = new ActiveValue<KeyValuePair<TKey, TValue>>(source.OrderBy(kv => kv.Key, keyComparer).First(), out setValue, elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: dispose);
                    }
                    catch
                    {
                        defaulted = true;
                        return new ActiveValue<KeyValuePair<TKey, TValue>>(default, out setValue, elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: dispose);
                    }
                });
            }
            try
            {
                return new ActiveValue<KeyValuePair<TKey, TValue>>(source.OrderBy(kv => kv.Key, keyComparer).First(), elementFaultChangeNotifier: elementFaultChangeNotifier);
            }
            catch (Exception ex)
            {
                return new ActiveValue<KeyValuePair<TKey, TValue>>(default, ex, elementFaultChangeNotifier);
            }
        }

        #endregion FirstOrDefault

        #region Last

        public static ActiveValue<KeyValuePair<TKey, TValue>> ActiveLast<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source)
        {
            var keyComparer = source.GetKeyComparer() ?? Comparer<TKey>.Default;
            var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
            if (source is INotifyDictionaryChanged<TKey, TValue> changingSource)
            {
                var synchronizedSource = source as ISynchronized;
                ActiveValue<KeyValuePair<TKey, TValue>> activeValue = null;
                Action<KeyValuePair<TKey, TValue>> setValue = null;
                Action<Exception> setOperationFault = null;

                void dispose() => changingSource.DictionaryChanged += sourceChanged;

                void sourceChanged(object sender, NotifyDictionaryChangedEventArgs<TKey, TValue> e) =>
                    synchronizedSource.SequentialExecute(() =>
                    {
                        if (e.Action == NotifyDictionaryChangedAction.Reset)
                        {
                            try
                            {
                                setOperationFault(null);
                                setValue(source.OrderByDescending(kv => kv.Key, keyComparer).First());
                            }
                            catch (Exception ex)
                            {
                                setOperationFault(ex);
                                setValue(default);
                            }
                        }
                        else
                        {
                            if (e.OldItems?.Any(kv => keyComparer.Compare(kv.Key, activeValue.Value.Key) == 0) ?? false)
                            {
                                try
                                {
                                    setValue(source.OrderByDescending(kv => kv.Key, keyComparer).First());
                                }
                                catch (Exception ex)
                                {
                                    setOperationFault(ex);
                                }
                            }
                            if ((e.NewItems?.Count ?? 0) > 0)
                            {
                                var lastKv = e.NewItems.OrderByDescending(kv => kv.Key, keyComparer).First();
                                if (activeValue.OperationFault != null || keyComparer.Compare(lastKv.Key, activeValue.Value.Key) > 0)
                                {
                                    setOperationFault(null);
                                    setValue(lastKv);
                                }
                            }
                        }
                    });

                return synchronizedSource.SequentialExecute(() =>
                {
                    changingSource.DictionaryChanged += sourceChanged;
                    try
                    {
                        return activeValue = new ActiveValue<KeyValuePair<TKey, TValue>>(source.OrderByDescending(kv => kv.Key, keyComparer).First(), out setValue, out setOperationFault, elementFaultChangeNotifier, dispose);
                    }
                    catch (Exception ex)
                    {
                        return new ActiveValue<KeyValuePair<TKey, TValue>>(default, out setValue, ex, out setOperationFault, elementFaultChangeNotifier, dispose);
                    }
                });
            }
            try
            {
                return new ActiveValue<KeyValuePair<TKey, TValue>>(source.OrderByDescending(kv => kv.Key, keyComparer).First(), elementFaultChangeNotifier: elementFaultChangeNotifier);
            }
            catch (Exception ex)
            {
                return new ActiveValue<KeyValuePair<TKey, TValue>>(default, ex, elementFaultChangeNotifier);
            }
        }

        #endregion Last

        #region LastOrDefault

        public static ActiveValue<KeyValuePair<TKey, TValue>> ActiveLastOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source)
        {
            var keyComparer = source.GetKeyComparer() ?? Comparer<TKey>.Default;
            var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
            if (source is INotifyDictionaryChanged<TKey, TValue> changingSource)
            {
                var synchronizedSource = source as ISynchronized;
                ActiveValue<KeyValuePair<TKey, TValue>> activeValue = null;
                Action<KeyValuePair<TKey, TValue>> setValue = null;
                var defaulted = false;

                void dispose() => changingSource.DictionaryChanged += sourceChanged;

                void sourceChanged(object sender, NotifyDictionaryChangedEventArgs<TKey, TValue> e) =>
                    synchronizedSource.SequentialExecute(() =>
                    {
                        if (e.Action == NotifyDictionaryChangedAction.Reset)
                        {
                            try
                            {
                                defaulted = false;
                                setValue(source.OrderByDescending(kv => kv.Key, keyComparer).First());
                            }
                            catch
                            {
                                defaulted = true;
                                setValue(default);
                            }
                        }
                        else
                        {
                            if (e.OldItems?.Any(kv => keyComparer.Compare(kv.Key, activeValue.Value.Key) == 0) ?? false)
                            {
                                try
                                {
                                    setValue(source.OrderByDescending(kv => kv.Key, keyComparer).First());
                                }
                                catch
                                {
                                    defaulted = true;
                                    setValue(default);
                                }
                            }
                            if ((e.NewItems?.Count ?? 0) > 0)
                            {
                                var firstKv = e.NewItems.OrderByDescending(kv => kv.Key, keyComparer).First();
                                if (defaulted || keyComparer.Compare(firstKv.Key, activeValue.Value.Key) > 0)
                                {
                                    defaulted = false;
                                    setValue(firstKv);
                                }
                            }
                        }
                    });

                return synchronizedSource.SequentialExecute(() =>
                {
                    changingSource.DictionaryChanged += sourceChanged;
                    try
                    {
                        return activeValue = new ActiveValue<KeyValuePair<TKey, TValue>>(source.OrderByDescending(kv => kv.Key, keyComparer).First(), out setValue, elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: dispose);
                    }
                    catch
                    {
                        defaulted = true;
                        return new ActiveValue<KeyValuePair<TKey, TValue>>(default, out setValue, elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: dispose);
                    }
                });
            }
            try
            {
                return new ActiveValue<KeyValuePair<TKey, TValue>>(source.OrderByDescending(kv => kv.Key, keyComparer).First(), elementFaultChangeNotifier: elementFaultChangeNotifier);
            }
            catch (Exception ex)
            {
                return new ActiveValue<KeyValuePair<TKey, TValue>>(default, ex, elementFaultChangeNotifier);
            }
        }

        #endregion LastOrDefault

        #region Max

        public static ActiveValue<TValue> ActiveMax<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source) =>
            ActiveMax(source, (key, value) => value);

        public static ActiveValue<TResult> ActiveMax<TKey, TValue, TResult>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, TResult>> selector, ActiveExpressionOptions selectorOptions = null)
        {
            ActiveQueryOptions.Optimize(ref selector);

            var comparer = Comparer<TResult>.Default;
            var synchronizedSource = source as ISynchronized;
            ReadOnlyDictionaryRangeActiveExpression<TKey, TValue, TResult> rangeActiveExpression;
            ActiveValue<TResult> activeValue = null;
            Action<TResult> setValue = null;
            Action<Exception> setOperationFault = null;

            void dispose()
            {
                rangeActiveExpression.DictionaryChanged -= rangeActiveExpressionChanged;
                rangeActiveExpression.ValueResultChanged -= valueResultChanged;
                rangeActiveExpression.Dispose();
            }

            void rangeActiveExpressionChanged(object sender, NotifyDictionaryChangedEventArgs<TKey, TResult> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    if (e.Action == NotifyDictionaryChangedAction.Reset)
                    {
                        try
                        {
                            setOperationFault(null);
                            setValue(rangeActiveExpression.GetResults().Select(kr => kr.result).Max());
                        }
                        catch (Exception ex)
                        {
                            setOperationFault(ex);
                            setValue(default);
                        }
                    }
                    else
                    {
                        if ((e.OldItems?.Count ?? 0) > 0)
                        {
                            var removedMax = e.OldItems.Select(kv => kv.Value).Max();
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
                        if ((e.NewItems?.Count ?? 0) > 0)
                        {
                            var addedMax = e.NewItems.Select(kv => kv.Value).Max();
                            if (activeValue.OperationFault != null || comparer.Compare(activeValue.Value, addedMax) < 0)
                            {
                                setOperationFault(null);
                                setValue(addedMax);
                            }
                        }
                    }
                });

            void valueResultChanged(object sender, RangeActiveExpressionResultChangeEventArgs<TKey, TResult> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    var comparison = comparer.Compare(activeValue.Value, e.Result);
                    if (comparison < 0)
                        setValue(e.Result);
                    else if (comparison > 0)
                        setValue(rangeActiveExpression.GetResultsUnderLock().Select(er => er.result).Max());
                });

            return synchronizedSource.SequentialExecute(() =>
            {
                rangeActiveExpression = RangeActiveExpression.Create(source, selector, selectorOptions);
                rangeActiveExpression.DictionaryChanged += rangeActiveExpressionChanged;
                rangeActiveExpression.ValueResultChanged += valueResultChanged;

                try
                {
                    return activeValue = new ActiveValue<TResult>(rangeActiveExpression.GetResults().Select(kr => kr.result).Max(), out setValue, null, out setOperationFault, rangeActiveExpression, dispose);
                }
                catch (Exception ex)
                {
                    return activeValue = new ActiveValue<TResult>(default, out setValue, ex, out setOperationFault, rangeActiveExpression, dispose);
                }
            });
        }

        #endregion Max

        #region Min

        public static ActiveValue<TValue> ActiveMin<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source) =>
            ActiveMin(source, (key, value) => value);

        public static ActiveValue<TResult> ActiveMin<TKey, TValue, TResult>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, TResult>> selector, ActiveExpressionOptions selectorOptions = null)
        {
            ActiveQueryOptions.Optimize(ref selector);

            var comparer = Comparer<TResult>.Default;
            var synchronizedSource = source as ISynchronized;
            ReadOnlyDictionaryRangeActiveExpression<TKey, TValue, TResult> rangeActiveExpression;
            ActiveValue<TResult> activeValue = null;
            Action<TResult> setValue = null;
            Action<Exception> setOperationFault = null;

            void dispose()
            {
                rangeActiveExpression.DictionaryChanged -= rangeActiveExpressionChanged;
                rangeActiveExpression.ValueResultChanged -= valueResultChanged;
                rangeActiveExpression.Dispose();
            }

            void rangeActiveExpressionChanged(object sender, NotifyDictionaryChangedEventArgs<TKey, TResult> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    if (e.Action == NotifyDictionaryChangedAction.Reset)
                    {
                        try
                        {
                            setOperationFault(null);
                            setValue(rangeActiveExpression.GetResults().Select(kr => kr.result).Min());
                        }
                        catch (Exception ex)
                        {
                            setOperationFault(ex);
                            setValue(default);
                        }
                    }
                    else
                    {
                        if ((e.OldItems?.Count ?? 0) > 0)
                        {
                            var removedMin = e.OldItems.Select(kv => kv.Value).Min();
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
                        if ((e.NewItems?.Count ?? 0) > 0)
                        {
                            var addedMin = e.NewItems.Select(kv => kv.Value).Min();
                            if (activeValue.OperationFault != null || comparer.Compare(activeValue.Value, addedMin) > 0)
                            {
                                setOperationFault(null);
                                setValue(addedMin);
                            }
                        }
                    }
                });

            void valueResultChanged(object sender, RangeActiveExpressionResultChangeEventArgs<TKey, TResult> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    var comparison = comparer.Compare(activeValue.Value, e.Result);
                    if (comparison > 0)
                        setValue(e.Result);
                    else if (comparison < 0)
                        setValue(rangeActiveExpression.GetResultsUnderLock().Select(er => er.result).Min());
                });

            return synchronizedSource.SequentialExecute(() =>
            {
                rangeActiveExpression = RangeActiveExpression.Create(source, selector, selectorOptions);
                rangeActiveExpression.DictionaryChanged += rangeActiveExpressionChanged;
                rangeActiveExpression.ValueResultChanged += valueResultChanged;

                try
                {
                    return activeValue = new ActiveValue<TResult>(rangeActiveExpression.GetResults().Select(kr => kr.result).Min(), out setValue, null, out setOperationFault, rangeActiveExpression, dispose);
                }
                catch (Exception ex)
                {
                    return activeValue = new ActiveValue<TResult>(default, out setValue, ex, out setOperationFault, rangeActiveExpression, dispose);
                }
            });
        }

        #endregion Min

        #region Select

        public static ActiveEnumerable<TResult> ActiveSelect<TKey, TValue, TResult>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, TResult>> selector, ActiveExpressionOptions selectorOptions = null)
        {
            ActiveQueryOptions.Optimize(ref selector);

            var synchronizedSource = source as ISynchronized;
            ReadOnlyDictionaryRangeActiveExpression<TKey, TValue, TResult> rangeActiveExpression;
            var keyToIndex = source.CreateSimilarDictionary<TKey, TValue, int>();
            SynchronizedRangeObservableCollection<TResult> rangeObservableCollection = null;

            void rangeActiveExpressionChanged(object sender, NotifyDictionaryChangedEventArgs<TKey, TResult> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    if (e.Action == NotifyDictionaryChangedAction.Reset)
                    {
                        rangeObservableCollection.Reset(rangeActiveExpression.GetResults().Select(((TKey key, TResult result) er, int index) =>
                        {
                            keyToIndex.Add(er.key, index);
                            return er.result;
                        }));
                    }
                    else
                    {
                        if ((e.OldItems?.Count ?? 0) > 0)
                        {
                            var removingIndicies = new List<int>();
                            foreach (var kv in e.OldItems)
                            {
                                var key = kv.Key;
                                removingIndicies.Add(keyToIndex[key]);
                                keyToIndex.Remove(key);
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
                            var revisedKeyedIndicies = keyToIndex.OrderBy(kv => kv.Value);
                            keyToIndex = source.CreateSimilarDictionary<TKey, TValue, int>();
                            foreach (var (key, index) in revisedKeyedIndicies.Select((kv, index) => (kv.Key, index)))
                                keyToIndex.Add(key, index);
                        }
                        if ((e.NewItems?.Count ?? 0) > 0)
                        {
                            var lastIndex = keyToIndex.Count - 1;
                            rangeObservableCollection.AddRange(e.NewItems.Select((KeyValuePair<TKey, TResult> kv, int index) =>
                            {
                                keyToIndex.Add(kv.Key, lastIndex + index);
                                return kv.Value;
                            }));
                        }
                    }
                });

            void valueResultChanged(object sender, RangeActiveExpressionResultChangeEventArgs<TKey, TResult> e) => synchronizedSource.SequentialExecute(() => rangeObservableCollection.Replace(keyToIndex[e.Element], e.Result));

            return synchronizedSource.SequentialExecute(() =>
            {
                rangeActiveExpression = RangeActiveExpression.Create(source, selector, selectorOptions);
                rangeActiveExpression.DictionaryChanged += rangeActiveExpressionChanged;
                rangeActiveExpression.ValueResultChanged += valueResultChanged;

                rangeObservableCollection = new SynchronizedRangeObservableCollection<TResult>(rangeActiveExpression.GetResults().Select(((TKey key, TResult result) er, int index) =>
                {
                    keyToIndex.Add(er.key, index);
                    return er.result;
                }));
                return new ActiveEnumerable<TResult>(rangeObservableCollection, rangeActiveExpression, () =>
                {
                    rangeActiveExpression.DictionaryChanged -= rangeActiveExpressionChanged;
                    rangeActiveExpression.ValueResultChanged -= valueResultChanged;
                    rangeActiveExpression.Dispose();
                });
            });
        }

        #endregion Select

        #region Single

        public static ActiveValue<KeyValuePair<TKey, TValue>> ActiveSingle<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source)
        {
            var keyEqualityComparer = source.GetKeyEqualityComparer() ?? EqualityComparer<TKey>.Default;
            var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
            if (source is INotifyDictionaryChanged<TKey, TValue> changingSource)
            {
                var synchronizedSource = source as ISynchronized;
                Action<KeyValuePair<TKey, TValue>> setValue = null;
                Action<Exception> setOperationFault = null;
                bool none = false, moreThanOne = false;

                void dispose() => changingSource.DictionaryChanged -= sourceChanged;

                void sourceChanged(object sender, NotifyDictionaryChangedEventArgs<TKey, TValue> e) =>
                    synchronizedSource.SequentialExecute(() =>
                    {
                        if (source.Count == 1)
                        {
                            none = false;
                            moreThanOne = false;
                            setOperationFault(null);
                            setValue(source.First());
                        }
                        else
                        {
                            if (source.Count == 0 && !none)
                            {
                                none = true;
                                moreThanOne = false;
                                setOperationFault(ExceptionHelper.SequenceContainsNoElements);
                            }
                            else if (!moreThanOne)
                            {
                                none = false;
                                moreThanOne = true;
                                setOperationFault(ExceptionHelper.SequenceContainsMoreThanOneElement);
                            }
                            setValue(default);
                        }
                    });

                return synchronizedSource.SequentialExecute(() =>
                {
                    changingSource.DictionaryChanged += sourceChanged;
                    switch (source.Count)
                    {
                        case 0:
                            none = true;
                            return new ActiveValue<KeyValuePair<TKey, TValue>>(default, out setValue, ExceptionHelper.SequenceContainsNoElements, out setOperationFault, elementFaultChangeNotifier, dispose);
                        case 1:
                            return new ActiveValue<KeyValuePair<TKey, TValue>>(source.Single(), out setValue, out setOperationFault, elementFaultChangeNotifier, dispose);
                        default:
                            moreThanOne = true;
                            return new ActiveValue<KeyValuePair<TKey, TValue>>(default, out setValue, ExceptionHelper.SequenceContainsMoreThanOneElement, out setOperationFault, elementFaultChangeNotifier, dispose);
                    }
                });
            }
            try
            {
                return new ActiveValue<KeyValuePair<TKey, TValue>>(source.Single(), elementFaultChangeNotifier: elementFaultChangeNotifier);
            }
            catch (Exception ex)
            {
                return new ActiveValue<KeyValuePair<TKey, TValue>>(default, ex, elementFaultChangeNotifier);
            }
        }

        #endregion Single

        #region SingleOrDefault

        public static ActiveValue<KeyValuePair<TKey, TValue>> ActiveSingleOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source)
        {
            var keyEqualityComparer = source.GetKeyEqualityComparer() ?? EqualityComparer<TKey>.Default;
            var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
            if (source is INotifyDictionaryChanged<TKey, TValue> changingSource)
            {
                var synchronizedSource = source as ISynchronized;
                ActiveValue<KeyValuePair<TKey, TValue>> activeValue = null;
                Action<KeyValuePair<TKey, TValue>> setValue = null;
                Action<Exception> setOperationFault = null;

                void dispose() => changingSource.DictionaryChanged += sourceChanged;

                void sourceChanged(object sender, NotifyDictionaryChangedEventArgs<TKey, TValue> e) =>
                    synchronizedSource.SequentialExecute(() =>
                    {
                        switch (source.Count)
                        {
                            case 0:
                                setOperationFault(null);
                                setValue(default);
                                break;
                            case 1:
                                setOperationFault(null);
                                setValue(source.First());
                                break;
                            default:
                                if (activeValue.OperationFault == null)
                                    setOperationFault(ExceptionHelper.SequenceContainsMoreThanOneElement);
                                setValue(default);
                                break;
                        }
                    });

                return synchronizedSource.SequentialExecute(() =>
                {
                    changingSource.DictionaryChanged += sourceChanged;
                    switch (source.Count)
                    {
                        case 0:
                            return activeValue = new ActiveValue<KeyValuePair<TKey, TValue>>(default, out setValue, out setOperationFault, elementFaultChangeNotifier, dispose);
                        case 1:
                            return activeValue = new ActiveValue<KeyValuePair<TKey, TValue>>(source.First(), out setValue, out setOperationFault, elementFaultChangeNotifier, dispose);
                        default:
                            return activeValue = new ActiveValue<KeyValuePair<TKey, TValue>>(default, out setValue, ExceptionHelper.SequenceContainsMoreThanOneElement, out setOperationFault, elementFaultChangeNotifier, dispose);
                    }
                });
            }
            try
            {
                return new ActiveValue<KeyValuePair<TKey, TValue>>(source.SingleOrDefault(), elementFaultChangeNotifier: elementFaultChangeNotifier);
            }
            catch (Exception ex)
            {
                return new ActiveValue<KeyValuePair<TKey, TValue>>(default, ex, elementFaultChangeNotifier);
            }
        }

        #endregion SingleOrDefault

        #region Sum

        public static ActiveValue<TResult> ActiveSum<TKey, TValue, TResult>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, TResult>> selector, ActiveExpressionOptions selectorOptions = null)
        {
            ActiveQueryOptions.Optimize(ref selector);

            var operations = new GenericOperations<TResult>();
            var synchronizedSource = source as ISynchronized;
            ReadOnlyDictionaryRangeActiveExpression<TKey, TValue, TResult> rangeActiveExpression;
            ActiveValue<TResult> activeValue = null;
            Action<TResult> setValue = null;
            var valuesChanging = new Dictionary<TKey, TResult>();

            void rangeActiveExpressionChanged(object sender, NotifyDictionaryChangedEventArgs<TKey, TResult> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    if (e.Action == NotifyDictionaryChangedAction.Reset)
                        setValue(rangeActiveExpression.GetResults().Select(kr => kr.result).Aggregate((a, b) => operations.Add(a, b)));
                    else
                    {
                        var sum = activeValue.Value;
                        if ((e.OldItems?.Count ?? 0) > 0)
                            sum = new TResult[] { activeValue.Value }.Concat(e.OldItems.Select(kv => kv.Value)).Aggregate(operations.Subtract);
                        if ((e.NewItems?.Count ?? 0) > 0)
                            sum = new TResult[] { activeValue.Value }.Concat(e.NewItems.Select(kv => kv.Value)).Aggregate(operations.Add);
                        setValue(sum);
                    }
                });

            void valueResultChanged(object sender, RangeActiveExpressionResultChangeEventArgs<TKey, TResult> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    var key = e.Element;
                    setValue(operations.Add(activeValue.Value, operations.Subtract(e.Result, valuesChanging[key])));
                    valuesChanging.Remove(key);
                });

            void valueResultChanging(object sender, RangeActiveExpressionResultChangeEventArgs<TKey, TResult> e) => synchronizedSource.SequentialExecute(() => valuesChanging.Add(e.Element, e.Result));

            return synchronizedSource.SequentialExecute(() =>
            {
                rangeActiveExpression = RangeActiveExpression.Create(source, selector, selectorOptions);
                rangeActiveExpression.DictionaryChanged += rangeActiveExpressionChanged;
                rangeActiveExpression.ValueResultChanged += valueResultChanged;
                rangeActiveExpression.ValueResultChanging += valueResultChanging;

                return activeValue = new ActiveValue<TResult>(rangeActiveExpression.GetResults().Select(kr => kr.result).Aggregate((a, b) => operations.Add(a, b)), out setValue, null, rangeActiveExpression, () =>
                {
                    rangeActiveExpression.DictionaryChanged -= rangeActiveExpressionChanged;
                    rangeActiveExpression.ValueResultChanged -= valueResultChanged;
                    rangeActiveExpression.ValueResultChanging -= valueResultChanging;
                    rangeActiveExpression.Dispose();
                });
            });
        }

        #endregion Sum

        #region ToActiveEnumerable

        public static ActiveEnumerable<TValue> ToActiveEnumerable<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source) =>
            ActiveSelect(source, (key, value) => value);

        #endregion ToActiveEnumerable

        #region ValueFor

        public static ActiveValue<TValue> ActiveValueFor<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, TKey key)
        {
            var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
            if (source is INotifyDictionaryChanged<TKey, TValue> changingSource)
            {
                var synchronizedSource = source as ISynchronized;
                Action<TValue> setValue = null;
                Action<Exception> setOperationFault = null;

                Func<TKey, bool> equalsKey;
                var keyComparer = source.GetKeyComparer();
                if (keyComparer != null)
                    equalsKey = otherKey => keyComparer.Compare(otherKey, key) == 0;
                else 
                {
                    var keyEqualityComparer = source.GetKeyEqualityComparer() ?? EqualityComparer<TKey>.Default;
                    equalsKey = otherKey => keyEqualityComparer.Equals(otherKey, key);
                }

                void dispose() => changingSource.DictionaryChanged -= sourceChanged;

                void sourceChanged(object sender, NotifyDictionaryChangedEventArgs<TKey, TValue> e) =>
                    synchronizedSource.SequentialExecute(() =>
                    {
                        if (e.Action == NotifyDictionaryChangedAction.Reset)
                        {
                            try
                            {
                                setOperationFault(null);
                                setValue(source[key]);
                            }
                            catch (Exception ex)
                            {
                                setOperationFault(ex);
                            }
                        }
                        else
                        {
                            if (e.OldItems?.Any(kv => equalsKey(kv.Key)) ?? false)
                            {
                                setOperationFault(ExceptionHelper.KeyNotFound);
                                setValue(default);
                            }
                            if ((e.NewItems?.Count ?? 0) > 0)
                            {
                                var matchingValues = e.NewItems.Where(kv => equalsKey(kv.Key)).Select(kv => kv.Value).ToList();
                                if (matchingValues.Count > 0)
                                {
                                    setOperationFault(null);
                                    setValue(matchingValues[0]);
                                }
                            }
                        }
                    });

                return synchronizedSource.SequentialExecute(() =>
                {
                    changingSource.DictionaryChanged += sourceChanged;
                    try
                    {
                        return new ActiveValue<TValue>(source[key], out setValue, out setOperationFault, elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: dispose);
                    }
                    catch (Exception ex)
                    {
                        return new ActiveValue<TValue>(default, out setValue, ex, out setOperationFault, elementFaultChangeNotifier, dispose);
                    }
                });
            }
            try
            {
                return new ActiveValue<TValue>(source[key], elementFaultChangeNotifier: elementFaultChangeNotifier);
            }
            catch (Exception ex)
            {
                return new ActiveValue<TValue>(default, ex, elementFaultChangeNotifier);
            }
        }

        #endregion ValueFor

        #region ValueForOrDefault

        public static ActiveValue<TValue> ActiveValueForOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, TKey key)
        {
            var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
            if (source is INotifyDictionaryChanged<TKey, TValue> changingSource)
            {
                var synchronizedSource = source as ISynchronized;
                Action<TValue> setValue = null;

                Func<TKey, bool> equalsKey;
                var keyComparer = source.GetKeyComparer();
                if (keyComparer != null)
                    equalsKey = otherKey => keyComparer.Compare(otherKey, key) == 0;
                else
                {
                    var keyEqualityComparer = source.GetKeyEqualityComparer() ?? EqualityComparer<TKey>.Default;
                    equalsKey = otherKey => keyEqualityComparer.Equals(otherKey, key);
                }

                void dispose() => changingSource.DictionaryChanged -= sourceChanged;

                void sourceChanged(object sender, NotifyDictionaryChangedEventArgs<TKey, TValue> e) =>
                    synchronizedSource.SequentialExecute(() =>
                    {
                        if (e.Action == NotifyDictionaryChangedAction.Reset)
                            setValue(source.TryGetValue(key, out var value) ? value : default);
                        else
                        {
                            if (e.OldItems?.Any(kv => equalsKey(kv.Key)) ?? false)
                                setValue(default);
                            if ((e.NewItems?.Count ?? 0) > 0)
                            {
                                var matchingValues = e.NewItems.Where(kv => equalsKey(kv.Key)).Select(kv => kv.Value).ToList();
                                if (matchingValues.Count > 0)
                                    setValue(matchingValues[0]);
                            }
                        }
                    });

                return synchronizedSource.SequentialExecute(() =>
                {
                    changingSource.DictionaryChanged += sourceChanged;
                    return new ActiveValue<TValue>(source.TryGetValue(key, out var value) ? value : default, out setValue, elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: dispose);
                });
            }
            else
                return new ActiveValue<TValue>(source.TryGetValue(key, out var value) ? value : default, elementFaultChangeNotifier: elementFaultChangeNotifier);
        }

        #endregion ValueForOrDefault

        #region Where

        public static ActiveLookup<TKey, TValue> ActiveWhere<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, bool>> predicate, ActiveExpressionOptions predicateOptions = null)
        {
            ActiveQueryOptions.Optimize(ref predicate);

            var synchronizedSource = source as ISynchronized;
            ReadOnlyDictionaryRangeActiveExpression<TKey, TValue, bool> rangeActiveExpression;
            ISynchronizedObservableRangeDictionary<TKey, TValue> rangeObservableDictionary = null;

            void rangeActiveExpressionChanged(object sender, NotifyDictionaryChangedEventArgs<TKey, bool> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    if (e.Action == NotifyDictionaryChangedAction.Reset)
                    {
                        var newDictionary = source.CreateSimilarDictionary();
                        foreach (var result in rangeActiveExpression.GetResults().Where(r => r.result))
                            newDictionary.Add(result.key, source[result.key]);
                        rangeObservableDictionary.Reset(newDictionary);
                    }
                    else
                    {
                        if ((e.OldItems?.Count ?? 0) > 0)
                            rangeObservableDictionary.RemoveRange(e.OldItems.Where(kv => kv.Value).Select(kv => kv.Key));
                        if ((e.NewItems?.Count ?? 0) > 0)
                            rangeObservableDictionary.AddRange(e.NewItems.Where(kv => kv.Value).Select(kv =>
                            {
                                var key = kv.Key;
                                return new KeyValuePair<TKey, TValue>(key, source[key]);
                            }));
                    }
                });

            void valueResultChanged(object sender, RangeActiveExpressionResultChangeEventArgs<TKey, bool> e) =>
                synchronizedSource.SequentialExecute(() =>
                {
                    var key = e.Element;
                    if (e.Result)
                        rangeObservableDictionary.Add(key, source[key]);
                    else
                        rangeObservableDictionary.Remove(key);
                });

            return synchronizedSource.SequentialExecute(() =>
            {
                rangeActiveExpression = RangeActiveExpression.Create(source, predicate, predicateOptions);
                rangeActiveExpression.DictionaryChanged += rangeActiveExpressionChanged;
                rangeActiveExpression.ValueResultChanged += valueResultChanged;

                rangeObservableDictionary = source.CreateSimilarSynchronizedObservableDictionary();
                rangeObservableDictionary.AddRange(rangeActiveExpression.GetResults().Where(r => r.result).Select(r => new KeyValuePair<TKey, TValue>(r.key, source[r.key])));
                return new ActiveLookup<TKey, TValue>(rangeObservableDictionary, rangeActiveExpression, () =>
                {
                    rangeActiveExpression.DictionaryChanged -= rangeActiveExpressionChanged;
                    rangeActiveExpression.ValueResultChanged -= valueResultChanged;
                    rangeActiveExpression.Dispose();
                });
            });
        }

        #endregion Where
    }
}
