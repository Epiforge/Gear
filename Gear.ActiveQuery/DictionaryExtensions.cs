using Gear.Components;
using System.Collections.Generic;
using System.Threading;

namespace Gear.ActiveQuery
{
    static class DictionaryExtensions
    {
        static IDictionary<TKey, TResultValue> CreateDictionary<TKey, TSourceValue, TResultValue>(IndexingStrategy? indexingStrategy = null, IEqualityComparer<TKey> keyEqualityComparer = null, IComparer<TKey> keyComparer = null)
        {
            switch (indexingStrategy)
            {
                case IndexingStrategy.SelfBalancingBinarySearchTree:
                    return keyComparer != null ? new SortedDictionary<TKey, TResultValue>(keyComparer) : new SortedDictionary<TKey, TResultValue>();
                default:
                    return keyEqualityComparer != null ? new Dictionary<TKey, TResultValue>(keyEqualityComparer) : new Dictionary<TKey, TResultValue>();
            }
        }

        public static IDictionary<TKey, TValue> CreateSimilarDictionary<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> readOnlyDictionary) => CreateSimilarDictionary<TKey, TValue, TValue>(readOnlyDictionary);

        public static IDictionary<TKey, TResultValue> CreateSimilarDictionary<TKey, TSourceValue, TResultValue>(this IReadOnlyDictionary<TKey, TSourceValue> readOnlyDictionary)
        {
            var indexingStrategy = GetIndexingStrategy(readOnlyDictionary);
            switch (indexingStrategy)
            {
                case IndexingStrategy.SelfBalancingBinarySearchTree:
                    return CreateDictionary<TKey, TSourceValue, TResultValue>(indexingStrategy, keyComparer: GetKeyComparer(readOnlyDictionary));
                default:
                    return CreateDictionary<TKey, TSourceValue, TResultValue>(indexingStrategy, keyEqualityComparer: GetKeyEqualityComparer(readOnlyDictionary));
            }
        }

        public static ISynchronizedObservableRangeDictionary<TKey, TValue> CreateSimilarSynchronizedObservableDictionary<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> readOnlyDictionary) => CreateSimilarSynchronizedObservableDictionary(readOnlyDictionary, SynchronizationContext.Current);

        public static ISynchronizedObservableRangeDictionary<TKey, TValue> CreateSimilarSynchronizedObservableDictionary<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> readOnlyDictionary, SynchronizationContext synchronizationContext) => CreateSimilarSynchronizedObservableDictionary<TKey, TValue, TValue>(readOnlyDictionary, synchronizationContext);

        public static ISynchronizedObservableRangeDictionary<TKey, TResultValue> CreateSimilarSynchronizedObservableDictionary<TKey, TSourceValue, TResultValue>(this IReadOnlyDictionary<TKey, TSourceValue> readOnlyDictionary, SynchronizationContext synchronizationContext)
        {
            var indexingStrategy = GetIndexingStrategy(readOnlyDictionary);
            switch (indexingStrategy)
            {
                case IndexingStrategy.SelfBalancingBinarySearchTree:
                    return CreateSynchronizedObservableDictionary<TKey, TSourceValue, TResultValue>(synchronizationContext, indexingStrategy, keyComparer: GetKeyComparer(readOnlyDictionary));
                default:
                    return CreateSynchronizedObservableDictionary<TKey, TSourceValue, TResultValue>(synchronizationContext, indexingStrategy, keyEqualityComparer: GetKeyEqualityComparer(readOnlyDictionary));
            }
        }

        static ISynchronizedObservableRangeDictionary<TKey, TResultValue> CreateSynchronizedObservableDictionary<TKey, TSourceValue, TResultValue>(SynchronizationContext synchronizationContext, IndexingStrategy? indexingStrategy = null, IEqualityComparer<TKey> keyEqualityComparer = null, IComparer<TKey> keyComparer = null)
        {
            switch (indexingStrategy)
            {
                case IndexingStrategy.SelfBalancingBinarySearchTree when keyComparer != null:
                    return keyComparer != null ? new SynchronizedObservableSortedDictionary<TKey, TResultValue>(synchronizationContext, keyComparer) : new SynchronizedObservableSortedDictionary<TKey, TResultValue>(synchronizationContext);
                default:
                    return keyEqualityComparer != null ? new SynchronizedObservableDictionary<TKey, TResultValue>(synchronizationContext, keyEqualityComparer) : new SynchronizedObservableDictionary<TKey, TResultValue>(synchronizationContext);
            }
        }

        public static IndexingStrategy? GetIndexingStrategy<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> readOnlyDictionary)
        {
            IndexingStrategy? result = null;
            if (readOnlyDictionary is ActiveDictionary<TKey, TValue> activeDictionary)
                result = activeDictionary.IndexingStrategy;
            else if (readOnlyDictionary is Dictionary<TKey, TValue> || readOnlyDictionary is ObservableDictionary<TKey, TValue> || readOnlyDictionary is SynchronizedObservableDictionary<TKey, TValue>)
                result = IndexingStrategy.HashTable;
            else if (readOnlyDictionary is SortedDictionary<TKey, TValue> || readOnlyDictionary is ObservableSortedDictionary<TKey, TValue> || readOnlyDictionary is SynchronizedObservableSortedDictionary<TKey, TValue>)
                result = IndexingStrategy.SelfBalancingBinarySearchTree;
            return result;
        }

        public static IEqualityComparer<TKey> GetKeyEqualityComparer<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> readOnlyDictionary, bool attemptCoercion = true)
        {
            IEqualityComparer<TKey> result = null;
            switch (readOnlyDictionary)
            {
                case ActiveDictionary<TKey, TValue> activeDictionary when activeDictionary.IndexingStrategy == IndexingStrategy.HashTable:
                    result = activeDictionary.EqualityComparer;
                    break;
                case SynchronizedObservableDictionary<TKey, TValue> synchronizedObservable:
                    result = synchronizedObservable.Comparer;
                    break;
                case ObservableDictionary<TKey, TValue> observable:
                    result = observable.Comparer;
                    break;
                case Dictionary<TKey, TValue> standard:
                    result = standard.Comparer;
                    break;
            }
            return result ?? (attemptCoercion && GetKeyComparer(readOnlyDictionary, false) is IEqualityComparer<TKey> coercedEqualityComparer ? coercedEqualityComparer : null);
        }

        public static IComparer<TKey> GetKeyComparer<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> readOnlyDictionary, bool attemptCoercion = true)
        {
            switch (readOnlyDictionary)
            {
                case ActiveDictionary<TKey, TValue> activeDictionary when activeDictionary.IndexingStrategy == IndexingStrategy.SelfBalancingBinarySearchTree:
                    return activeDictionary.Comparer;
                case SynchronizedObservableSortedDictionary<TKey, TValue> synchronizedObservable:
                    return synchronizedObservable.Comparer;
                case ObservableSortedDictionary<TKey, TValue> observable:
                    return observable.Comparer;
                case SortedDictionary<TKey, TValue> standard:
                    return standard.Comparer;
                default:
                    return attemptCoercion && GetKeyEqualityComparer(readOnlyDictionary, false) is IComparer<TKey> coercedComparer ? coercedComparer : null;
            }
        }
    }
}
