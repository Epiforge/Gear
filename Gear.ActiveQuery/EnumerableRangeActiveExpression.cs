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
    /// <summary>
    /// Represents the sequence of results derived from creating an active expression for each element in a sequence
    /// </summary>
    /// <typeparam name="TElement">The type of the elements in the sequence</typeparam>
    /// <typeparam name="TResult">The type of the result of the active expression</typeparam>
    class EnumerableRangeActiveExpression<TResult> : OverridableSyncDisposablePropertyChangeNotifier, INotifyElementFaultChanges
    {
        static readonly object rangeActiveExpressionsAccess = new object();
        static readonly Dictionary<(IEnumerable source, string expressionString), EnumerableRangeActiveExpression<TResult>> rangeActiveExpressions = new Dictionary<(IEnumerable source, string expressionString), EnumerableRangeActiveExpression<TResult>>();

        public static EnumerableRangeActiveExpression<TResult> Create(IEnumerable source, Expression<Func<object, TResult>> expression, ActiveExpressionOptions options = null)
        {
            EnumerableRangeActiveExpression<TResult> rangeActiveExpression;
            bool monitorCreated;
            var expressionString = expression.ToString();
            var key = (source, expressionString);
            lock (rangeActiveExpressionsAccess)
            {
                if (monitorCreated = !rangeActiveExpressions.TryGetValue(key, out rangeActiveExpression))
                {
                    rangeActiveExpression = new EnumerableRangeActiveExpression<TResult>(source, expression, options);
                    rangeActiveExpressions.Add(key, rangeActiveExpression);
                }
                ++rangeActiveExpression.disposalCount;
            }
            if (monitorCreated)
            {
                var initialized = false;
                try
                {
                    rangeActiveExpression.Initialize();
                    initialized = true;
                }
                finally
                {
                    if (!initialized)
                        rangeActiveExpression.Dispose();
                }
            }
            return rangeActiveExpression;
        }

        EnumerableRangeActiveExpression(IEnumerable source, Expression<Func<object, TResult>> expression, ActiveExpressionOptions options)
        {
            this.source = source;
            this.expression = expression;
            Options = options;
        }

        readonly Dictionary<ActiveExpression<object, TResult>, int> activeExpressionCounts = new Dictionary<ActiveExpression<object, TResult>, int>();
        readonly List<(object element, ActiveExpression<object, TResult> activeExpression)> activeExpressions = new List<(object element, ActiveExpression<object, TResult> activeExpression)>();
        readonly ReaderWriterLockSlim activeExpressionsAccess = new ReaderWriterLockSlim();
        int disposalCount;
        readonly Expression<Func<object, TResult>> expression;
        readonly IEnumerable source;

        public event EventHandler<ElementFaultChangeEventArgs> ElementFaultChanged;
        public event EventHandler<ElementFaultChangeEventArgs> ElementFaultChanging;
        public event EventHandler<RangeActiveExpressionResultChangeEventArgs<object, TResult>> ElementResultChanged;
        public event EventHandler<RangeActiveExpressionResultChangeEventArgs<object, TResult>> ElementResultChanging;
        public event EventHandler<RangeActiveExpressionMembershipEventArgs<object, TResult>> ElementsAdded;
        public event EventHandler<RangeActiveExpressionMovedEventArgs<object, TResult>> ElementsMoved;
        public event EventHandler<RangeActiveExpressionMembershipEventArgs<object, TResult>> ElementsRemoved;

        void ActiveExpressionPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var activeExpression = (ActiveExpression<object, TResult>)sender;
            if (e.PropertyName == nameof(ActiveExpression<object, TResult>.Fault))
            {
                activeExpressionsAccess.EnterReadLock();
                try
                {
                    OnElementFaultChanged(activeExpression.Arg, activeExpression.Fault, activeExpressionCounts[activeExpression]);
                }
                finally
                {
                    activeExpressionsAccess.ExitReadLock();
                }
            }
            else if (e.PropertyName == nameof(ActiveExpression<object, TResult>.Value))
            {
                activeExpressionsAccess.EnterReadLock();
                try
                {
                    OnElementResultChanged(activeExpression.Arg, activeExpression.Value, activeExpressionCounts[activeExpression]);
                }
                finally
                {
                    activeExpressionsAccess.ExitReadLock();
                }
            }
        }

        void ActiveExpressionPropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            var activeExpression = (ActiveExpression<object, TResult>)sender;
            if (e.PropertyName == nameof(ActiveExpression<object, TResult>.Fault))
            {
                activeExpressionsAccess.EnterReadLock();
                try
                {
                    OnElementFaultChanging(activeExpression.Arg, activeExpression.Fault, activeExpressionCounts[activeExpression]);
                }
                finally
                {
                    activeExpressionsAccess.ExitReadLock();
                }
            }
            else if (e.PropertyName == nameof(ActiveExpression<object, TResult>.Value))
            {
                activeExpressionsAccess.EnterReadLock();
                try
                {
                    OnElementResultChanging(activeExpression.Arg, activeExpression.Value, activeExpressionCounts[activeExpression]);
                }
                finally
                {
                    activeExpressionsAccess.ExitReadLock();
                }
            }
        }

        IReadOnlyList<(object element, TResult result)> AddActiveExpressions(int index, IEnumerable<object> elements)
        {
            if (elements.Any())
            {
                var addedActiveExpressions = new List<ActiveExpression<object, TResult>>();
                activeExpressionsAccess.EnterWriteLock();
                OnPropertyChanging(nameof(Count));
                try
                {
                    activeExpressions.InsertRange(index, elements.Select(element =>
                    {
                        var activeExpression = ActiveExpression.Create(expression, element, Options);
                        if (activeExpressionCounts.TryGetValue(activeExpression, out var activeExpressionCount))
                            activeExpressionCounts[activeExpression] = activeExpressionCount + 1;
                        else
                        {
                            activeExpressionCounts.Add(activeExpression, 1);
                            activeExpression.PropertyChanging += ActiveExpressionPropertyChanging;
                            activeExpression.PropertyChanged += ActiveExpressionPropertyChanged;
                        }
                        addedActiveExpressions.Add(activeExpression);
                        return (element, activeExpression);
                    }));
                }
                finally
                {
                    OnPropertyChanged(nameof(Count));
                    activeExpressionsAccess.ExitWriteLock();
                }
                return addedActiveExpressions.Select(ae => (ae.Arg, ae.Value)).ToImmutableArray();
            }
            return null;
        }

        void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var oldItems = e.OldItems != null ? e.OldItems.Cast<object>() : Enumerable.Empty<object>();
            var oldItemsCount = e.OldItems != null ? e.OldItems.Count : 0;
            var newItems = e.NewItems != null ? e.NewItems.Cast<object>() : Enumerable.Empty<object>();
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    if (activeExpressions.Count > 0)
                        OnElementsRemoved(RemoveActiveExpressions(0, activeExpressions.Count), 0);
                    var addedActiveExpressions = AddActiveExpressions(0, source.Cast<object>());
                    if (addedActiveExpressions != null)
                        OnElementsAdded(addedActiveExpressions, 0);
                    break;
                case NotifyCollectionChangedAction.Move when oldItems.SequenceEqual(newItems):
                    List<(object element, ActiveExpression<object, TResult> activeExpression)> moving;
                    activeExpressionsAccess.EnterWriteLock();
                    try
                    {
                        moving = activeExpressions.GetRange(e.OldStartingIndex, oldItemsCount);
                        activeExpressions.RemoveRange(e.OldStartingIndex, oldItemsCount);
                        activeExpressions.InsertRange(e.NewStartingIndex, moving);
                    }
                    finally
                    {
                        activeExpressionsAccess.ExitWriteLock();
                    }
                    OnElementsMoved(moving.Select(eae => (eae.element, eae.activeExpression.Value)).ToList(), e.OldStartingIndex, e.NewStartingIndex);
                    break;
                default:
                    if (e.OldItems != null && e.OldStartingIndex >= 0)
                        OnElementsRemoved(RemoveActiveExpressions(e.OldStartingIndex, oldItemsCount), e.OldStartingIndex);
                    if (e.NewItems != null && e.NewStartingIndex >= 0)
                        OnElementsAdded(AddActiveExpressions(e.NewStartingIndex, newItems), e.NewStartingIndex);
                    break;
            }
        }

        protected override bool Dispose(bool disposing)
        {
            lock (rangeActiveExpressionsAccess)
            {
                if (--disposalCount > 0)
                    return false;
                rangeActiveExpressions.Remove((source, expression.ToString()));
            }
            RemoveActiveExpressions(0, activeExpressions.Count);
            if (source is INotifyCollectionChanged collectionChangedNotifier)
                collectionChangedNotifier.CollectionChanged -= CollectionChanged;
            if (source is INotifyElementFaultChanges faultNotifier)
            {
                faultNotifier.ElementFaultChanged -= SourceElementFaultChanged;
                faultNotifier.ElementFaultChanging -= SourceElementFaultChanging;
            }
            return true;
        }

        public IReadOnlyList<(object element, Exception fault)> GetElementFaults()
        {
            activeExpressionsAccess.EnterReadLock();
            try
            {
                return GetElementFaultsUnderLock();
            }
            finally
            {
                activeExpressionsAccess.ExitReadLock();
            }
        }

        internal IReadOnlyList<(object element, Exception fault)> GetElementFaultsUnderLock() => activeExpressions.Select(eae => (eae.element, fault: eae.activeExpression.Fault)).Where(ef => ef.fault != null).ToImmutableArray();

        public IReadOnlyList<(object element, TResult result)> GetResults()
        {
            activeExpressionsAccess.EnterReadLock();
            try
            {
                return GetResultsUnderLock();
            }
            finally
            {
                activeExpressionsAccess.ExitReadLock();
            }
        }

        internal IReadOnlyList<(object element, TResult result)> GetResultsUnderLock() => activeExpressions.Select(eae => (eae.element, eae.activeExpression.Value)).ToImmutableArray();

        public IReadOnlyList<(object element, TResult result, Exception fault)> GetResultsAndFaults()
        {
            activeExpressionsAccess.EnterReadLock();
            try
            {
                return GetResultsAndFaultsUnderLock();
            }
            finally
            {
                activeExpressionsAccess.ExitReadLock();
            }
        }

        internal IReadOnlyList<(object element, TResult result, Exception fault)> GetResultsAndFaultsUnderLock() => activeExpressions.Select(eae => (eae.element, eae.activeExpression.Value, eae.activeExpression.Fault)).ToImmutableArray();

        public IReadOnlyList<(object element, TResult result, Exception fault, int count)> GetResultsFaultsAndCounts()
        {
            activeExpressionsAccess.EnterReadLock();
            try
            {
                return GetResultsFaultsAndCountsUnderLock();
            }
            finally
            {
                activeExpressionsAccess.ExitReadLock();
            }
        }

        internal IReadOnlyList<(object element, TResult result, Exception fault, int count)> GetResultsFaultsAndCountsUnderLock() => activeExpressions.Select(eae => (eae.element, eae.activeExpression.Value, eae.activeExpression.Fault, activeExpressionCounts[eae.activeExpression])).ToImmutableArray();

        void Initialize()
        {
            AddActiveExpressions(0, source.Cast<object>());
            if (source is INotifyCollectionChanged collectionChangedNotifier)
                collectionChangedNotifier.CollectionChanged += CollectionChanged;
            if (source is INotifyElementFaultChanges faultNotifier)
            {
                faultNotifier.ElementFaultChanged += SourceElementFaultChanged;
                faultNotifier.ElementFaultChanging += SourceElementFaultChanging;
            }
        }

        protected virtual void OnElementFaultChanged(ElementFaultChangeEventArgs e) =>
            ElementFaultChanged?.Invoke(this, e);

        protected void OnElementFaultChanged(object element, Exception fault, int count) =>
            OnElementFaultChanged(new ElementFaultChangeEventArgs(element, fault, count));

        protected virtual void OnElementFaultChanging(ElementFaultChangeEventArgs e) =>
            ElementFaultChanging?.Invoke(this, e);

        protected void OnElementFaultChanging(object element, Exception fault, int count) =>
            OnElementFaultChanging(new ElementFaultChangeEventArgs(element, fault, count));

        protected virtual void OnElementResultChanged(RangeActiveExpressionResultChangeEventArgs<object, TResult> e) =>
            ElementResultChanged?.Invoke(this, e);

        protected void OnElementResultChanged(object element, TResult result, int count) =>
            OnElementResultChanged(new RangeActiveExpressionResultChangeEventArgs<object, TResult>(element, result, count));

        protected virtual void OnElementResultChanging(RangeActiveExpressionResultChangeEventArgs<object, TResult> e) =>
            ElementResultChanging?.Invoke(this, e);

        protected void OnElementResultChanging(object element, TResult result, int count) =>
            OnElementResultChanging(new RangeActiveExpressionResultChangeEventArgs<object, TResult>(element, result, count));

        protected virtual void OnElementsAdded(RangeActiveExpressionMembershipEventArgs<object, TResult> e) =>
            ElementsAdded?.Invoke(this, e);

        void OnElementsAdded(IReadOnlyList<(object element, TResult result)> elementResults, int index) =>
            OnElementsAdded(new RangeActiveExpressionMembershipEventArgs<object, TResult>(elementResults, index));

        protected virtual void OnElementsMoved(RangeActiveExpressionMovedEventArgs<object, TResult> e) =>
            ElementsMoved?.Invoke(this, e);

        void OnElementsMoved(IReadOnlyList<(object element, TResult result)> elementResults, int fromIndex, int toIndex) =>
            OnElementsMoved(new RangeActiveExpressionMovedEventArgs<object, TResult>(elementResults, fromIndex, toIndex));

        protected virtual void OnElementsRemoved(RangeActiveExpressionMembershipEventArgs<object, TResult> e) =>
            ElementsRemoved?.Invoke(this, e);

        void OnElementsRemoved(IReadOnlyList<(object element, TResult result)> elementResults, int index) =>
            OnElementsRemoved(new RangeActiveExpressionMembershipEventArgs<object, TResult>(elementResults, index));

        IReadOnlyList<(object element, TResult result)> RemoveActiveExpressions(int index, int count)
        {
            List<(object element, TResult result)> result = null;
            activeExpressionsAccess.EnterWriteLock();
            try
            {
                if (count > 0)
                {
                    result = new List<(object element, TResult result)>();
                    OnPropertyChanging(nameof(Count));
                    foreach (var (element, activeExpression) in activeExpressions.GetRange(index, count))
                    {
                        result.Add((element, activeExpression.Value));
                        var activeExpressionCount = activeExpressionCounts[activeExpression];
                        if (activeExpressionCount == 0)
                        {
                            activeExpressionCounts.Remove(activeExpression);
                            activeExpression.PropertyChanging -= ActiveExpressionPropertyChanging;
                            activeExpression.PropertyChanged -= ActiveExpressionPropertyChanged;
                        }
                        else
                            activeExpressionCounts[activeExpression] = activeExpressionCount - 1;
                        activeExpression.Dispose();
                    }
                    activeExpressions.RemoveRange(index, count);
                    OnPropertyChanged(nameof(Count));
                }
            }
            finally
            {
                if (result != null)
                    OnPropertyChanged(nameof(Count));
                activeExpressionsAccess.ExitWriteLock();
            }
            return (result ?? Enumerable.Empty<(object element, TResult result)>()).ToImmutableArray();
        }

        void SourceElementFaultChanged(object sender, ElementFaultChangeEventArgs e) => ElementFaultChanged?.Invoke(sender, e);

        void SourceElementFaultChanging(object sender, ElementFaultChangeEventArgs e) => ElementFaultChanging?.Invoke(sender, e);

        public int Count
        {
            get
            {
                activeExpressionsAccess.EnterReadLock();
                try
                {
                    return CountUnderLock;
                }
                finally
                {
                    activeExpressionsAccess.ExitReadLock();
                }
            }
        }

        internal int CountUnderLock => activeExpressions.Count;

        public ActiveExpressionOptions Options { get; }
    }

    /// <summary>
    /// Represents the sequence of results derived from creating an active expression for each element in a sequence
    /// </summary>
    /// <typeparam name="TElement">The type of the elements in the sequence</typeparam>
    /// <typeparam name="TResult">The type of the result of the active expression</typeparam>
    class EnumerableRangeActiveExpression<TElement, TResult> : OverridableSyncDisposablePropertyChangeNotifier, INotifyElementFaultChanges
    {
        static readonly object rangeActiveExpressionsAccess = new object();
        static readonly Dictionary<(IEnumerable<TElement> source, string expressionString), EnumerableRangeActiveExpression<TElement, TResult>> rangeActiveExpressions = new Dictionary<(IEnumerable<TElement> source, string expressionString), EnumerableRangeActiveExpression<TElement, TResult>>();

        public static EnumerableRangeActiveExpression<TElement, TResult> Create(IEnumerable<TElement> source, Expression<Func<TElement, TResult>> expression, ActiveExpressionOptions options = null)
        {
            EnumerableRangeActiveExpression<TElement, TResult> rangeActiveExpression;
            bool monitorCreated;
            var expressionString = expression.ToString();
            var key = (source, expressionString);
            lock (rangeActiveExpressionsAccess)
            {
                if (monitorCreated = !rangeActiveExpressions.TryGetValue(key, out rangeActiveExpression))
                {
                    rangeActiveExpression = new EnumerableRangeActiveExpression<TElement, TResult>(source, expression, options);
                    rangeActiveExpressions.Add(key, rangeActiveExpression);
                }
                ++rangeActiveExpression.disposalCount;
            }
            if (monitorCreated)
            {
                var initialized = false;
                try
                {
                    rangeActiveExpression.Initialize();
                    initialized = true;
                }
                finally
                {
                    if (!initialized)
                        rangeActiveExpression.Dispose();
                }
            }
            return rangeActiveExpression;
        }

        EnumerableRangeActiveExpression(IEnumerable<TElement> source, Expression<Func<TElement, TResult>> expression, ActiveExpressionOptions options)
        {
            this.source = source;
            this.expression = expression;
            Options = options;
        }

        readonly Dictionary<ActiveExpression<TElement, TResult>, int> activeExpressionCounts = new Dictionary<ActiveExpression<TElement, TResult>, int>();
        readonly List<(TElement element, ActiveExpression<TElement, TResult> activeExpression)> activeExpressions = new List<(TElement element, ActiveExpression<TElement, TResult> activeExpression)>();
        readonly ReaderWriterLockSlim activeExpressionsAccess = new ReaderWriterLockSlim();
        int disposalCount;
        readonly Expression<Func<TElement, TResult>> expression;
        readonly IEnumerable<TElement> source;

        public event EventHandler<ElementFaultChangeEventArgs> ElementFaultChanged;
        public event EventHandler<ElementFaultChangeEventArgs> ElementFaultChanging;
        public event EventHandler<RangeActiveExpressionResultChangeEventArgs<TElement, TResult>> ElementResultChanged;
        public event EventHandler<RangeActiveExpressionResultChangeEventArgs<TElement, TResult>> ElementResultChanging;
        public event EventHandler<RangeActiveExpressionMembershipEventArgs<TElement, TResult>> ElementsAdded;
        public event EventHandler<RangeActiveExpressionMovedEventArgs<TElement, TResult>> ElementsMoved;
        public event EventHandler<RangeActiveExpressionMembershipEventArgs<TElement, TResult>> ElementsRemoved;

        void ActiveExpressionPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var activeExpression = (ActiveExpression<TElement, TResult>)sender;
            if (e.PropertyName == nameof(ActiveExpression<TElement, TResult>.Fault))
            {
                activeExpressionsAccess.EnterReadLock();
                try
                {
                    OnElementFaultChanged(activeExpression.Arg, activeExpression.Fault, activeExpressionCounts[activeExpression]);
                }
                finally
                {
                    activeExpressionsAccess.ExitReadLock();
                }
            }
            else if (e.PropertyName == nameof(ActiveExpression<TElement, TResult>.Value))
            {
                activeExpressionsAccess.EnterReadLock();
                try
                {
                    OnElementResultChanged(activeExpression.Arg, activeExpression.Value, activeExpressionCounts[activeExpression]);
                }
                finally
                {
                    activeExpressionsAccess.ExitReadLock();
                }
            }
        }

        void ActiveExpressionPropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            var activeExpression = (ActiveExpression<TElement, TResult>)sender;
            if (e.PropertyName == nameof(ActiveExpression<TElement, TResult>.Fault))
            {
                activeExpressionsAccess.EnterReadLock();
                try
                {
                    OnElementFaultChanging(activeExpression.Arg, activeExpression.Fault, activeExpressionCounts[activeExpression]);
                }
                finally
                {
                    activeExpressionsAccess.ExitReadLock();
                }
            }
            else if (e.PropertyName == nameof(ActiveExpression<TElement, TResult>.Value))
            {
                activeExpressionsAccess.EnterReadLock();
                try
                {
                    OnElementResultChanging(activeExpression.Arg, activeExpression.Value, activeExpressionCounts[activeExpression]);
                }
                finally
                {
                    activeExpressionsAccess.ExitReadLock();
                }
            }
        }

        IReadOnlyList<(TElement element, TResult result)> AddActiveExpressions(int index, IEnumerable<TElement> elements)
        {
            if (elements.Any())
            {
                var addedActiveExpressions = new List<ActiveExpression<TElement, TResult>>();
                activeExpressionsAccess.EnterWriteLock();
                OnPropertyChanging(nameof(Count));
                try
                {
                    activeExpressions.InsertRange(index, elements.Select(element =>
                    {
                        var activeExpression = ActiveExpression.Create(expression, element, Options);
                        if (activeExpressionCounts.TryGetValue(activeExpression, out var activeExpressionCount))
                            activeExpressionCounts[activeExpression] = activeExpressionCount + 1;
                        else
                        {
                            activeExpressionCounts.Add(activeExpression, 1);
                            activeExpression.PropertyChanging += ActiveExpressionPropertyChanging;
                            activeExpression.PropertyChanged += ActiveExpressionPropertyChanged;
                        }
                        addedActiveExpressions.Add(activeExpression);
                        return (element, activeExpression);
                    }));
                }
                finally
                {
                    OnPropertyChanged(nameof(Count));
                    activeExpressionsAccess.ExitWriteLock();
                }
                return addedActiveExpressions.Select(ae => (ae.Arg, ae.Value)).ToImmutableArray();
            }
            return null;
        }

        void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var oldItems = e.OldItems != null ? e.OldItems.Cast<TElement>() : Enumerable.Empty<TElement>();
            var oldItemsCount = e.OldItems != null ? e.OldItems.Count : 0;
            var newItems = e.NewItems != null ? e.NewItems.Cast<TElement>() : Enumerable.Empty<TElement>();
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    if (activeExpressions.Count > 0)
                        OnElementsRemoved(RemoveActiveExpressions(0, activeExpressions.Count), 0);
                    var addedActiveExpressions = AddActiveExpressions(0, source);
                    if (addedActiveExpressions != null)
                        OnElementsAdded(addedActiveExpressions, 0);
                    break;
                case NotifyCollectionChangedAction.Move when oldItems.SequenceEqual(newItems):
                    List<(TElement element, ActiveExpression<TElement, TResult> activeExpression)> moving;
                    activeExpressionsAccess.EnterWriteLock();
                    try
                    {
                        moving = activeExpressions.GetRange(e.OldStartingIndex, oldItemsCount);
                        activeExpressions.RemoveRange(e.OldStartingIndex, oldItemsCount);
                        activeExpressions.InsertRange(e.NewStartingIndex, moving);
                    }
                    finally
                    {
                        activeExpressionsAccess.ExitWriteLock();
                    }
                    OnElementsMoved(moving.Select(ae => (ae.element, ae.activeExpression.Value)).ToList(), e.OldStartingIndex, e.NewStartingIndex);
                    break;
                default:
                    if (e.OldItems != null && e.OldStartingIndex >= 0)
                        OnElementsRemoved(RemoveActiveExpressions(e.OldStartingIndex, oldItemsCount), e.OldStartingIndex);
                    if (e.NewItems != null && e.NewStartingIndex >= 0)
                        OnElementsAdded(AddActiveExpressions(e.NewStartingIndex, newItems), e.NewStartingIndex);
                    break;
            }
        }

        protected override bool Dispose(bool disposing)
        {
            lock (rangeActiveExpressionsAccess)
            {
                if (--disposalCount > 0)
                    return false;
                rangeActiveExpressions.Remove((source, expression.ToString()));
            }
            RemoveActiveExpressions(0, activeExpressions.Count);
            if (source is INotifyCollectionChanged collectionChangedNotifier)
                collectionChangedNotifier.CollectionChanged -= CollectionChanged;
            if (source is INotifyElementFaultChanges faultNotifier)
            {
                faultNotifier.ElementFaultChanged -= SourceElementFaultChanged;
                faultNotifier.ElementFaultChanging -= SourceElementFaultChanging;
            }
            return true;
        }

        public IReadOnlyList<(object element, Exception fault)> GetElementFaults()
        {
            activeExpressionsAccess.EnterReadLock();
            try
            {
                return GetElementFaultsUnderLock();
            }
            finally
            {
                activeExpressionsAccess.ExitReadLock();
            }
        }

        internal IReadOnlyList<(object element, Exception fault)> GetElementFaultsUnderLock() => activeExpressions.Select(ae => (element: (object)ae.element, fault: ae.activeExpression.Fault)).Where(ef => ef.fault != null).ToImmutableArray();

        public IReadOnlyList<(TElement element, TResult result)> GetResults()
        {
            activeExpressionsAccess.EnterReadLock();
            try
            {
                return GetResultsUnderLock();
            }
            finally
            {
                activeExpressionsAccess.ExitReadLock();
            }
        }

        internal IReadOnlyList<(TElement element, TResult result)> GetResultsUnderLock() => activeExpressions.Select(ae => (ae.element, ae.activeExpression.Value)).ToImmutableArray();

        public IReadOnlyList<(TElement element, TResult result, Exception fault)> GetResultsAndFaults()
        {
            activeExpressionsAccess.EnterReadLock();
            try
            {
                return GetResultsAndFaultsUnderLock();
            }
            finally
            {
                activeExpressionsAccess.ExitReadLock();
            }
        }

        internal IReadOnlyList<(TElement element, TResult result, Exception fault)> GetResultsAndFaultsUnderLock() => activeExpressions.Select(ae => (ae.element, ae.activeExpression.Value, ae.activeExpression.Fault)).ToImmutableArray();

        public IReadOnlyList<(TElement element, TResult result, Exception fault, int count)> GetResultsFaultsAndCounts()
        {
            activeExpressionsAccess.EnterReadLock();
            try
            {
                return GetResultsFaultsAndCountsUnderLock();
            }
            finally
            {
                activeExpressionsAccess.ExitReadLock();
            }
        }

        internal IReadOnlyList<(TElement element, TResult result, Exception fault, int count)> GetResultsFaultsAndCountsUnderLock() => activeExpressions.Select(ae => (ae.element, ae.activeExpression.Value, ae.activeExpression.Fault, activeExpressionCounts[ae.activeExpression])).ToImmutableArray();

        void Initialize()
        {
            AddActiveExpressions(0, source);
            if (source is INotifyCollectionChanged collectionChangedNotifier)
                collectionChangedNotifier.CollectionChanged += CollectionChanged;
            if (source is INotifyElementFaultChanges faultNotifier)
            {
                faultNotifier.ElementFaultChanged += SourceElementFaultChanged;
                faultNotifier.ElementFaultChanging += SourceElementFaultChanging;
            }
        }

        protected virtual void OnElementFaultChanged(ElementFaultChangeEventArgs e) =>
            ElementFaultChanged?.Invoke(this, e);

        protected void OnElementFaultChanged(TElement element, Exception fault, int count) =>
            OnElementFaultChanged(new ElementFaultChangeEventArgs(element, fault, count));

        protected virtual void OnElementFaultChanging(ElementFaultChangeEventArgs e) =>
            ElementFaultChanging?.Invoke(this, e);

        protected void OnElementFaultChanging(TElement element, Exception fault, int count) =>
            OnElementFaultChanging(new ElementFaultChangeEventArgs(element, fault, count));

        protected virtual void OnElementResultChanged(RangeActiveExpressionResultChangeEventArgs<TElement, TResult> e) =>
            ElementResultChanged?.Invoke(this, e);

        protected void OnElementResultChanged(TElement element, TResult result, int count) =>
            OnElementResultChanged(new RangeActiveExpressionResultChangeEventArgs<TElement, TResult>(element, result, count));

        protected virtual void OnElementResultChanging(RangeActiveExpressionResultChangeEventArgs<TElement, TResult> e) =>
            ElementResultChanging?.Invoke(this, e);

        protected void OnElementResultChanging(TElement element, TResult result, int count) =>
            OnElementResultChanging(new RangeActiveExpressionResultChangeEventArgs<TElement, TResult>(element, result, count));

        protected virtual void OnElementsAdded(RangeActiveExpressionMembershipEventArgs<TElement, TResult> e) =>
            ElementsAdded?.Invoke(this, e);

        void OnElementsAdded(IReadOnlyList<(TElement element, TResult result)> elementResults, int index) =>
            OnElementsAdded(new RangeActiveExpressionMembershipEventArgs<TElement, TResult>(elementResults, index));

        protected virtual void OnElementsMoved(RangeActiveExpressionMovedEventArgs<TElement, TResult> e) =>
            ElementsMoved?.Invoke(this, e);

        void OnElementsMoved(IReadOnlyList<(TElement element, TResult result)> elementResults, int fromIndex, int toIndex) =>
            OnElementsMoved(new RangeActiveExpressionMovedEventArgs<TElement, TResult>(elementResults, fromIndex, toIndex));

        protected virtual void OnElementsRemoved(RangeActiveExpressionMembershipEventArgs<TElement, TResult> e) =>
            ElementsRemoved?.Invoke(this, e);

        void OnElementsRemoved(IReadOnlyList<(TElement element, TResult result)> elementResults, int index) =>
            OnElementsRemoved(new RangeActiveExpressionMembershipEventArgs<TElement, TResult>(elementResults, index));

        IReadOnlyList<(TElement element, TResult result)> RemoveActiveExpressions(int index, int count)
        {
            List<(TElement element, TResult result)> result = null;
            activeExpressionsAccess.EnterWriteLock();
            try
            {
                if (count > 0)
                {
                    result = new List<(TElement element, TResult result)>();
                    OnPropertyChanging(nameof(Count));
                    foreach (var (element, activeExpression) in activeExpressions.GetRange(index, count))
                    {
                        result.Add((element, activeExpression.Value));
                        var activeExpressionCount = activeExpressionCounts[activeExpression];
                        if (activeExpressionCount == 0)
                        {
                            activeExpressionCounts.Remove(activeExpression);
                            activeExpression.PropertyChanging -= ActiveExpressionPropertyChanging;
                            activeExpression.PropertyChanged -= ActiveExpressionPropertyChanged;
                        }
                        else
                            activeExpressionCounts[activeExpression] = activeExpressionCount - 1;
                        activeExpression.Dispose();
                    }
                    activeExpressions.RemoveRange(index, count);
                }
            }
            finally
            {
                if (result != null)
                    OnPropertyChanged(nameof(Count));
                activeExpressionsAccess.ExitWriteLock();
            }
            return (result ?? Enumerable.Empty<(TElement element, TResult result)>()).ToImmutableArray();
        }

        void SourceElementFaultChanged(object sender, ElementFaultChangeEventArgs e) => ElementFaultChanged?.Invoke(sender, e);

        void SourceElementFaultChanging(object sender, ElementFaultChangeEventArgs e) => ElementFaultChanging?.Invoke(sender, e);

        public int Count
        {
            get
            {
                activeExpressionsAccess.EnterReadLock();
                try
                {
                    return CountUnderLock;
                }
                finally
                {
                    activeExpressionsAccess.ExitReadLock();
                }
            }
        }

        internal int CountUnderLock => activeExpressions.Count;

        public ActiveExpressionOptions Options { get; }
    }
}
