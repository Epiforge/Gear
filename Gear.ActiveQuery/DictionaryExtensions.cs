using Gear.Components;
using System.Collections.Generic;
using System.Threading;

namespace Gear.ActiveQuery
{
    static class DictionaryExtensions
    {
        public static IDictionary<TKey, TResultValue> CreateDictionary<TKey, TSourceValue, TResultValue>(IndexingStrategy? indexingStrategy = null, IEqualityComparer<TKey> keyEqualityComparer = null, IComparer<TKey> keyComparer = null)
        {
            switch (indexingStrategy)
            {
                case IndexingStrategy.SelfBalancingBinarySearchTree when (keyComparer != null):
                    return new SortedDictionary<TKey, TResultValue>(keyComparer);
                case IndexingStrategy.SelfBalancingBinarySearchTree:
                    return new SortedDictionary<TKey, TResultValue>();
                case IndexingStrategy.NoneOrInherit when (keyEqualityComparer != null):
                case IndexingStrategy.HashTable when (keyEqualityComparer != null):
                    return new Dictionary<TKey, TResultValue>(keyEqualityComparer);
                default:
                    return new Dictionary<TKey, TResultValue>();
            }
        }

        public static IObservableRangeDictionary<TKey, TResultValue> CreateObservableDictionary<TKey, TSourceValue, TResultValue>(IndexingStrategy? indexingStrategy = null, IEqualityComparer<TKey> keyEqualityComparer = null, IComparer<TKey> keyComparer = null)
        {
            switch (indexingStrategy)
            {
                case IndexingStrategy.SelfBalancingBinarySearchTree when (keyComparer != null):
                    return new ObservableSortedDictionary<TKey, TResultValue>(keyComparer);
                case IndexingStrategy.SelfBalancingBinarySearchTree:
                    return new ObservableSortedDictionary<TKey, TResultValue>();
                case IndexingStrategy.NoneOrInherit when (keyEqualityComparer != null):
                case IndexingStrategy.HashTable when (keyEqualityComparer != null):
                    return new ObservableDictionary<TKey, TResultValue>(keyEqualityComparer);
                default:
                    return new ObservableDictionary<TKey, TResultValue>();
            }
        }

        public static IDictionary<TKey, TValue> CreateSimilarDictionary<TKey, TValue>(this IDictionary<TKey, TValue> dictionary) => CreateSimilarDictionary<TKey, TValue, TValue>(dictionary);

        public static IDictionary<TKey, TResultValue> CreateSimilarDictionary<TKey, TSourceValue, TResultValue>(this IDictionary<TKey, TSourceValue> dictionary)
        {
            var indexingStrategy = GetIndexingStrategy(dictionary);
            switch (indexingStrategy)
            {
                case IndexingStrategy.SelfBalancingBinarySearchTree:
                    return CreateDictionary<TKey, TSourceValue, TResultValue>(indexingStrategy, keyComparer: GetKeyComparer(dictionary));
                default:
                    return CreateDictionary<TKey, TSourceValue, TResultValue>(indexingStrategy, keyEqualityComparer: GetKeyEqualityComparer(dictionary));
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

        public static IObservableRangeDictionary<TKey, TValue> CreateSimilarObservableDictionary<TKey, TValue>(this IDictionary<TKey, TValue> dictionary) => CreateSimilarObservableDictionary<TKey, TValue, TValue>(dictionary);

        public static IObservableRangeDictionary<TKey, TResultValue> CreateSimilarObservableDictionary<TKey, TSourceValue, TResultValue>(this IDictionary<TKey, TSourceValue> dictionary)
        {
            var indexingStrategy = GetIndexingStrategy(dictionary);
            switch (indexingStrategy)
            {
                case IndexingStrategy.SelfBalancingBinarySearchTree:
                    return CreateObservableDictionary<TKey, TSourceValue, TResultValue>(indexingStrategy, keyComparer: GetKeyComparer(dictionary));
                default:
                    return CreateObservableDictionary<TKey, TSourceValue, TResultValue>(indexingStrategy, keyEqualityComparer: GetKeyEqualityComparer(dictionary));
            }
        }

        public static IObservableRangeDictionary<TKey, TValue> CreateSimilarObservableDictionary<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> readOnlyDictionary) => CreateSimilarObservableDictionary<TKey, TValue, TValue>(readOnlyDictionary);

        public static IObservableRangeDictionary<TKey, TResultValue> CreateSimilarObservableDictionary<TKey, TSourceValue, TResultValue>(this IReadOnlyDictionary<TKey, TSourceValue> readOnlyDictionary)
        {
            var indexingStrategy = GetIndexingStrategy(readOnlyDictionary);
            switch (indexingStrategy)
            {
                case IndexingStrategy.SelfBalancingBinarySearchTree:
                    return CreateObservableDictionary<TKey, TSourceValue, TResultValue>(indexingStrategy, keyComparer: GetKeyComparer(readOnlyDictionary));
                default:
                    return CreateObservableDictionary<TKey, TSourceValue, TResultValue>(indexingStrategy, keyEqualityComparer: GetKeyEqualityComparer(readOnlyDictionary));
            }
        }

        public static ISynchronizableObservableRangeDictionary<TKey, TValue> CreateSimilarSynchronizedObservableDictionary<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, SynchronizationContext synchronizationContext, bool isSynchronized = true) => CreateSimilarSynchronizedObservableDictionary<TKey, TValue, TValue>(dictionary, synchronizationContext, isSynchronized);

        public static ISynchronizableObservableRangeDictionary<TKey, TResultValue> CreateSimilarSynchronizedObservableDictionary<TKey, TSourceValue, TResultValue>(this IDictionary<TKey, TSourceValue> dictionary, SynchronizationContext synchronizationContext, bool isSynchronized = true)
        {
            var indexingStrategy = GetIndexingStrategy(dictionary);
            switch (indexingStrategy)
            {
                case IndexingStrategy.SelfBalancingBinarySearchTree:
                    return CreateSynchronizedObservableDictionary<TKey, TSourceValue, TResultValue>(synchronizationContext, isSynchronized, indexingStrategy, keyComparer: GetKeyComparer(dictionary));
                default:
                    return CreateSynchronizedObservableDictionary<TKey, TSourceValue, TResultValue>(synchronizationContext, isSynchronized, indexingStrategy, keyEqualityComparer: GetKeyEqualityComparer(dictionary));
            }
        }

        public static ISynchronizableObservableRangeDictionary<TKey, TValue> CreateSimilarSynchronizedObservableDictionary<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> readOnlyDictionary, SynchronizationContext synchronizationContext, bool isSynchronized = true) => CreateSimilarSynchronizedObservableDictionary<TKey, TValue, TValue>(readOnlyDictionary, synchronizationContext, isSynchronized);

        public static ISynchronizableObservableRangeDictionary<TKey, TResultValue> CreateSimilarSynchronizedObservableDictionary<TKey, TSourceValue, TResultValue>(this IReadOnlyDictionary<TKey, TSourceValue> readOnlyDictionary, SynchronizationContext synchronizationContext, bool isSynchronized = true)
        {
            var indexingStrategy = GetIndexingStrategy(readOnlyDictionary);
            switch (indexingStrategy)
            {
                case IndexingStrategy.SelfBalancingBinarySearchTree:
                    return CreateSynchronizedObservableDictionary<TKey, TSourceValue, TResultValue>(synchronizationContext, isSynchronized, indexingStrategy, keyComparer: GetKeyComparer(readOnlyDictionary));
                default:
                    return CreateSynchronizedObservableDictionary<TKey, TSourceValue, TResultValue>(synchronizationContext, isSynchronized, indexingStrategy, keyEqualityComparer: GetKeyEqualityComparer(readOnlyDictionary));
            }
        }

        public static ISynchronizableObservableRangeDictionary<TKey, TResultValue> CreateSynchronizedObservableDictionary<TKey, TSourceValue, TResultValue>(SynchronizationContext synchronizationContext, bool isSynchronized = true, IndexingStrategy? indexingStrategy = null, IEqualityComparer<TKey> keyEqualityComparer = null, IComparer<TKey> keyComparer = null)
        {
            switch (indexingStrategy)
            {
                case IndexingStrategy.SelfBalancingBinarySearchTree when (keyComparer != null):
                    return new SynchronizedObservableSortedDictionary<TKey, TResultValue>(synchronizationContext, keyComparer, isSynchronized);
                case IndexingStrategy.SelfBalancingBinarySearchTree:
                    return new SynchronizedObservableSortedDictionary<TKey, TResultValue>(synchronizationContext, isSynchronized);
                case IndexingStrategy.NoneOrInherit when (keyEqualityComparer != null):
                case IndexingStrategy.HashTable when (keyEqualityComparer != null):
                    return new SynchronizedObservableDictionary<TKey, TResultValue>(synchronizationContext, keyEqualityComparer, isSynchronized);
                default:
                    return new SynchronizedObservableDictionary<TKey, TResultValue>(synchronizationContext, isSynchronized);
            }
        }

        public static IndexingStrategy? GetIndexingStrategy<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
        {
            if (dictionary is Dictionary<TKey, TValue> || dictionary is ObservableDictionary<TKey, TValue> || dictionary is SynchronizedObservableDictionary<TKey, TValue>)
                return IndexingStrategy.HashTable;
            if (dictionary is SortedDictionary<TKey, TValue> || dictionary is ObservableSortedDictionary<TKey, TValue> || dictionary is SynchronizedObservableSortedDictionary<TKey, TValue>)
                return IndexingStrategy.SelfBalancingBinarySearchTree;
            return null;
        }

        public static IndexingStrategy? GetIndexingStrategy<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> readOnlyDictionary)
        {
            if (readOnlyDictionary is Dictionary<TKey, TValue> || readOnlyDictionary is ObservableDictionary<TKey, TValue> || readOnlyDictionary is SynchronizedObservableDictionary<TKey, TValue>)
                return IndexingStrategy.HashTable;
            if (readOnlyDictionary is SortedDictionary<TKey, TValue> || readOnlyDictionary is ObservableSortedDictionary<TKey, TValue> || readOnlyDictionary is SynchronizedObservableSortedDictionary<TKey, TValue>)
                return IndexingStrategy.SelfBalancingBinarySearchTree;
            return null;
        }

        public static IEqualityComparer<TKey> GetKeyEqualityComparer<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, bool attemptCoercion = true)
        {
            switch (dictionary)
            {
                case SynchronizedObservableDictionary<TKey, TValue> synchronizedObservable:
                    return synchronizedObservable.Comparer;
                case ObservableDictionary<TKey, TValue> observable:
                    return observable.Comparer;
                case Dictionary<TKey, TValue> standard:
                    return standard.Comparer;
                default:
                    return attemptCoercion && GetKeyComparer(dictionary, false) is IEqualityComparer<TKey> coercedEqualityComparer ? coercedEqualityComparer : null;
            }
        }

        public static IEqualityComparer<TKey> GetKeyEqualityComparer<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> readOnlyDictionary, bool attemptCoercion = true)
        {
            switch (readOnlyDictionary)
            {
                case SynchronizedObservableDictionary<TKey, TValue> synchronizedObservable:
                    return synchronizedObservable.Comparer;
                case ObservableDictionary<TKey, TValue> observable:
                    return observable.Comparer;
                case Dictionary<TKey, TValue> standard:
                    return standard.Comparer;
                default:
                    return attemptCoercion && GetKeyComparer(readOnlyDictionary, false) is IEqualityComparer<TKey> coercedEqualityComparer ? coercedEqualityComparer : null;
            }
        }

        public static IComparer<TKey> GetKeyComparer<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, bool attemptCoercion = true)
        {
            switch (dictionary)
            {
                case SynchronizedObservableSortedDictionary<TKey, TValue> synchronizedObservable:
                    return synchronizedObservable.Comparer;
                case ObservableSortedDictionary<TKey, TValue> observable:
                    return observable.Comparer;
                case SortedDictionary<TKey, TValue> standard:
                    return standard.Comparer;
                default:
                    return attemptCoercion && GetKeyEqualityComparer(dictionary, false) is IComparer<TKey> coercedComparer ? coercedComparer : null;
            }
        }

        public static IComparer<TKey> GetKeyComparer<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> readOnlyDictionary, bool attemptCoercion = true)
        {
            switch (readOnlyDictionary)
            {
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
