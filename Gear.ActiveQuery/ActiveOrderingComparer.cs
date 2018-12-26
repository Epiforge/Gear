using Gear.Components;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Gear.ActiveQuery
{
    class ActiveOrderingComparer<TElement> : SyncDisposable, IComparer<TElement>
    {
        static readonly EqualityComparer<TElement> equalityComparer = EqualityComparer<TElement>.Default;

        public ActiveOrderingComparer(IReadOnlyList<(EnumerableRangeActiveExpression<TElement, IComparable> rangeActiveExpression, bool isDescending)> selectors, IndexingStrategy indexingStrategy)
        {
            this.indexingStrategy = indexingStrategy;
            switch (this.indexingStrategy)
            {
                case IndexingStrategy.HashTable:
                    comparables = new Dictionary<TElement, List<IComparable>>();
                    counts = new Dictionary<TElement, int>();
                    break;
                case IndexingStrategy.SelfBalancingBinarySearchTree:
                    comparables = new SortedDictionary<TElement, List<IComparable>>();
                    counts = new SortedDictionary<TElement, int>();
                    break;
            }

            lock (comparablesAccess)
            {
                this.selectors = selectors.ToImmutableArray();
                if (this.indexingStrategy != IndexingStrategy.NoneOrInherit)
                {
                    foreach (var (rangeActiveExpression, isDescending) in this.selectors)
                    {
                        rangeActiveExpression.ElementResultChanged += RangeActiveExpressionElementResultChanged;
                        rangeActiveExpression.ElementsAdded += RangeActiveExpressionElementsAdded;
                    }
                    this.selectors.Last().rangeActiveExpression.ElementsRemoved += RangeActiveExpressionElementsRemoved;
                    rangeActiveExpressionIndicies = new Dictionary<EnumerableRangeActiveExpression<TElement, IComparable>, int>();
                    var index = -1;
                    foreach (var (rangeActiveExpression, isDescending) in this.selectors.Take(1))
                    {
                        rangeActiveExpressionIndicies.Add(rangeActiveExpression, ++index);
                        foreach (var elementAndResults in rangeActiveExpression.GetResults().GroupBy(er => er.element, er => er.result))
                        {
                            var element = elementAndResults.Key;
                            var elementComparables = new List<IComparable>();
                            comparables.Add(element, elementComparables);
                            elementComparables.Add(elementAndResults.First());
                            counts.Add(element, elementAndResults.Count());
                        }
                    }
                    foreach (var (rangeActiveExpression, isDescending) in this.selectors.Skip(1))
                    {
                        rangeActiveExpressionIndicies.Add(rangeActiveExpression, ++index);
                        foreach (var elementAndResults in rangeActiveExpression.GetResults().GroupBy(er => er.element, er => er.result))
                            comparables[elementAndResults.Key].Add(elementAndResults.First());
                    }
                }
            }
        }

        readonly IDictionary<TElement, List<IComparable>> comparables;
        readonly object comparablesAccess = new object();
        readonly IDictionary<TElement, int> counts;
        readonly IndexingStrategy indexingStrategy;
        readonly Dictionary<EnumerableRangeActiveExpression<TElement, IComparable>, int> rangeActiveExpressionIndicies;
        readonly IReadOnlyList<(EnumerableRangeActiveExpression<TElement, IComparable> rangeActiveExpression, bool isDescending)> selectors;

        public int Compare(TElement x, TElement y)
        {
            IReadOnlyList<IComparable> xList, yList;
            if (indexingStrategy == IndexingStrategy.NoneOrInherit)
            {
                xList = GetComparables(x);
                yList = GetComparables(y);
            }
            else
            {
                xList = comparables[x];
                yList = comparables[y];
            }
            for (var i = 0; i < selectors.Count; ++i)
            {
                var isDescending = selectors[i].isDescending;
                var xComparable = xList[i];
                var yComparable = yList[i];
                if (xComparable == null)
                    return yComparable == null ? 0 : isDescending ? 1 : -1;
                else if (yComparable == null)
                    return isDescending ? -1 : 1;
                var comparison = xComparable.CompareTo(yComparable);
                if (comparison != 0)
                    return comparison * (isDescending ? -1 : 1);
            }
            return 0;
        }

        protected override void Dispose(bool disposing)
        {
            if (indexingStrategy != IndexingStrategy.NoneOrInherit)
            {
                foreach (var (rangeActiveExpression, isDescending) in selectors)
                {
                    rangeActiveExpression.ElementResultChanged -= RangeActiveExpressionElementResultChanged;
                    rangeActiveExpression.ElementsAdded -= RangeActiveExpressionElementsAdded;
                }
                selectors.Last().rangeActiveExpression.ElementsRemoved += RangeActiveExpressionElementsRemoved;
            }
        }

        IReadOnlyList<IComparable> GetComparables(TElement element) =>
            selectors.Select(expressionAndOrder => expressionAndOrder.rangeActiveExpression.GetResultsUnderLock().First(er => equalityComparer.Equals(er.element, element)).result).ToImmutableArray();

        void RangeActiveExpressionElementResultChanged(object sender, RangeActiveExpressionResultChangeEventArgs<TElement, IComparable> e)
        {
            lock (comparablesAccess)
                comparables[e.Element][rangeActiveExpressionIndicies[(EnumerableRangeActiveExpression<TElement, IComparable>)sender]] = e.Result;
        }

        void RangeActiveExpressionElementsAdded(object sender, RangeActiveExpressionMembershipEventArgs<TElement, IComparable> e)
        {
            lock (comparablesAccess)
            {
                var rangeActiveExpressionIndex = rangeActiveExpressionIndicies[(EnumerableRangeActiveExpression<TElement, IComparable>)sender];
                if (rangeActiveExpressionIndex == 0)
                    foreach (var elementAndResults in e.ElementResults.GroupBy(er => er.element, er => er.result))
                    {
                        var element = elementAndResults.Key;
                        var count = elementAndResults.Count();
                        if (!comparables.TryGetValue(element, out var elementComparables))
                        {
                            elementComparables = new List<IComparable>();
                            comparables.Add(elementAndResults.Key, elementComparables);
                            elementComparables.Add(elementAndResults.First());
                            counts.Add(element, count);
                        }
                        else
                            counts[element] += count;
                    }
                else
                    foreach (var elementAndResults in e.ElementResults.GroupBy(er => er.element, er => er.result))
                    {
                        var comparablesList = comparables[elementAndResults.Key];
                        if (comparablesList.Count == rangeActiveExpressionIndex)
                            comparablesList.Add(elementAndResults.First());
                    }
            }
        }

        void RangeActiveExpressionElementsRemoved(object sender, RangeActiveExpressionMembershipEventArgs<TElement, IComparable> e)
        {
            lock (comparablesAccess)
                foreach (var elementAndResults in e.ElementResults.GroupBy(er => er.element, er => er.result))
                {
                    var element = elementAndResults.Key;
                    var currentCount = counts[element];
                    var removedCount = elementAndResults.Count();
                    if (currentCount - removedCount == 0)
                    {
                        counts.Remove(element);
                        comparables.Remove(element);
                    }
                    else
                        counts[element] = currentCount - removedCount;
                }
        }
    }
}
