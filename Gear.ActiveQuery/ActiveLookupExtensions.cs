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
            var activeValueAccess = new object();
            ActiveLookup<TKey, TValue> where;
            Action<bool> setValue = null;

            void whereChanged(object sender, EventArgs e)
            {
                lock (activeValueAccess)
                    setValue(where.Count == source.Count);
            }

            lock (activeValueAccess)
            {
                where = ActiveWhere(source, predicate, predicateOptions);
                where.ValueAdded += whereChanged;
                where.ValueRemoved += whereChanged;
                where.ValuesAdded += whereChanged;
                where.ValuesRemoved += whereChanged;

                return new ActiveValue<bool>(where.Count == source.Count, out setValue, elementFaultChangeNotifier: where, onDispose: () =>
                {
                    where.ValueAdded -= whereChanged;
                    where.ValueRemoved -= whereChanged;
                    where.ValuesAdded -= whereChanged;
                    where.ValuesRemoved -= whereChanged;
                    where.Dispose();
                });
            }
        }

        #endregion All

        #region Any

        public static ActiveValue<bool> ActiveAny<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source)
        {
            var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
            if (source is INotifyDictionaryChanged<TKey, TValue> changeNotifyingSource)
            {
                var activeValueAccess = new object();
                Action<bool> setValue = null;

                void sourceChanged(object sender, EventArgs e)
                {
                    lock (activeValueAccess)
                        setValue(source.Any());
                }

                lock (activeValueAccess)
                {
                    changeNotifyingSource.ValueAdded += sourceChanged;
                    changeNotifyingSource.ValueRemoved += sourceChanged;
                    changeNotifyingSource.ValuesAdded += sourceChanged;
                    changeNotifyingSource.ValuesRemoved += sourceChanged;

                    return new ActiveValue<bool>(source.Any(), out setValue, elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: () =>
                    {
                        changeNotifyingSource.ValueAdded -= sourceChanged;
                        changeNotifyingSource.ValueRemoved -= sourceChanged;
                        changeNotifyingSource.ValuesAdded -= sourceChanged;
                        changeNotifyingSource.ValuesRemoved -= sourceChanged;
                    });
                }
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
            var activeValueAccess = new object();
            ActiveLookup<TKey, TValue> where;
            Action<bool> setValue = null;

            void whereChanged(object sender, EventArgs e)
            {
                lock (activeValueAccess)
                    setValue(where.Count > 0);
            }

            lock (activeValueAccess)
            {
                where = ActiveWhere(source, predicate, predicateOptions);
                where.ValueAdded += whereChanged;
                where.ValueRemoved += whereChanged;
                where.ValuesAdded += whereChanged;
                where.ValuesRemoved += whereChanged;

                return new ActiveValue<bool>(where.Count > 0, out setValue, elementFaultChangeNotifier: where, onDispose: () =>
                {
                    where.ValueAdded -= whereChanged;
                    where.ValueRemoved -= whereChanged;
                    where.ValuesAdded -= whereChanged;
                    where.ValuesRemoved -= whereChanged;
                    where.Dispose();
                });
            }
        }

        #endregion Any

        #region Average

        public static ActiveValue<TValue> ActiveAverage<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source) =>
            ActiveAverage(source, (key, value) => value);

        public static ActiveValue<TResult> ActiveAverage<TKey, TValue, TResult>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, TResult>> selector, ActiveExpressionOptions selectorOptions = null)
        {
            var convertCount = CountConversion.GetConverter(typeof(TResult));
            var operations = new GenericOperations<TResult>();
            var activeValueAccess = new object();
            ActiveValue<TResult> sum;
            Action<TResult> setValue = null;
            Action<Exception> setOperationFault = null;

            void propertyChanged(object sender, PropertyChangedEventArgs e)
            {
                lock (activeValueAccess)
                {
                    if (e.PropertyName == nameof(ActiveValue<TResult>.Value))
                    {
                        var currentCount = source.Count;
                        if (currentCount == 0)
                            setOperationFault(ExceptionHelper.SequenceContainsNoElements);
                        else
                        {
                            setOperationFault(null);
                            setValue(operations.Divide(sum.Value, (TResult)convertCount(currentCount)));
                        }
                    }
                }
            }

            lock (activeValueAccess)
            {
                sum = ActiveSum(source, selector, selectorOptions);
                sum.PropertyChanged += propertyChanged;

                var currentCount = source.Count;
                return new ActiveValue<TResult>(currentCount > 0 ? operations.Divide(sum.Value, (TResult)convertCount(currentCount)) : default, out setValue, currentCount == 0 ? ExceptionHelper.SequenceContainsNoElements : null, out setOperationFault, sum, () =>
                {
                    sum.PropertyChanged -= propertyChanged;
                    sum.Dispose();
                });
            }
        }

        #endregion Average

        #region First

        public static ActiveValue<KeyValuePair<TKey, TValue>> ActiveFirst<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source)
        {
            var keyComparer = source.GetKeyComparer() ?? Comparer<TKey>.Default;
            var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
            if (source is INotifyDictionaryChanged<TKey, TValue> changingSource)
            {
                var activeValueAccess = new object();
                ActiveValue<KeyValuePair<TKey, TValue>> activeValue = null;
                Action<KeyValuePair<TKey, TValue>> setValue = null;
                Action<Exception> setOperationFault = null;

                void dispose()
                {
                    changingSource.ValueAdded += valueAdded;
                    changingSource.ValueRemoved += valueRemoved;
                    changingSource.ValueReplaced += valueReplaced;
                    changingSource.ValuesAdded += valuesAdded;
                    changingSource.ValuesRemoved += valuesRemoved;
                }

                void valueAdded(object sender, NotifyDictionaryValueEventArgs<TKey, TValue> e)
                {
                    lock (activeValueAccess)
                        if (activeValue.OperationFault != null || keyComparer.Compare(e.Key, activeValue.Value.Key) < 0)
                        {
                            setOperationFault(null);
                            setValue(new KeyValuePair<TKey, TValue>(e.Key, e.Value));
                        }
                }

                void valueRemoved(object sender, NotifyDictionaryValueEventArgs<TKey, TValue> e)
                {
                    lock (activeValueAccess)
                        if (keyComparer.Compare(e.Key, activeValue.Value.Key) == 0)
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
                }

                void valueReplaced(object sender, NotifyDictionaryValueReplacedEventArgs<TKey, TValue> e)
                {
                    lock (activeValueAccess)
                        if (keyComparer.Compare(e.Key, activeValue.Value.Key) == 0)
                            setValue(new KeyValuePair<TKey, TValue>(e.Key, e.NewValue));
                }

                void valuesAdded(object sender, NotifyDictionaryValuesEventArgs<TKey, TValue> e)
                {
                    lock (activeValueAccess)
                    {
                        var keyValuePairs = e.KeyValuePairs;
                        if (keyValuePairs.Count > 0)
                        {
                            var firstKv = keyValuePairs.OrderBy(kv => kv.Key, keyComparer).First();
                            if (activeValue.OperationFault != null || keyComparer.Compare(firstKv.Key, activeValue.Value.Key) < 0)
                            {
                                setOperationFault(null);
                                setValue(firstKv);
                            }
                        }
                    }
                }

                void valuesRemoved(object sender, NotifyDictionaryValuesEventArgs<TKey, TValue> e)
                {
                    lock (activeValueAccess)
                        if (e.KeyValuePairs.Any(kv => keyComparer.Compare(kv.Key, activeValue.Value.Key) == 0))
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
                }

                lock (activeValueAccess)
                {
                    changingSource.ValueAdded += valueAdded;
                    changingSource.ValueRemoved += valueRemoved;
                    changingSource.ValueReplaced += valueReplaced;
                    changingSource.ValuesAdded += valuesAdded;
                    changingSource.ValuesRemoved += valuesRemoved;
                    try
                    {
                        return activeValue = new ActiveValue<KeyValuePair<TKey, TValue>>(source.OrderBy(kv => kv.Key, keyComparer).First(), out setValue, out setOperationFault, elementFaultChangeNotifier, dispose);
                    }
                    catch (Exception ex)
                    {
                        return new ActiveValue<KeyValuePair<TKey, TValue>>(default, out setValue, ex, out setOperationFault, elementFaultChangeNotifier, dispose);
                    }
                }
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
                var activeValueAccess = new object();
                ActiveValue<KeyValuePair<TKey, TValue>> activeValue = null;
                Action<KeyValuePair<TKey, TValue>> setValue = null;
                var defaulted = false;

                void dispose()
                {
                    changingSource.ValueAdded += valueAdded;
                    changingSource.ValueRemoved += valueRemoved;
                    changingSource.ValueReplaced += valueReplaced;
                    changingSource.ValuesAdded += valuesAdded;
                    changingSource.ValuesRemoved += valuesRemoved;
                }

                void valueAdded(object sender, NotifyDictionaryValueEventArgs<TKey, TValue> e)
                {
                    lock (activeValueAccess)
                        if (defaulted || keyComparer.Compare(e.Key, activeValue.Value.Key) < 0)
                        {
                            defaulted = false;
                            setValue(new KeyValuePair<TKey, TValue>(e.Key, e.Value));
                        }
                }

                void valueRemoved(object sender, NotifyDictionaryValueEventArgs<TKey, TValue> e)
                {
                    lock (activeValueAccess)
                        if (keyComparer.Compare(e.Key, activeValue.Value.Key) == 0)
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
                }

                void valueReplaced(object sender, NotifyDictionaryValueReplacedEventArgs<TKey, TValue> e)
                {
                    lock (activeValueAccess)
                        if (keyComparer.Compare(e.Key, activeValue.Value.Key) == 0)
                            setValue(new KeyValuePair<TKey, TValue>(e.Key, e.NewValue));
                }

                void valuesAdded(object sender, NotifyDictionaryValuesEventArgs<TKey, TValue> e)
                {
                    lock (activeValueAccess)
                    {
                        var keyValuePairs = e.KeyValuePairs;
                        if (keyValuePairs.Count > 0)
                        {
                            var firstKv = keyValuePairs.OrderBy(kv => kv.Key, keyComparer).First();
                            if (activeValue.OperationFault != null || keyComparer.Compare(firstKv.Key, activeValue.Value.Key) < 0)
                            {
                                defaulted = false;
                                setValue(firstKv);
                            }
                        }
                    }
                }

                void valuesRemoved(object sender, NotifyDictionaryValuesEventArgs<TKey, TValue> e)
                {
                    lock (activeValueAccess)
                        if (e.KeyValuePairs.Any(kv => keyComparer.Compare(kv.Key, activeValue.Value.Key) == 0))
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
                }

                lock (activeValueAccess)
                {
                    changingSource.ValueAdded += valueAdded;
                    changingSource.ValueRemoved += valueRemoved;
                    changingSource.ValueReplaced += valueReplaced;
                    changingSource.ValuesAdded += valuesAdded;
                    changingSource.ValuesRemoved += valuesRemoved;
                    try
                    {
                        return activeValue = new ActiveValue<KeyValuePair<TKey, TValue>>(source.OrderBy(kv => kv.Key, keyComparer).First(), out setValue, elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: dispose);
                    }
                    catch
                    {
                        return new ActiveValue<KeyValuePair<TKey, TValue>>(default, out setValue, elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: dispose);
                    }
                }
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
                var activeValueAccess = new object();
                ActiveValue<KeyValuePair<TKey, TValue>> activeValue = null;
                Action<KeyValuePair<TKey, TValue>> setValue = null;
                Action<Exception> setOperationFault = null;

                void dispose()
                {
                    changingSource.ValueAdded += valueAdded;
                    changingSource.ValueRemoved += valueRemoved;
                    changingSource.ValueReplaced += valueReplaced;
                    changingSource.ValuesAdded += valuesAdded;
                    changingSource.ValuesRemoved += valuesRemoved;
                }

                void valueAdded(object sender, NotifyDictionaryValueEventArgs<TKey, TValue> e)
                {
                    lock (activeValueAccess)
                        if (activeValue.OperationFault != null || keyComparer.Compare(e.Key, activeValue.Value.Key) > 0)
                        {
                            setOperationFault(null);
                            setValue(new KeyValuePair<TKey, TValue>(e.Key, e.Value));
                        }
                }

                void valueRemoved(object sender, NotifyDictionaryValueEventArgs<TKey, TValue> e)
                {
                    lock (activeValueAccess)
                        if (keyComparer.Compare(e.Key, activeValue.Value.Key) == 0)
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
                }

                void valueReplaced(object sender, NotifyDictionaryValueReplacedEventArgs<TKey, TValue> e)
                {
                    lock (activeValueAccess)
                        if (keyComparer.Compare(e.Key, activeValue.Value.Key) == 0)
                            setValue(new KeyValuePair<TKey, TValue>(e.Key, e.NewValue));
                }

                void valuesAdded(object sender, NotifyDictionaryValuesEventArgs<TKey, TValue> e)
                {
                    lock (activeValueAccess)
                    {
                        var keyValuePairs = e.KeyValuePairs;
                        if (keyValuePairs.Count > 0)
                        {
                            var lastKv = keyValuePairs.OrderByDescending(kv => kv.Key, keyComparer).First();
                            if (activeValue.OperationFault != null || keyComparer.Compare(lastKv.Key, activeValue.Value.Key) > 0)
                            {
                                setOperationFault(null);
                                setValue(lastKv);
                            }
                        }
                    }
                }

                void valuesRemoved(object sender, NotifyDictionaryValuesEventArgs<TKey, TValue> e)
                {
                    lock (activeValueAccess)
                        if (e.KeyValuePairs.Any(kv => keyComparer.Compare(kv.Key, activeValue.Value.Key) == 0))
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
                }

                lock (activeValueAccess)
                {
                    changingSource.ValueAdded += valueAdded;
                    changingSource.ValueRemoved += valueRemoved;
                    changingSource.ValueReplaced += valueReplaced;
                    changingSource.ValuesAdded += valuesAdded;
                    changingSource.ValuesRemoved += valuesRemoved;
                    try
                    {
                        return activeValue = new ActiveValue<KeyValuePair<TKey, TValue>>(source.OrderByDescending(kv => kv.Key, keyComparer).First(), out setValue, out setOperationFault, elementFaultChangeNotifier, dispose);
                    }
                    catch (Exception ex)
                    {
                        return new ActiveValue<KeyValuePair<TKey, TValue>>(default, out setValue, ex, out setOperationFault, elementFaultChangeNotifier, dispose);
                    }
                }
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
                var activeValueAccess = new object();
                ActiveValue<KeyValuePair<TKey, TValue>> activeValue = null;
                Action<KeyValuePair<TKey, TValue>> setValue = null;
                var defaulted = false;

                void dispose()
                {
                    changingSource.ValueAdded += valueAdded;
                    changingSource.ValueRemoved += valueRemoved;
                    changingSource.ValueReplaced += valueReplaced;
                    changingSource.ValuesAdded += valuesAdded;
                    changingSource.ValuesRemoved += valuesRemoved;
                }

                void valueAdded(object sender, NotifyDictionaryValueEventArgs<TKey, TValue> e)
                {
                    lock (activeValueAccess)
                        if (defaulted || keyComparer.Compare(e.Key, activeValue.Value.Key) > 0)
                        {
                            defaulted = false;
                            setValue(new KeyValuePair<TKey, TValue>(e.Key, e.Value));
                        }
                }

                void valueRemoved(object sender, NotifyDictionaryValueEventArgs<TKey, TValue> e)
                {
                    lock (activeValueAccess)
                        if (keyComparer.Compare(e.Key, activeValue.Value.Key) == 0)
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
                }

                void valueReplaced(object sender, NotifyDictionaryValueReplacedEventArgs<TKey, TValue> e)
                {
                    lock (activeValueAccess)
                        if (keyComparer.Compare(e.Key, activeValue.Value.Key) == 0)
                            setValue(new KeyValuePair<TKey, TValue>(e.Key, e.NewValue));
                }

                void valuesAdded(object sender, NotifyDictionaryValuesEventArgs<TKey, TValue> e)
                {
                    lock (activeValueAccess)
                    {
                        var keyValuePairs = e.KeyValuePairs;
                        if (keyValuePairs.Count > 0)
                        {
                            var firstKv = keyValuePairs.OrderByDescending(kv => kv.Key, keyComparer).First();
                            if (activeValue.OperationFault != null || keyComparer.Compare(firstKv.Key, activeValue.Value.Key) > 0)
                            {
                                defaulted = false;
                                setValue(firstKv);
                            }
                        }
                    }
                }

                void valuesRemoved(object sender, NotifyDictionaryValuesEventArgs<TKey, TValue> e)
                {
                    lock (activeValueAccess)
                        if (e.KeyValuePairs.Any(kv => keyComparer.Compare(kv.Key, activeValue.Value.Key) == 0))
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
                }

                lock (activeValueAccess)
                {
                    changingSource.ValueAdded += valueAdded;
                    changingSource.ValueRemoved += valueRemoved;
                    changingSource.ValueReplaced += valueReplaced;
                    changingSource.ValuesAdded += valuesAdded;
                    changingSource.ValuesRemoved += valuesRemoved;
                    try
                    {
                        return activeValue = new ActiveValue<KeyValuePair<TKey, TValue>>(source.OrderByDescending(kv => kv.Key, keyComparer).First(), out setValue, elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: dispose);
                    }
                    catch
                    {
                        return new ActiveValue<KeyValuePair<TKey, TValue>>(default, out setValue, elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: dispose);
                    }
                }
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

        #endregion FirstOrDefault

        #region Max

        public static ActiveValue<TValue> ActiveMax<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source) =>
            ActiveMax(source, (key, value) => value);

        public static ActiveValue<TResult> ActiveMax<TKey, TValue, TResult>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, TResult>> selector, ActiveExpressionOptions selectorOptions = null)
        {
            var comparer = Comparer<TResult>.Default;
            var activeValueAccess = new object();
            ReadOnlyDictionaryRangeActiveExpression<TKey, TValue, TResult> rangeActiveExpression;
            ActiveValue<TResult> activeValue = null;
            Action<TResult> setValue = null;
            Action<Exception> setOperationFault = null;

            void dispose()
            {
                rangeActiveExpression.ValueAdded -= valueAdded;
                rangeActiveExpression.ValueRemoved -= valueRemoved;
                rangeActiveExpression.ValueReplaced -= valueReplaced;
                rangeActiveExpression.ValueResultChanged -= valueResultChanged;
                rangeActiveExpression.ValuesAdded -= valuesAdded;
                rangeActiveExpression.ValuesRemoved -= valuesRemoved;
                rangeActiveExpression.Dispose();
            }

            void valueAdded(object sender, NotifyDictionaryValueEventArgs<TKey, TResult> e)
            {
                lock (activeValueAccess)
                {
                    var added = e.Value;
                    if (activeValue.OperationFault != null || comparer.Compare(activeValue.Value, added) < 0)
                    {
                        setOperationFault(null);
                        setValue(added);
                    }
                }
            }

            void valueRemoved(object sender, NotifyDictionaryValueEventArgs<TKey, TResult> e)
            {
                lock (activeValueAccess)
                    if (comparer.Compare(activeValue.Value, e.Value) == 0)
                    {
                        try
                        {
                            var value = rangeActiveExpression.GetResultsUnderLock().Select(kr => kr.result).Max();
                            setOperationFault(null);
                            setValue(value);
                        }
                        catch (Exception ex)
                        {
                            setOperationFault(ex);
                        }
                    }
            }

            void valueReplaced(object sender, NotifyDictionaryValueReplacedEventArgs<TKey, TResult> e)
            {
                lock (activeValueAccess)
                {
                    var added = e.NewValue;
                    if (comparer.Compare(activeValue.Value, added) < 0)
                        setValue(added);
                    else if (comparer.Compare(activeValue.Value, e.OldValue) == 0)
                        setValue(rangeActiveExpression.GetResultsUnderLock().Select(kr => kr.result).Max());
                }
            }

            void valueResultChanged(object sender, RangeActiveExpressionResultChangeEventArgs<TKey, TResult> e)
            {
                lock (activeValueAccess)
                {
                    var comparison = comparer.Compare(activeValue.Value, e.Result);
                    if (comparison < 0)
                        setValue(e.Result);
                    else if (comparison > 0)
                        setValue(rangeActiveExpression.GetResultsUnderLock().Select(er => er.result).Max());
                }
            }

            void valuesAdded(object sender, NotifyDictionaryValuesEventArgs<TKey, TResult> e)
            {
                lock (activeValueAccess)
                    if (e.KeyValuePairs.Count > 0)
                    {
                        var addedMax = e.KeyValuePairs.Select(kv => kv.Value).Max();
                        if (activeValue.OperationFault != null || comparer.Compare(activeValue.Value, addedMax) < 0)
                        {
                            setOperationFault(null);
                            setValue(addedMax);
                        }
                    }
            }

            void valuesRemoved(object sender, NotifyDictionaryValuesEventArgs<TKey, TResult> e)
            {
                lock (activeValueAccess)
                    if (e.KeyValuePairs.Count > 0)
                    {
                        var removedMax = e.KeyValuePairs.Select(kv => kv.Value).Max();
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
            }

            lock (activeValueAccess)
            {
                rangeActiveExpression = RangeActiveExpression.Create(source, selector, selectorOptions);
                rangeActiveExpression.ValueAdded += valueAdded;
                rangeActiveExpression.ValueRemoved += valueRemoved;
                rangeActiveExpression.ValueReplaced += valueReplaced;
                rangeActiveExpression.ValueResultChanged += valueResultChanged;
                rangeActiveExpression.ValuesAdded += valuesAdded;
                rangeActiveExpression.ValuesRemoved += valuesRemoved;

                try
                {
                    return activeValue = new ActiveValue<TResult>(rangeActiveExpression.GetResults().Select(kr => kr.result).Max(), out setValue, null, out setOperationFault, rangeActiveExpression, dispose);
                }
                catch (Exception ex)
                {
                    return activeValue = new ActiveValue<TResult>(default, out setValue, ex, out setOperationFault, rangeActiveExpression, dispose);
                }
            }
        }

        #endregion Max

        #region Min

        public static ActiveValue<TValue> ActiveMin<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source) =>
            ActiveMin(source, (key, value) => value);

        public static ActiveValue<TResult> ActiveMin<TKey, TValue, TResult>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, TResult>> selector, ActiveExpressionOptions selectorOptions = null)
        {
            var comparer = Comparer<TResult>.Default;
            var activeValueAccess = new object();
            ReadOnlyDictionaryRangeActiveExpression<TKey, TValue, TResult> rangeActiveExpression;
            ActiveValue<TResult> activeValue = null;
            Action<TResult> setValue = null;
            Action<Exception> setOperationFault = null;

            void dispose()
            {
                rangeActiveExpression.ValueAdded -= valueAdded;
                rangeActiveExpression.ValueRemoved -= valueRemoved;
                rangeActiveExpression.ValueReplaced -= valueReplaced;
                rangeActiveExpression.ValueResultChanged -= valueResultChanged;
                rangeActiveExpression.ValuesAdded -= valuesAdded;
                rangeActiveExpression.ValuesRemoved -= valuesRemoved;
                rangeActiveExpression.Dispose();
            }

            void valueAdded(object sender, NotifyDictionaryValueEventArgs<TKey, TResult> e)
            {
                lock (activeValueAccess)
                {
                    var added = e.Value;
                    if (activeValue.OperationFault != null || comparer.Compare(activeValue.Value, added) > 0)
                    {
                        setOperationFault(null);
                        setValue(added);
                    }
                }
            }

            void valueRemoved(object sender, NotifyDictionaryValueEventArgs<TKey, TResult> e)
            {
                lock (activeValueAccess)
                    if (comparer.Compare(activeValue.Value, e.Value) == 0)
                    {
                        try
                        {
                            var value = rangeActiveExpression.GetResultsUnderLock().Select(kr => kr.result).Min();
                            setOperationFault(null);
                            setValue(value);
                        }
                        catch (Exception ex)
                        {
                            setOperationFault(ex);
                        }
                    }
            }

            void valueReplaced(object sender, NotifyDictionaryValueReplacedEventArgs<TKey, TResult> e)
            {
                lock (activeValueAccess)
                {
                    var added = e.NewValue;
                    if (comparer.Compare(activeValue.Value, added) > 0)
                        setValue(added);
                    else if (comparer.Compare(activeValue.Value, e.OldValue) == 0)
                        setValue(rangeActiveExpression.GetResultsUnderLock().Select(kr => kr.result).Min());
                }
            }

            void valueResultChanged(object sender, RangeActiveExpressionResultChangeEventArgs<TKey, TResult> e)
            {
                lock (activeValueAccess)
                {
                    var comparison = comparer.Compare(activeValue.Value, e.Result);
                    if (comparison > 0)
                        setValue(e.Result);
                    else if (comparison < 0)
                        setValue(rangeActiveExpression.GetResultsUnderLock().Select(er => er.result).Min());
                }
            }

            void valuesAdded(object sender, NotifyDictionaryValuesEventArgs<TKey, TResult> e)
            {
                lock (activeValueAccess)
                    if (e.KeyValuePairs.Count > 0)
                    {
                        var addedMin = e.KeyValuePairs.Select(kv => kv.Value).Min();
                        if (activeValue.OperationFault != null || comparer.Compare(activeValue.Value, addedMin) > 0)
                        {
                            setOperationFault(null);
                            setValue(addedMin);
                        }
                    }
            }

            void valuesRemoved(object sender, NotifyDictionaryValuesEventArgs<TKey, TResult> e)
            {
                lock (activeValueAccess)
                    if (e.KeyValuePairs.Count > 0)
                    {
                        var removedMin = e.KeyValuePairs.Select(kv => kv.Value).Min();
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
            }

            lock (activeValueAccess)
            {
                rangeActiveExpression = RangeActiveExpression.Create(source, selector, selectorOptions);
                rangeActiveExpression.ValueAdded += valueAdded;
                rangeActiveExpression.ValueRemoved += valueRemoved;
                rangeActiveExpression.ValueReplaced += valueReplaced;
                rangeActiveExpression.ValueResultChanged += valueResultChanged;
                rangeActiveExpression.ValuesAdded += valuesAdded;
                rangeActiveExpression.ValuesRemoved += valuesRemoved;

                try
                {
                    return activeValue = new ActiveValue<TResult>(rangeActiveExpression.GetResults().Select(kr => kr.result).Min(), out setValue, null, out setOperationFault, rangeActiveExpression, dispose);
                }
                catch (Exception ex)
                {
                    return activeValue = new ActiveValue<TResult>(default, out setValue, ex, out setOperationFault, rangeActiveExpression, dispose);
                }
            }
        }

        #endregion Min

        #region Select

        public static ActiveEnumerable<TResult> ActiveSelect<TKey, TValue, TResult>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, TResult>> selector, ActiveExpressionOptions selectorOptions = null)
        {
            var synchronizableSource = source as ISynchronizable;
            var rangeObservableCollectionAccess = new object();
            var keyToIndex = source.CreateSimilarDictionary<TKey, TValue, int>();
            SynchronizedRangeObservableCollection<TResult> rangeObservableCollection = null;

            void propertyChanged(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == nameof(ISynchronizable.IsSynchronized))
                    rangeObservableCollection.IsSynchronized = synchronizableSource.IsSynchronized;
            }

            void valueAdded(object sender, NotifyDictionaryValueEventArgs<TKey, TResult> e)
            {
                lock (rangeObservableCollectionAccess)
                {
                    keyToIndex.Add(e.Key, keyToIndex.Count);
                    rangeObservableCollection.Add(e.Value);
                }
            }

            void valueRemoved(object sender, NotifyDictionaryValueEventArgs<TKey, TResult> e)
            {
                lock (rangeObservableCollectionAccess)
                {
                    var key = e.Key;
                    var removingIndex = keyToIndex[key];
                    keyToIndex.Remove(key);
                    foreach (var indexKey in keyToIndex.Keys.ToList())
                    {
                        var index = keyToIndex[key];
                        if (index > removingIndex)
                            keyToIndex[key] = index - 1;
                    }
                }
            }

            void valueReplaced(object sender, NotifyDictionaryValueReplacedEventArgs<TKey, TResult> e)
            {
                lock (rangeObservableCollectionAccess)
                    rangeObservableCollection.Replace(keyToIndex[e.Key], e.NewValue);
            }

            void valueResultChanged(object sender, RangeActiveExpressionResultChangeEventArgs<TKey, TResult> e)
            {
                lock (rangeObservableCollectionAccess)
                    rangeObservableCollection.Replace(keyToIndex[e.Element], e.Result);
            }

            void valuesAdded(object sender, NotifyDictionaryValuesEventArgs<TKey, TResult> e)
            {
                lock (rangeObservableCollectionAccess)
                {
                    var lastIndex = keyToIndex.Count - 1;
                    rangeObservableCollection.AddRange(e.KeyValuePairs.Select((KeyValuePair<TKey, TResult> kv, int index) =>
                    {
                        keyToIndex.Add(kv.Key, lastIndex + index);
                        return kv.Value;
                    }));
                }
            }

            void valuesRemoved(object sender, NotifyDictionaryValuesEventArgs<TKey, TResult> e)
            {
                lock (rangeObservableCollectionAccess)
                {
                    var removingIndicies = new List<int>();
                    foreach (var kv in e.KeyValuePairs)
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
            }

            lock (rangeObservableCollectionAccess)
            {
                var rangeActiveExpression = RangeActiveExpression.Create(source, selector, selectorOptions);
                rangeActiveExpression.ValueAdded += valueAdded;
                rangeActiveExpression.ValueRemoved += valueRemoved;
                rangeActiveExpression.ValueReplaced += valueReplaced;
                rangeActiveExpression.ValueResultChanged += valueResultChanged;
                rangeActiveExpression.ValuesAdded += valuesAdded;
                rangeActiveExpression.ValuesRemoved += valuesRemoved;
                if (synchronizableSource != null)
                    synchronizableSource.PropertyChanged += propertyChanged;

                rangeObservableCollection = new SynchronizedRangeObservableCollection<TResult>(synchronizableSource?.SynchronizationContext, rangeActiveExpression.GetResults().Select(((TKey key, TResult result) er, int index) =>
                {
                    keyToIndex.Add(er.key, index);
                    return er.result;
                }), synchronizableSource?.IsSynchronized ?? false);
                return new ActiveEnumerable<TResult>(rangeObservableCollection, rangeActiveExpression, () =>
                {
                    rangeActiveExpression.ValueAdded -= valueAdded;
                    rangeActiveExpression.ValueRemoved -= valueRemoved;
                    rangeActiveExpression.ValueReplaced -= valueReplaced;
                    rangeActiveExpression.ValueResultChanged -= valueResultChanged;
                    rangeActiveExpression.ValuesAdded -= valuesAdded;
                    rangeActiveExpression.ValuesRemoved -= valuesRemoved;
                    if (synchronizableSource != null)
                        synchronizableSource.PropertyChanged -= propertyChanged;

                    rangeActiveExpression.Dispose();
                });
            }
        }

        #endregion Select

        #region Single

        public static ActiveValue<KeyValuePair<TKey, TValue>> ActiveSingle<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source)
        {
            var keyEqualityComparer = source.GetKeyEqualityComparer() ?? EqualityComparer<TKey>.Default;
            var elementFaultChangeNotifier = source as INotifyElementFaultChanges;
            if (source is INotifyDictionaryChanged<TKey, TValue> changingSource)
            {
                var activeValueAccess = new object();
                ActiveValue<KeyValuePair<TKey, TValue>> activeValue = null;
                Action<KeyValuePair<TKey, TValue>> setValue = null;
                Action<Exception> setOperationFault = null;

                void dispose()
                {
                    changingSource.ValueAdded += valuesChanged;
                    changingSource.ValueRemoved += valuesChanged;
                    if (activeValue.OperationFault == null)
                        changingSource.ValueReplaced -= valueReplaced;
                    changingSource.ValuesAdded += valuesChanged;
                    changingSource.ValuesRemoved += valuesChanged;
                }

                void valuesChanged(object sender, EventArgs e)
                {
                    lock (activeValueAccess)
                    {
                        if (source.Count == 1)
                        {
                            changingSource.ValueReplaced += valueReplaced;
                            setOperationFault(null);
                            setValue(source.First());
                        }
                        else
                        {
                            if (activeValue.OperationFault == null)
                                changingSource.ValueReplaced -= valueReplaced;
                            if (source.Count == 0)
                                setOperationFault(ExceptionHelper.SequenceContainsNoElements);
                            else
                                setOperationFault(ExceptionHelper.SequenceContainsMoreThanOneElement);
                            setValue(default);
                        }
                    }
                }

                void valueReplaced(object sender, NotifyDictionaryValueReplacedEventArgs<TKey, TValue> e)
                {
                    lock (activeValueAccess)
                        setValue(new KeyValuePair<TKey, TValue>(e.Key, e.NewValue));
                }

                lock (activeValueAccess)
                {
                    changingSource.ValueAdded += valuesChanged;
                    changingSource.ValueRemoved += valuesChanged;
                    changingSource.ValuesAdded += valuesChanged;
                    changingSource.ValuesRemoved += valuesChanged;
                    try
                    {
                        activeValue = new ActiveValue<KeyValuePair<TKey, TValue>>(source.Single(), out setValue, out setOperationFault, elementFaultChangeNotifier, dispose);
                        changingSource.ValueReplaced += valueReplaced;
                        return activeValue;
                    }
                    catch (Exception ex)
                    {
                        return activeValue = new ActiveValue<KeyValuePair<TKey, TValue>>(default, out setValue, ex, out setOperationFault, elementFaultChangeNotifier, dispose);
                    }
                }
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
                var activeValueAccess = new object();
                ActiveValue<KeyValuePair<TKey, TValue>> activeValue = null;
                Action<KeyValuePair<TKey, TValue>> setValue = null;
                Action<Exception> setOperationFault = null;

                void dispose()
                {
                    changingSource.ValueAdded += valuesChanged;
                    changingSource.ValueRemoved += valuesChanged;
                    if (activeValue.OperationFault == null)
                        changingSource.ValueReplaced -= valueReplaced;
                    changingSource.ValuesAdded += valuesChanged;
                    changingSource.ValuesRemoved += valuesChanged;
                }

                void valuesChanged(object sender, EventArgs e)
                {
                    lock (activeValueAccess)
                    {
                        switch (source.Count)
                        {
                            case 0:
                                if (activeValue.OperationFault == null)
                                    changingSource.ValueReplaced -= valueReplaced;
                                setOperationFault(null);
                                setValue(default);
                                break;
                            case 1:
                                changingSource.ValueReplaced += valueReplaced;
                                setOperationFault(null);
                                setValue(source.First());
                                break;
                            default:
                                if (activeValue.OperationFault == null)
                                    changingSource.ValueReplaced -= valueReplaced;
                                setOperationFault(ExceptionHelper.SequenceContainsMoreThanOneElement);
                                setValue(default);
                                break;
                        }
                    }
                }

                void valueReplaced(object sender, NotifyDictionaryValueReplacedEventArgs<TKey, TValue> e)
                {
                    lock (activeValueAccess)
                        setValue(new KeyValuePair<TKey, TValue>(e.Key, e.NewValue));
                }

                lock (activeValueAccess)
                {
                    changingSource.ValueAdded += valuesChanged;
                    changingSource.ValueRemoved += valuesChanged;
                    changingSource.ValuesAdded += valuesChanged;
                    changingSource.ValuesRemoved += valuesChanged;
                    try
                    {
                        activeValue = new ActiveValue<KeyValuePair<TKey, TValue>>(source.SingleOrDefault(), out setValue, out setOperationFault, elementFaultChangeNotifier, dispose);
                        if (source.Count == 1)
                            changingSource.ValueReplaced += valueReplaced;
                        return activeValue;
                    }
                    catch (Exception ex)
                    {
                        return activeValue = new ActiveValue<KeyValuePair<TKey, TValue>>(default, out setValue, ex, out setOperationFault, elementFaultChangeNotifier, dispose);
                    }
                }
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

        #endregion Single

        #region Sum

        public static ActiveValue<TResult> ActiveSum<TKey, TValue, TResult>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, TResult>> selector, ActiveExpressionOptions selectorOptions = null)
        {
            var operations = new GenericOperations<TResult>();
            var activeValueAccess = new object();
            ActiveValue<TResult> activeValue = null;
            Action<TResult> setValue = null;
            var valuesChanging = new Dictionary<TKey, TResult>();

            void valueAdded(object sender, NotifyDictionaryValueEventArgs<TKey, TResult> e)
            {
                lock (activeValueAccess)
                    setValue(operations.Add(activeValue.Value, e.Value));
            }

            void valueRemoved(object sender, NotifyDictionaryValueEventArgs<TKey, TResult> e)
            {
                lock (activeValueAccess)
                    setValue(operations.Subtract(activeValue.Value, e.Value));
            }

            void valueReplaced(object sender, NotifyDictionaryValueReplacedEventArgs<TKey, TResult> e)
            {
                lock (activeValueAccess)
                    setValue(operations.Add(activeValue.Value, operations.Subtract(e.NewValue, e.OldValue)));
            }

            void valueResultChanged(object sender, RangeActiveExpressionResultChangeEventArgs<TKey, TResult> e)
            {
                lock (activeValueAccess)
                {
                    var key = e.Element;
                    setValue(operations.Add(activeValue.Value, operations.Subtract(e.Result, valuesChanging[key])));
                    valuesChanging.Remove(key);
                }
            }

            void valueResultChanging(object sender, RangeActiveExpressionResultChangeEventArgs<TKey, TResult> e)
            {
                lock (activeValueAccess)
                    valuesChanging.Add(e.Element, e.Result);
            }

            void valuesAdded(object sender, NotifyDictionaryValuesEventArgs<TKey, TResult> e)
            {
                lock (activeValueAccess)
                    setValue(new TResult[] { activeValue.Value }.Concat(e.KeyValuePairs.Select(kv => kv.Value)).Aggregate(operations.Add));
            }

            void valuesRemoved(object sender, NotifyDictionaryValuesEventArgs<TKey, TResult> e)
            {
                lock (activeValueAccess)
                    setValue(new TResult[] { activeValue.Value }.Concat(e.KeyValuePairs.Select(kv => kv.Value)).Aggregate(operations.Subtract));
            }

            lock (activeValueAccess)
            {
                var rangeActiveExpression = RangeActiveExpression.Create(source, selector, selectorOptions);
                rangeActiveExpression.ValueAdded += valueAdded;
                rangeActiveExpression.ValueRemoved += valueRemoved;
                rangeActiveExpression.ValueReplaced += valueReplaced;
                rangeActiveExpression.ValueResultChanged += valueResultChanged;
                rangeActiveExpression.ValueResultChanging += valueResultChanging;
                rangeActiveExpression.ValuesAdded += valuesAdded;
                rangeActiveExpression.ValuesRemoved += valuesRemoved;

                return activeValue = new ActiveValue<TResult>(rangeActiveExpression.GetResults().Select(kr => kr.result).Aggregate((a, b) => operations.Add(a, b)), out setValue, null, rangeActiveExpression, () =>
                {
                    rangeActiveExpression.ValueAdded -= valueAdded;
                    rangeActiveExpression.ValueRemoved -= valueRemoved;
                    rangeActiveExpression.ValueReplaced -= valueReplaced;
                    rangeActiveExpression.ValueResultChanged -= valueResultChanged;
                    rangeActiveExpression.ValueResultChanging -= valueResultChanging;
                    rangeActiveExpression.ValuesAdded -= valuesAdded;
                    rangeActiveExpression.ValuesRemoved -= valuesRemoved;

                    rangeActiveExpression.Dispose();
                });
            }
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
                var activeValueAccess = new object();
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

                void dispose()
                {
                    changingSource.ValueAdded -= valueAdded;
                    changingSource.ValueRemoved -= valueRemoved;
                    changingSource.ValueReplaced -= valueReplaced;
                    changingSource.ValuesAdded -= valuesAdded;
                    changingSource.ValuesRemoved -= valuesRemoved;
                }

                void valueAdded(object sender, NotifyDictionaryValueEventArgs<TKey, TValue> e)
                {
                    lock (activeValueAccess)
                        if (equalsKey(e.Key))
                        {
                            setOperationFault(null);
                            setValue(e.Value);
                        }
                }

                void valueRemoved(object sender, NotifyDictionaryValueEventArgs<TKey, TValue> e)
                {
                    lock (activeValueAccess)
                        if (equalsKey(e.Key))
                        {
                            setOperationFault(ExceptionHelper.KeyNotFound);
                            setValue(default);
                        }
                }

                void valueReplaced(object sender, NotifyDictionaryValueReplacedEventArgs<TKey, TValue> e)
                {
                    lock (activeValueAccess)
                        if (equalsKey(e.Key))
                            setValue(e.NewValue);
                }

                void valuesAdded(object sender, NotifyDictionaryValuesEventArgs<TKey, TValue> e)
                {
                    lock (activeValueAccess)
                    {
                        var matchingValues = e.KeyValuePairs.Where(kv => equalsKey(kv.Key)).Select(kv => kv.Value).ToList();
                        if (matchingValues.Count > 0)
                        {
                            setOperationFault(null);
                            setValue(matchingValues[0]);
                        }
                    }
                }

                void valuesRemoved(object sender, NotifyDictionaryValuesEventArgs<TKey, TValue> e)
                {
                    lock (activeValueAccess)
                        if (e.KeyValuePairs.Any(kv => equalsKey(kv.Key)))
                        {
                            setOperationFault(ExceptionHelper.KeyNotFound);
                            setValue(default);
                        }
                }

                lock (activeValueAccess)
                {
                    changingSource.ValueAdded += valueAdded;
                    changingSource.ValueRemoved += valueRemoved;
                    changingSource.ValueReplaced += valueReplaced;
                    changingSource.ValuesAdded += valuesAdded;
                    changingSource.ValuesRemoved += valuesRemoved;
                    try
                    {
                        return new ActiveValue<TValue>(source[key], out setValue, out setOperationFault, elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: dispose);
                    }
                    catch (Exception ex)
                    {
                        return new ActiveValue<TValue>(default, out setValue, ex, out setOperationFault, elementFaultChangeNotifier, dispose);
                    }
                }
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
                var activeValueAccess = new object();
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

                void dispose()
                {
                    changingSource.ValueAdded -= valueAdded;
                    changingSource.ValueRemoved -= valueRemoved;
                    changingSource.ValueReplaced -= valueReplaced;
                    changingSource.ValuesAdded -= valuesAdded;
                    changingSource.ValuesRemoved -= valuesRemoved;
                }

                void valueAdded(object sender, NotifyDictionaryValueEventArgs<TKey, TValue> e)
                {
                    lock (activeValueAccess)
                        if (equalsKey(e.Key))
                            setValue(e.Value);
                }

                void valueRemoved(object sender, NotifyDictionaryValueEventArgs<TKey, TValue> e)
                {
                    lock (activeValueAccess)
                        if (equalsKey(e.Key))
                            setValue(default);
                }

                void valueReplaced(object sender, NotifyDictionaryValueReplacedEventArgs<TKey, TValue> e)
                {
                    lock (activeValueAccess)
                        if (equalsKey(e.Key))
                            setValue(e.NewValue);
                }

                void valuesAdded(object sender, NotifyDictionaryValuesEventArgs<TKey, TValue> e)
                {
                    lock (activeValueAccess)
                    {
                        var matchingValues = e.KeyValuePairs.Where(kv => equalsKey(kv.Key)).Select(kv => kv.Value).ToList();
                        if (matchingValues.Count > 0)
                            setValue(matchingValues[0]);
                    }
                }

                void valuesRemoved(object sender, NotifyDictionaryValuesEventArgs<TKey, TValue> e)
                {
                    lock (activeValueAccess)
                        if (e.KeyValuePairs.Any(kv => equalsKey(kv.Key)))
                            setValue(default);
                }

                lock (activeValueAccess)
                {
                    changingSource.ValueAdded += valueAdded;
                    changingSource.ValueRemoved += valueRemoved;
                    changingSource.ValueReplaced += valueReplaced;
                    changingSource.ValuesAdded += valuesAdded;
                    changingSource.ValuesRemoved += valuesRemoved;
                    return new ActiveValue<TValue>(source.TryGetValue(key, out var value) ? value : default, out setValue, elementFaultChangeNotifier: elementFaultChangeNotifier, onDispose: dispose);
                }
            }
            else
                return new ActiveValue<TValue>(source.TryGetValue(key, out var value) ? value : default, elementFaultChangeNotifier: elementFaultChangeNotifier);
        }

        #endregion ValueFor

        #region Where

        public static ActiveLookup<TKey, TValue> ActiveWhere<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, bool>> predicate, ActiveExpressionOptions predicateOptions = null)
        {
            var synchronizableSource = source as ISynchronizable;
            var rangeObservableDictionaryAccess = new object();
            ISynchronizableObservableRangeDictionary<TKey, TValue> rangeObservableDictionary = null;

            void propertyChanged(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == nameof(ISynchronizable.IsSynchronized))
                {
                    if (rangeObservableDictionary is SynchronizedObservableSortedDictionary<TKey, TValue> sorted)
                        sorted.IsSynchronized = synchronizableSource.IsSynchronized;
                    else
                        ((SynchronizedObservableDictionary<TKey, TValue>)rangeObservableDictionary).IsSynchronized = synchronizableSource.IsSynchronized;
                }
            }

            void valueAdded(object sender, NotifyDictionaryValueEventArgs<TKey, bool> e)
            {
                lock (rangeObservableDictionaryAccess)
                    if (e.Value)
                    {
                        var key = e.Key;
                        rangeObservableDictionary.Add(key, source[key]);
                    }
            }

            void valueRemoved(object sender, NotifyDictionaryValueEventArgs<TKey, bool> e)
            {
                lock (rangeObservableDictionaryAccess)
                    if (e.Value)
                        rangeObservableDictionary.Remove(e.Key);
            }

            void valueReplaced(object sender, NotifyDictionaryValueReplacedEventArgs<TKey, bool> e)
            {
                lock (rangeObservableDictionaryAccess)
                    if (e.OldValue)
                    {
                        var key = e.Key;
                        var value = source[key];
                        if (e.NewValue)
                            rangeObservableDictionary[key] = source[key];
                        else
                            rangeObservableDictionary.Remove(key);
                    }
            }

            void valueResultChanged(object sender, RangeActiveExpressionResultChangeEventArgs<TKey, bool> e)
            {
                lock (rangeObservableDictionaryAccess)
                {
                    var key = e.Element;
                    if (e.Result)
                        rangeObservableDictionary.Add(key, source[key]);
                    else
                        rangeObservableDictionary.Remove(key);
                }
            }

            void valuesAdded(object sender, NotifyDictionaryValuesEventArgs<TKey, bool> e)
            {
                lock (rangeObservableDictionaryAccess)
                    rangeObservableDictionary.AddRange(e.KeyValuePairs.Where(kv => kv.Value).Select(kv =>
                    {
                        var key = kv.Key;
                        return new KeyValuePair<TKey, TValue>(key, source[key]);
                    }));
            }

            void valuesRemoved(object sender, NotifyDictionaryValuesEventArgs<TKey, bool> e)
            {
                lock (rangeObservableDictionaryAccess)
                    rangeObservableDictionary.RemoveRange(e.KeyValuePairs.Where(kv => kv.Value).Select(kv => kv.Key));
            }

            lock (rangeObservableDictionaryAccess)
            {
                var rangeActiveExpression = RangeActiveExpression.Create(source, predicate, predicateOptions);
                rangeActiveExpression.ValueAdded += valueAdded;
                rangeActiveExpression.ValueRemoved += valueRemoved;
                rangeActiveExpression.ValueReplaced += valueReplaced;
                rangeActiveExpression.ValueResultChanged += valueResultChanged;
                rangeActiveExpression.ValuesAdded += valuesAdded;
                rangeActiveExpression.ValuesRemoved += valuesRemoved;
                if (synchronizableSource != null)
                    synchronizableSource.PropertyChanged += propertyChanged;

                rangeObservableDictionary = source.CreateSimilarSynchronizedObservableDictionary(synchronizableSource?.SynchronizationContext, synchronizableSource?.IsSynchronized ?? false);
                return new ActiveLookup<TKey, TValue>(rangeObservableDictionary, rangeActiveExpression, () =>
                {
                    rangeActiveExpression.ValueAdded -= valueAdded;
                    rangeActiveExpression.ValueRemoved -= valueRemoved;
                    rangeActiveExpression.ValueReplaced -= valueReplaced;
                    rangeActiveExpression.ValueResultChanged -= valueResultChanged;
                    rangeActiveExpression.ValuesAdded -= valuesAdded;
                    rangeActiveExpression.ValuesRemoved -= valuesRemoved;
                    if (synchronizableSource != null)
                        synchronizableSource.PropertyChanged -= propertyChanged;

                    rangeActiveExpression.Dispose();
                });
            }
        }

        #endregion Where
    }
}
