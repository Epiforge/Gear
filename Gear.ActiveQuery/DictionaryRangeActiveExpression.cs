using Gear.ActiveExpressions;
using Gear.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace Gear.ActiveQuery
{
    /// <summary>
    /// Represents the dictionary of results derived from creating an active expression for each key-value pair in a dictionary
    /// </summary>
    /// <typeparam name="TResult">The type of the result of the active expression</typeparam>
    class DictionaryRangeActiveExpression<TResult> : SyncDisposablePropertyChangeNotifier, INotifyElementFaultChanges
    {
        public DictionaryRangeActiveExpression(IDictionary source, Expression<Func<object, object, TResult>> expression, ActiveExpressionOptions options)
        {
            this.source = source;
            this.expression = expression;
            Options = options;
            activeExpressions = new Dictionary<object, ActiveExpression<object, object, TResult>>();
            var keyValuePairs = new List<KeyValuePair<object, object>>();
            var enumerator = source.GetEnumerator();
            while (enumerator?.MoveNext() ?? false)
                keyValuePairs.Add(new KeyValuePair<object, object>(enumerator.Key, enumerator.Value));
            if (enumerator is IDisposable disposableEnumerator)
                disposableEnumerator.Dispose();
            if (keyValuePairs.Count > 0)
                AddActiveExpressions(keyValuePairs);
            if (source is INotifyDictionaryChanged dictionaryChangedNotifier)
            {
                dictionaryChangedNotifier.ValueAdded += SourceValueAdded;
                dictionaryChangedNotifier.ValueRemoved += SourceValueRemoved;
                dictionaryChangedNotifier.ValueReplaced += SourceValueReplaced;
                dictionaryChangedNotifier.ValuesAdded += SourceValuesAdded;
                dictionaryChangedNotifier.ValuesRemoved += SourceValuesRemoved;
            }
            if (source is INotifyElementFaultChanges faultNotifier)
            {
                faultNotifier.ElementFaultChanged += SourceFaultChanged;
                faultNotifier.ElementFaultChanging += SourceFaultChanging;
            }
        }

        readonly Dictionary<object, ActiveExpression<object, object, TResult>> activeExpressions;
        readonly ReaderWriterLockSlim activeExpressionsAccess = new ReaderWriterLockSlim();
        readonly Expression<Func<object, object, TResult>> expression;
        readonly IDictionary source;

        public event EventHandler<ElementFaultChangeEventArgs> ElementFaultChanged;
        public event EventHandler<ElementFaultChangeEventArgs> ElementFaultChanging;
        public event EventHandler<NotifyDictionaryValueEventArgs<object, TResult>> ValueAdded;
        public event EventHandler<NotifyDictionaryValueEventArgs<object, TResult>> ValueRemoved;
        public event EventHandler<NotifyDictionaryValueReplacedEventArgs<object, TResult>> ValueReplaced;
        public event EventHandler<RangeActiveExpressionResultChangeEventArgs<object, TResult>> ValueResultChanged;
        public event EventHandler<RangeActiveExpressionResultChangeEventArgs<object, TResult>> ValueResultChanging;
        public event EventHandler<NotifyDictionaryValuesEventArgs<object, TResult>> ValuesAdded;
        public event EventHandler<NotifyDictionaryValuesEventArgs<object, TResult>> ValuesRemoved;

        void ActiveExpressionPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var activeExpression = (ActiveExpression<object, object, TResult>)sender;
            if (e.PropertyName == nameof(ActiveExpression<object, object, TResult>.Fault))
            {
                activeExpressionsAccess.EnterReadLock();
                try
                {
                    OnElementFaultChanged(new ElementFaultChangeEventArgs(activeExpression.Arg1, activeExpression.Fault));
                }
                finally
                {
                    activeExpressionsAccess.ExitReadLock();
                }
            }
            else if (e.PropertyName == nameof(ActiveExpression<object, object, TResult>.Value))
            {
                activeExpressionsAccess.EnterReadLock();
                try
                {
                    OnValueResultChanged(activeExpression.Arg1, activeExpression.Value);
                }
                finally
                {
                    activeExpressionsAccess.ExitReadLock();
                }
            }
        }

        void ActiveExpressionPropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            var activeExpression = (ActiveExpression<object, object, TResult>)sender;
            if (e.PropertyName == nameof(ActiveExpression<object, object, TResult>.Fault))
            {
                activeExpressionsAccess.EnterReadLock();
                try
                {
                    OnElementFaultChanging(new ElementFaultChangeEventArgs(activeExpression.Arg1, activeExpression.Fault));
                }
                finally
                {
                    activeExpressionsAccess.ExitReadLock();
                }
            }
            else if (e.PropertyName == nameof(ActiveExpression<object, object, TResult>.Value))
            {
                activeExpressionsAccess.EnterReadLock();
                try
                {
                    OnValueResultChanging(activeExpression.Arg1, activeExpression.Value);
                }
                finally
                {
                    activeExpressionsAccess.ExitReadLock();
                }
            }
        }

        TResult AddActiveExpression(object key, object value)
        {
            activeExpressionsAccess.EnterWriteLock();
            OnPropertyChanging(nameof(Count));
            try
            {
                var activeExpression = ActiveExpression.Create(expression, key, value, Options);
                activeExpression.PropertyChanging += ActiveExpressionPropertyChanging;
                activeExpression.PropertyChanged += ActiveExpressionPropertyChanged;
                return activeExpression.Value;
            }
            finally
            {
                OnPropertyChanged(nameof(Count));
                activeExpressionsAccess.ExitWriteLock();
            }
        }

        IReadOnlyList<KeyValuePair<object, TResult>> AddActiveExpressions(IEnumerable<KeyValuePair<object, object>> keyValuePairs)
        {
            if (keyValuePairs.Any())
            {
                var addedActiveExpressions = new List<ActiveExpression<object, object, TResult>>();
                activeExpressionsAccess.EnterWriteLock();
                OnPropertyChanging(nameof(Count));
                try
                {
                    foreach (var keyValuePair in keyValuePairs)
                    {
                        var activeExpression = ActiveExpression.Create(expression, keyValuePair.Key, keyValuePair.Value, Options);
                        activeExpression.PropertyChanging += ActiveExpressionPropertyChanging;
                        activeExpression.PropertyChanged += ActiveExpressionPropertyChanged;
                        addedActiveExpressions.Add(activeExpression);
                    }
                }
                finally
                {
                    OnPropertyChanged(nameof(Count));
                    activeExpressionsAccess.ExitWriteLock();
                }
                return addedActiveExpressions.Select(ae => new KeyValuePair<object, TResult>(ae.Arg1, ae.Value)).ToImmutableArray();
            }
            return null;
        }

        protected override void Dispose(bool disposing)
        {
            RemoveActiveExpressions(activeExpressions.Keys);
            if (source is INotifyDictionaryChanged dictionaryChangedNotifier)
            {
                dictionaryChangedNotifier.ValueAdded -= SourceValueAdded;
                dictionaryChangedNotifier.ValueRemoved -= SourceValueRemoved;
                dictionaryChangedNotifier.ValueReplaced -= SourceValueReplaced;
                dictionaryChangedNotifier.ValuesAdded -= SourceValuesAdded;
                dictionaryChangedNotifier.ValuesRemoved -= SourceValuesRemoved;
            }
            if (source is INotifyElementFaultChanges faultNotifier)
            {
                faultNotifier.ElementFaultChanged -= SourceFaultChanged;
                faultNotifier.ElementFaultChanging -= SourceFaultChanging;
            }
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

        internal IReadOnlyList<(object element, Exception fault)> GetElementFaultsUnderLock() => activeExpressions.Select(ae => ((object)ae.Key, ae.Value.Fault)).ToImmutableArray();

        public IReadOnlyList<(object key, TResult result)> GetResults()
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

        internal IReadOnlyList<(object key, TResult result)> GetResultsUnderLock() => activeExpressions.Select(ae => (ae.Key, ae.Value.Value)).ToImmutableArray();

        public IReadOnlyList<(object key, TResult result, Exception fault)> GetResultsAndFaults()
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

        internal IReadOnlyList<(object key, TResult result, Exception fault)> GetResultsAndFaultsUnderLock() => activeExpressions.Select(ae => (ae.Key, ae.Value.Value, ae.Value.Fault)).ToImmutableArray();

        protected virtual void OnElementFaultChanged(ElementFaultChangeEventArgs e) =>
            ElementFaultChanged?.Invoke(this, e);

        protected void OnElementFaultChanged(object key, Exception fault) =>
            OnElementFaultChanged(new ElementFaultChangeEventArgs(key, fault));

        protected virtual void OnElementFaultChanging(ElementFaultChangeEventArgs e) =>
            ElementFaultChanging?.Invoke(this, e);

        protected void OnElementFaultChanging(object key, Exception fault) =>
            OnElementFaultChanging(new ElementFaultChangeEventArgs(key, fault));

        protected virtual void OnValueAdded(NotifyDictionaryValueEventArgs<object, TResult> e) =>
            ValueAdded?.Invoke(this, e);

        protected void OnValueAdded(object key, TResult result) =>
            OnValueAdded(new NotifyDictionaryValueEventArgs<object, TResult>(key, result));

        protected virtual void OnValueRemoved(NotifyDictionaryValueEventArgs<object, TResult> e) =>
            ValueRemoved?.Invoke(this, e);

        protected void OnValueRemoved(object key, TResult result) =>
            OnValueRemoved(new NotifyDictionaryValueEventArgs<object, TResult>(key, result));

        protected virtual void OnValueReplaced(NotifyDictionaryValueReplacedEventArgs<object, TResult> e) =>
            ValueReplaced?.Invoke(this, e);

        protected void OnValueReplaced(object key, TResult oldResult, TResult newResult) =>
            OnValueReplaced(new NotifyDictionaryValueReplacedEventArgs<object, TResult>(key, oldResult, newResult));

        protected virtual void OnValueResultChanged(RangeActiveExpressionResultChangeEventArgs<object, TResult> e) =>
            ValueResultChanged?.Invoke(this, e);

        protected void OnValueResultChanged(object key, TResult result) =>
            OnValueResultChanged(new RangeActiveExpressionResultChangeEventArgs<object, TResult>(key, result));

        protected virtual void OnValueResultChanging(RangeActiveExpressionResultChangeEventArgs<object, TResult> e) =>
            ValueResultChanging?.Invoke(this, e);

        protected void OnValueResultChanging(object key, TResult result) =>
            OnValueResultChanging(new RangeActiveExpressionResultChangeEventArgs<object, TResult>(key, result));

        protected virtual void OnValuesAdded(NotifyDictionaryValuesEventArgs<object, TResult> e) =>
            ValuesAdded?.Invoke(this, e);

        protected void OnValuesAdded(IReadOnlyList<KeyValuePair<object, TResult>> keyValuePairs) =>
            OnValuesAdded(new NotifyDictionaryValuesEventArgs<object, TResult>(keyValuePairs));

        protected virtual void OnValuesRemoved(NotifyDictionaryValuesEventArgs<object, TResult> e) =>
            ValuesRemoved?.Invoke(this, e);

        protected void OnValuesRemoved(IReadOnlyList<KeyValuePair<object, TResult>> keyValuePairs) =>
            OnValuesRemoved(new NotifyDictionaryValuesEventArgs<object, TResult>(keyValuePairs));

        TResult RemoveActiveExpression(object key)
        {
            OnPropertyChanging(nameof(Count));
            activeExpressionsAccess.EnterWriteLock();
            try
            {
                var activeExpression = activeExpressions[key];
                var result = activeExpression.Value;
                activeExpressions.Remove(key);
                activeExpression.PropertyChanging -= ActiveExpressionPropertyChanging;
                activeExpression.PropertyChanged -= ActiveExpressionPropertyChanged;
                activeExpression.Dispose();
                return result;
            }
            finally
            {
                activeExpressionsAccess.ExitWriteLock();
                OnPropertyChanged(nameof(Count));
            }
        }

        IReadOnlyList<KeyValuePair<object, TResult>> RemoveActiveExpressions(IEnumerable<object> keys)
        {
            List<KeyValuePair<object, TResult>> result = null;
            activeExpressionsAccess.EnterWriteLock();
            try
            {
                if (keys?.Any() ?? false)
                {
                    result = new List<KeyValuePair<object, TResult>>();
                    OnPropertyChanging(nameof(Count));
                    foreach (var key in keys)
                    {
                        var activeExpression = activeExpressions[key];
                        result.Add(new KeyValuePair<object, TResult>(key, activeExpression.Value));
                        activeExpressions.Remove(key);
                        activeExpression.PropertyChanging -= ActiveExpressionPropertyChanging;
                        activeExpression.PropertyChanged -= ActiveExpressionPropertyChanged;
                        activeExpression.Dispose();
                    }
                }
            }
            finally
            {
                if (result != null)
                    OnPropertyChanged(nameof(Count));
                activeExpressionsAccess.ExitWriteLock();
            }
            return (result ?? Enumerable.Empty<KeyValuePair<object, TResult>>()).ToImmutableArray();
        }

        (TResult oldResult, TResult newResult) ReplaceActiveExpression(object key, object value)
        {
            activeExpressionsAccess.EnterWriteLock();
            try
            {
                var oldActiveExpression = activeExpressions[key];
                var oldResult = oldActiveExpression.Value;
                oldActiveExpression.PropertyChanging -= ActiveExpressionPropertyChanging;
                oldActiveExpression.PropertyChanged -= ActiveExpressionPropertyChanged;
                oldActiveExpression.Dispose();
                var newActiveExpression = ActiveExpression.Create(expression, key, value, Options);
                newActiveExpression.PropertyChanging += ActiveExpressionPropertyChanging;
                newActiveExpression.PropertyChanged += ActiveExpressionPropertyChanged;
                activeExpressions[key] = newActiveExpression;
                return (oldResult, newActiveExpression.Value);
            }
            finally
            {
                activeExpressionsAccess.ExitWriteLock();
            }
        }

        void SourceFaultChanged(object sender, ElementFaultChangeEventArgs e) => ElementFaultChanged?.Invoke(sender, e);

        void SourceFaultChanging(object sender, ElementFaultChangeEventArgs e) => ElementFaultChanging?.Invoke(sender, e);

        void SourceValueAdded(object sender, NotifyDictionaryValueEventArgs e) => OnValueAdded(e.Key, AddActiveExpression(e.Key, e.Value));

        void SourceValueRemoved(object sender, NotifyDictionaryValueEventArgs e) => OnValueRemoved(e.Key, RemoveActiveExpression(e.Key));

        void SourceValueReplaced(object sender, NotifyDictionaryValueReplacedEventArgs e)
        {
            var key = e.Key;
            var (oldResult, newResult) = ReplaceActiveExpression(key, e.NewValue);
            OnValueReplaced(key, oldResult, newResult);
        }

        void SourceValuesAdded(object sender, NotifyDictionaryValuesEventArgs e) => OnValuesAdded(AddActiveExpressions(e.KeyValuePairs));

        void SourceValuesRemoved(object sender, NotifyDictionaryValuesEventArgs e) => OnValuesRemoved(RemoveActiveExpressions(e.KeyValuePairs.Select(kv => kv.Key)));

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
    /// Represents the dictionary of results derived from creating an active expression for each key-value pair in a dictionary
    /// </summary>
    /// <typeparam name="TKey">The type of the keys</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary</typeparam>
    /// <typeparam name="TResult">The type of the result of the active expression</typeparam>
    class DictionaryRangeActiveExpression<TKey, TValue, TResult> : SyncDisposablePropertyChangeNotifier, INotifyElementFaultChanges
    {
        public DictionaryRangeActiveExpression(IDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, TResult>> expression, ActiveExpressionOptions options)
        {
            this.source = source;
            this.expression = expression;
            Options = options;
            activeExpressions = this.source.CreateSimilarDictionary<TKey, TValue, ActiveExpression<TKey, TValue, TResult>>();
            AddActiveExpressions(source);
            if (source is INotifyDictionaryChanged<TKey, TValue> dictionaryChangedNotifier)
            {
                dictionaryChangedNotifier.ValueAdded += SourceValueAdded;
                dictionaryChangedNotifier.ValueRemoved += SourceValueRemoved;
                dictionaryChangedNotifier.ValueReplaced += SourceValueReplaced;
                dictionaryChangedNotifier.ValuesAdded += SourceValuesAdded;
                dictionaryChangedNotifier.ValuesRemoved += SourceValuesRemoved;
            }
            if (source is INotifyElementFaultChanges faultNotifier)
            {
                faultNotifier.ElementFaultChanged += SourceFaultChanged;
                faultNotifier.ElementFaultChanging += SourceFaultChanging;
            }
        }

        readonly IDictionary<TKey, ActiveExpression<TKey, TValue, TResult>> activeExpressions;
        readonly ReaderWriterLockSlim activeExpressionsAccess = new ReaderWriterLockSlim();
        readonly Expression<Func<TKey, TValue, TResult>> expression;
        readonly IDictionary<TKey, TValue> source;

        public event EventHandler<ElementFaultChangeEventArgs> ElementFaultChanged;
        public event EventHandler<ElementFaultChangeEventArgs> ElementFaultChanging;
        public event EventHandler<NotifyDictionaryValueEventArgs<TKey, TResult>> ValueAdded;
        public event EventHandler<NotifyDictionaryValueEventArgs<TKey, TResult>> ValueRemoved;
        public event EventHandler<NotifyDictionaryValueReplacedEventArgs<TKey, TResult>> ValueReplaced;
        public event EventHandler<RangeActiveExpressionResultChangeEventArgs<TKey, TResult>> ValueResultChanged;
        public event EventHandler<RangeActiveExpressionResultChangeEventArgs<TKey, TResult>> ValueResultChanging;
        public event EventHandler<NotifyDictionaryValuesEventArgs<TKey, TResult>> ValuesAdded;
        public event EventHandler<NotifyDictionaryValuesEventArgs<TKey, TResult>> ValuesRemoved;

        void ActiveExpressionPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var activeExpression = (ActiveExpression<TKey, TValue, TResult>)sender;
            if (e.PropertyName == nameof(ActiveExpression<TKey, TValue, TResult>.Fault))
            {
                activeExpressionsAccess.EnterReadLock();
                try
                {
                    OnElementFaultChanged(new ElementFaultChangeEventArgs(activeExpression.Arg1, activeExpression.Fault));
                }
                finally
                {
                    activeExpressionsAccess.ExitReadLock();
                }
            }
            else if (e.PropertyName == nameof(ActiveExpression<TKey, TValue, TResult>.Value))
            {
                activeExpressionsAccess.EnterReadLock();
                try
                {
                    OnValueResultChanged(activeExpression.Arg1, activeExpression.Value);
                }
                finally
                {
                    activeExpressionsAccess.ExitReadLock();
                }
            }
        }

        void ActiveExpressionPropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            var activeExpression = (ActiveExpression<TKey, TValue, TResult>)sender;
            if (e.PropertyName == nameof(ActiveExpression<TKey, TValue, TResult>.Fault))
            {
                activeExpressionsAccess.EnterReadLock();
                try
                {
                    OnElementFaultChanging(new ElementFaultChangeEventArgs(activeExpression.Arg1, activeExpression.Fault));
                }
                finally
                {
                    activeExpressionsAccess.ExitReadLock();
                }
            }
            else if (e.PropertyName == nameof(ActiveExpression<TKey, TValue, TResult>.Value))
            {
                activeExpressionsAccess.EnterReadLock();
                try
                {
                    OnValueResultChanging(activeExpression.Arg1, activeExpression.Value);
                }
                finally
                {
                    activeExpressionsAccess.ExitReadLock();
                }
            }
        }

        TResult AddActiveExpression(TKey key, TValue value)
        {
            activeExpressionsAccess.EnterWriteLock();
            OnPropertyChanging(nameof(Count));
            try
            {
                var activeExpression = ActiveExpression.Create(expression, key, value, Options);
                activeExpression.PropertyChanging += ActiveExpressionPropertyChanging;
                activeExpression.PropertyChanged += ActiveExpressionPropertyChanged;
                return activeExpression.Value;
            }
            finally
            {
                OnPropertyChanged(nameof(Count));
                activeExpressionsAccess.ExitWriteLock();
            }
        }

        IReadOnlyList<KeyValuePair<TKey, TResult>> AddActiveExpressions(IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs)
        {
            if (keyValuePairs.Any())
            {
                var addedActiveExpressions = new List<ActiveExpression<TKey, TValue, TResult>>();
                activeExpressionsAccess.EnterWriteLock();
                OnPropertyChanging(nameof(Count));
                try
                {
                    foreach (var keyValuePair in keyValuePairs)
                    {
                        var activeExpression = ActiveExpression.Create(expression, keyValuePair.Key, keyValuePair.Value, Options);
                        activeExpression.PropertyChanging += ActiveExpressionPropertyChanging;
                        activeExpression.PropertyChanged += ActiveExpressionPropertyChanged;
                        addedActiveExpressions.Add(activeExpression);
                    }
                }
                finally
                {
                    OnPropertyChanged(nameof(Count));
                    activeExpressionsAccess.ExitWriteLock();
                }
                return addedActiveExpressions.Select(ae => new KeyValuePair<TKey, TResult>(ae.Arg1, ae.Value)).ToImmutableArray();
            }
            return null;
        }

        protected override void Dispose(bool disposing)
        {
            RemoveActiveExpressions(activeExpressions.Keys);
            if (source is INotifyDictionaryChanged<TKey, TValue> dictionaryChangedNotifier)
            {
                dictionaryChangedNotifier.ValueAdded -= SourceValueAdded;
                dictionaryChangedNotifier.ValueRemoved -= SourceValueRemoved;
                dictionaryChangedNotifier.ValueReplaced -= SourceValueReplaced;
                dictionaryChangedNotifier.ValuesAdded -= SourceValuesAdded;
                dictionaryChangedNotifier.ValuesRemoved -= SourceValuesRemoved;
            }
            if (source is INotifyElementFaultChanges faultNotifier)
            {
                faultNotifier.ElementFaultChanged -= SourceFaultChanged;
                faultNotifier.ElementFaultChanging -= SourceFaultChanging;
            }
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

        internal IReadOnlyList<(object element, Exception fault)> GetElementFaultsUnderLock() => activeExpressions.Select(ae => ((object)ae.Key, ae.Value.Fault)).ToImmutableArray();

        public IReadOnlyList<(TKey key, TResult result)> GetResults()
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

        internal IReadOnlyList<(TKey key, TResult result)> GetResultsUnderLock() => activeExpressions.Select(ae => (ae.Key, ae.Value.Value)).ToImmutableArray();

        public IReadOnlyList<(TKey key, TResult result, Exception fault)> GetResultsAndFaults()
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

        internal IReadOnlyList<(TKey key, TResult result, Exception fault)> GetResultsAndFaultsUnderLock() => activeExpressions.Select(ae => (ae.Key, ae.Value.Value, ae.Value.Fault)).ToImmutableArray();

        protected virtual void OnElementFaultChanged(ElementFaultChangeEventArgs e) =>
            ElementFaultChanged?.Invoke(this, e);

        protected void OnElementFaultChanged(TKey key, Exception fault) =>
            OnElementFaultChanged(new ElementFaultChangeEventArgs(key, fault));

        protected virtual void OnElementFaultChanging(ElementFaultChangeEventArgs e) =>
            ElementFaultChanging?.Invoke(this, e);

        protected void OnElementFaultChanging(TKey key, Exception fault) =>
            OnElementFaultChanging(new ElementFaultChangeEventArgs(key, fault));

        protected virtual void OnValueAdded(NotifyDictionaryValueEventArgs<TKey, TResult> e) =>
            ValueAdded?.Invoke(this, e);

        protected void OnValueAdded(TKey key, TResult result) =>
            OnValueAdded(new NotifyDictionaryValueEventArgs<TKey, TResult>(key, result));

        protected virtual void OnValueRemoved(NotifyDictionaryValueEventArgs<TKey, TResult> e) =>
            ValueRemoved?.Invoke(this, e);

        protected void OnValueRemoved(TKey key, TResult result) =>
            OnValueRemoved(new NotifyDictionaryValueEventArgs<TKey, TResult>(key, result));

        protected virtual void OnValueReplaced(NotifyDictionaryValueReplacedEventArgs<TKey, TResult> e) =>
            ValueReplaced?.Invoke(this, e);

        protected void OnValueReplaced(TKey key, TResult oldResult, TResult newResult) =>
            OnValueReplaced(new NotifyDictionaryValueReplacedEventArgs<TKey, TResult>(key, oldResult, newResult));

        protected virtual void OnValueResultChanged(RangeActiveExpressionResultChangeEventArgs<TKey, TResult> e) =>
            ValueResultChanged?.Invoke(this, e);

        protected void OnValueResultChanged(TKey key, TResult result) =>
            OnValueResultChanged(new RangeActiveExpressionResultChangeEventArgs<TKey, TResult>(key, result));

        protected virtual void OnValueResultChanging(RangeActiveExpressionResultChangeEventArgs<TKey, TResult> e) =>
            ValueResultChanging?.Invoke(this, e);

        protected void OnValueResultChanging(TKey key, TResult result) =>
            OnValueResultChanging(new RangeActiveExpressionResultChangeEventArgs<TKey, TResult>(key, result));

        protected virtual void OnValuesAdded(NotifyDictionaryValuesEventArgs<TKey, TResult> e) =>
            ValuesAdded?.Invoke(this, e);

        protected void OnValuesAdded(IReadOnlyList<KeyValuePair<TKey, TResult>> keyValuePairs) =>
            OnValuesAdded(new NotifyDictionaryValuesEventArgs<TKey, TResult>(keyValuePairs));

        protected virtual void OnValuesRemoved(NotifyDictionaryValuesEventArgs<TKey, TResult> e) =>
            ValuesRemoved?.Invoke(this, e);

        protected void OnValuesRemoved(IReadOnlyList<KeyValuePair<TKey, TResult>> keyValuePairs) =>
            OnValuesRemoved(new NotifyDictionaryValuesEventArgs<TKey, TResult>(keyValuePairs));

        TResult RemoveActiveExpression(TKey key)
        {
            OnPropertyChanging(nameof(Count));
            activeExpressionsAccess.EnterWriteLock();
            try
            {
                var activeExpression = activeExpressions[key];
                var result = activeExpression.Value;
                activeExpressions.Remove(key);
                activeExpression.PropertyChanging -= ActiveExpressionPropertyChanging;
                activeExpression.PropertyChanged -= ActiveExpressionPropertyChanged;
                activeExpression.Dispose();
                return result;
            }
            finally
            {
                activeExpressionsAccess.ExitWriteLock();
                OnPropertyChanged(nameof(Count));
            }
        }

        IReadOnlyList<KeyValuePair<TKey, TResult>> RemoveActiveExpressions(IEnumerable<TKey> keys)
        {
            List<KeyValuePair<TKey, TResult>> result = null;
            activeExpressionsAccess.EnterWriteLock();
            try
            {
                if (keys?.Any() ?? false)
                {
                    result = new List<KeyValuePair<TKey, TResult>>();
                    OnPropertyChanging(nameof(Count));
                    foreach (var key in keys)
                    {
                        var activeExpression = activeExpressions[key];
                        result.Add(new KeyValuePair<TKey, TResult>(key, activeExpression.Value));
                        activeExpressions.Remove(key);
                        activeExpression.PropertyChanging -= ActiveExpressionPropertyChanging;
                        activeExpression.PropertyChanged -= ActiveExpressionPropertyChanged;
                        activeExpression.Dispose();
                    }
                }
            }
            finally
            {
                if (result != null)
                    OnPropertyChanged(nameof(Count));
                activeExpressionsAccess.ExitWriteLock();
            }
            return (result ?? Enumerable.Empty<KeyValuePair<TKey, TResult>>()).ToImmutableArray();
        }

        (TResult oldResult, TResult newResult) ReplaceActiveExpression(TKey key, TValue value)
        {
            activeExpressionsAccess.EnterWriteLock();
            try
            {
                var oldActiveExpression = activeExpressions[key];
                var oldResult = oldActiveExpression.Value;
                oldActiveExpression.PropertyChanging -= ActiveExpressionPropertyChanging;
                oldActiveExpression.PropertyChanged -= ActiveExpressionPropertyChanged;
                oldActiveExpression.Dispose();
                var newActiveExpression = ActiveExpression.Create(expression, key, value, Options);
                newActiveExpression.PropertyChanging += ActiveExpressionPropertyChanging;
                newActiveExpression.PropertyChanged += ActiveExpressionPropertyChanged;
                activeExpressions[key] = newActiveExpression;
                return (oldResult, newActiveExpression.Value);
            }
            finally
            {
                activeExpressionsAccess.ExitWriteLock();
            }
        }

        void SourceFaultChanged(object sender, ElementFaultChangeEventArgs e) => ElementFaultChanged?.Invoke(sender, e);

        void SourceFaultChanging(object sender, ElementFaultChangeEventArgs e) => ElementFaultChanging?.Invoke(sender, e);

        void SourceValueAdded(object sender, NotifyDictionaryValueEventArgs<TKey, TValue> e) => OnValueAdded(e.Key, AddActiveExpression(e.Key, e.Value));

        void SourceValueRemoved(object sender, NotifyDictionaryValueEventArgs<TKey, TValue> e) => OnValueRemoved(e.Key, RemoveActiveExpression(e.Key));

        void SourceValueReplaced(object sender, NotifyDictionaryValueReplacedEventArgs<TKey, TValue> e)
        {
            var key = e.Key;
            var (oldResult, newResult) = ReplaceActiveExpression(key, e.NewValue);
            OnValueReplaced(key, oldResult, newResult);
        }

        void SourceValuesAdded(object sender, NotifyDictionaryValuesEventArgs<TKey, TValue> e) => OnValuesAdded(AddActiveExpressions(e.KeyValuePairs));

        void SourceValuesRemoved(object sender, NotifyDictionaryValuesEventArgs<TKey, TValue> e) => OnValuesRemoved(RemoveActiveExpressions(e.KeyValuePairs.Select(kv => kv.Key)));

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

        public int CountUnderLock => activeExpressions.Count;

        public ActiveExpressionOptions Options { get; }
    }

    /// <summary>
    /// Represents the dictionary of results derived from creating an active expression for each key-value pair in a dictionary
    /// </summary>
    /// <typeparam name="TKey">The type of the keys</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary</typeparam>
    /// <typeparam name="TResult">The type of the result of the active expression</typeparam>
    class ReadOnlyDictionaryRangeActiveExpression<TKey, TValue, TResult> : SyncDisposablePropertyChangeNotifier, INotifyElementFaultChanges
    {
        public ReadOnlyDictionaryRangeActiveExpression(IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, TResult>> expression, ActiveExpressionOptions options)
        {
            this.source = source;
            this.expression = expression;
            Options = options;
            activeExpressions = this.source.CreateSimilarDictionary<TKey, TValue, ActiveExpression<TKey, TValue, TResult>>();
            AddActiveExpressions(source);
            if (source is INotifyDictionaryChanged<TKey, TValue> dictionaryChangedNotifier)
            {
                dictionaryChangedNotifier.ValueAdded += SourceValueAdded;
                dictionaryChangedNotifier.ValueRemoved += SourceValueRemoved;
                dictionaryChangedNotifier.ValueReplaced += SourceValueReplaced;
                dictionaryChangedNotifier.ValuesAdded += SourceValuesAdded;
                dictionaryChangedNotifier.ValuesRemoved += SourceValuesRemoved;
            }
            if (source is INotifyElementFaultChanges faultNotifier)
            {
                faultNotifier.ElementFaultChanged += SourceFaultChanged;
                faultNotifier.ElementFaultChanging += SourceFaultChanging;
            }
        }

        readonly IDictionary<TKey, ActiveExpression<TKey, TValue, TResult>> activeExpressions;
        readonly ReaderWriterLockSlim activeExpressionsAccess = new ReaderWriterLockSlim();
        readonly Expression<Func<TKey, TValue, TResult>> expression;
        readonly IReadOnlyDictionary<TKey, TValue> source;

        public event EventHandler<ElementFaultChangeEventArgs> ElementFaultChanged;
        public event EventHandler<ElementFaultChangeEventArgs> ElementFaultChanging;
        public event EventHandler<NotifyDictionaryValueEventArgs<TKey, TResult>> ValueAdded;
        public event EventHandler<NotifyDictionaryValueEventArgs<TKey, TResult>> ValueRemoved;
        public event EventHandler<NotifyDictionaryValueReplacedEventArgs<TKey, TResult>> ValueReplaced;
        public event EventHandler<RangeActiveExpressionResultChangeEventArgs<TKey, TResult>> ValueResultChanged;
        public event EventHandler<RangeActiveExpressionResultChangeEventArgs<TKey, TResult>> ValueResultChanging;
        public event EventHandler<NotifyDictionaryValuesEventArgs<TKey, TResult>> ValuesAdded;
        public event EventHandler<NotifyDictionaryValuesEventArgs<TKey, TResult>> ValuesRemoved;

        void ActiveExpressionPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var activeExpression = (ActiveExpression<TKey, TValue, TResult>)sender;
            if (e.PropertyName == nameof(ActiveExpression<TKey, TValue, TResult>.Fault))
            {
                activeExpressionsAccess.EnterReadLock();
                try
                {
                    OnElementFaultChanged(new ElementFaultChangeEventArgs(activeExpression.Arg1, activeExpression.Fault));
                }
                finally
                {
                    activeExpressionsAccess.ExitReadLock();
                }
            }
            else if (e.PropertyName == nameof(ActiveExpression<TKey, TValue, TResult>.Value))
            {
                activeExpressionsAccess.EnterReadLock();
                try
                {
                    OnValueResultChanged(activeExpression.Arg1, activeExpression.Value);
                }
                finally
                {
                    activeExpressionsAccess.ExitReadLock();
                }
            }
        }

        void ActiveExpressionPropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            var activeExpression = (ActiveExpression<TKey, TValue, TResult>)sender;
            if (e.PropertyName == nameof(ActiveExpression<TKey, TValue, TResult>.Fault))
            {
                activeExpressionsAccess.EnterReadLock();
                try
                {
                    OnElementFaultChanging(new ElementFaultChangeEventArgs(activeExpression.Arg1, activeExpression.Fault));
                }
                finally
                {
                    activeExpressionsAccess.ExitReadLock();
                }
            }
            else if (e.PropertyName == nameof(ActiveExpression<TKey, TValue, TResult>.Value))
            {
                activeExpressionsAccess.EnterReadLock();
                try
                {
                    OnValueResultChanging(activeExpression.Arg1, activeExpression.Value);
                }
                finally
                {
                    activeExpressionsAccess.ExitReadLock();
                }
            }
        }

        TResult AddActiveExpression(TKey key, TValue value)
        {
            activeExpressionsAccess.EnterWriteLock();
            OnPropertyChanging(nameof(Count));
            try
            {
                var activeExpression = ActiveExpression.Create(expression, key, value, Options);
                activeExpression.PropertyChanging += ActiveExpressionPropertyChanging;
                activeExpression.PropertyChanged += ActiveExpressionPropertyChanged;
                return activeExpression.Value;
            }
            finally
            {
                OnPropertyChanged(nameof(Count));
                activeExpressionsAccess.ExitWriteLock();
            }
        }

        IReadOnlyList<KeyValuePair<TKey, TResult>> AddActiveExpressions(IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs)
        {
            if (keyValuePairs.Any())
            {
                var addedActiveExpressions = new List<ActiveExpression<TKey, TValue, TResult>>();
                activeExpressionsAccess.EnterWriteLock();
                OnPropertyChanging(nameof(Count));
                try
                {
                    foreach (var keyValuePair in keyValuePairs)
                    {
                        var activeExpression = ActiveExpression.Create(expression, keyValuePair.Key, keyValuePair.Value, Options);
                        activeExpression.PropertyChanging += ActiveExpressionPropertyChanging;
                        activeExpression.PropertyChanged += ActiveExpressionPropertyChanged;
                        addedActiveExpressions.Add(activeExpression);
                    }
                }
                finally
                {
                    OnPropertyChanged(nameof(Count));
                    activeExpressionsAccess.ExitWriteLock();
                }
                return addedActiveExpressions.Select(ae => new KeyValuePair<TKey, TResult>(ae.Arg1, ae.Value)).ToImmutableArray();
            }
            return null;
        }

        protected override void Dispose(bool disposing)
        {
            RemoveActiveExpressions(activeExpressions.Keys);
            if (source is INotifyDictionaryChanged<TKey, TValue> dictionaryChangedNotifier)
            {
                dictionaryChangedNotifier.ValueAdded -= SourceValueAdded;
                dictionaryChangedNotifier.ValueRemoved -= SourceValueRemoved;
                dictionaryChangedNotifier.ValueReplaced -= SourceValueReplaced;
                dictionaryChangedNotifier.ValuesAdded -= SourceValuesAdded;
                dictionaryChangedNotifier.ValuesRemoved -= SourceValuesRemoved;
            }
            if (source is INotifyElementFaultChanges faultNotifier)
            {
                faultNotifier.ElementFaultChanged -= SourceFaultChanged;
                faultNotifier.ElementFaultChanging -= SourceFaultChanging;
            }
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

        internal IReadOnlyList<(object element, Exception fault)> GetElementFaultsUnderLock() => activeExpressions.Select(ae => ((object)ae.Key, ae.Value.Fault)).ToImmutableArray();

        public IReadOnlyList<(TKey key, TResult result)> GetResults()
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

        internal IReadOnlyList<(TKey key, TResult result)> GetResultsUnderLock() => activeExpressions.Select(ae => (ae.Key, ae.Value.Value)).ToImmutableArray();

        public IReadOnlyList<(TKey key, TResult result, Exception fault)> GetResultsAndFaults()
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

        internal IReadOnlyList<(TKey key, TResult result, Exception fault)> GetResultsAndFaultsUnderLock() => activeExpressions.Select(ae => (ae.Key, ae.Value.Value, ae.Value.Fault)).ToImmutableArray();

        protected virtual void OnElementFaultChanged(ElementFaultChangeEventArgs e) =>
            ElementFaultChanged?.Invoke(this, e);

        protected void OnElementFaultChanged(TKey key, Exception fault) =>
            OnElementFaultChanged(new ElementFaultChangeEventArgs(key, fault));

        protected virtual void OnElementFaultChanging(ElementFaultChangeEventArgs e) =>
            ElementFaultChanging?.Invoke(this, e);

        protected void OnElementFaultChanging(TKey key, Exception fault) =>
            OnElementFaultChanging(new ElementFaultChangeEventArgs(key, fault));

        protected virtual void OnValueAdded(NotifyDictionaryValueEventArgs<TKey, TResult> e) =>
            ValueAdded?.Invoke(this, e);

        protected void OnValueAdded(TKey key, TResult result) =>
            OnValueAdded(new NotifyDictionaryValueEventArgs<TKey, TResult>(key, result));

        protected virtual void OnValueRemoved(NotifyDictionaryValueEventArgs<TKey, TResult> e) =>
            ValueRemoved?.Invoke(this, e);

        protected void OnValueRemoved(TKey key, TResult result) =>
            OnValueRemoved(new NotifyDictionaryValueEventArgs<TKey, TResult>(key, result));

        protected virtual void OnValueReplaced(NotifyDictionaryValueReplacedEventArgs<TKey, TResult> e) =>
            ValueReplaced?.Invoke(this, e);

        protected void OnValueReplaced(TKey key, TResult oldResult, TResult newResult) =>
            OnValueReplaced(new NotifyDictionaryValueReplacedEventArgs<TKey, TResult>(key, oldResult, newResult));

        protected virtual void OnValueResultChanged(RangeActiveExpressionResultChangeEventArgs<TKey, TResult> e) =>
            ValueResultChanged?.Invoke(this, e);

        protected void OnValueResultChanged(TKey key, TResult result) =>
            OnValueResultChanged(new RangeActiveExpressionResultChangeEventArgs<TKey, TResult>(key, result));

        protected virtual void OnValueResultChanging(RangeActiveExpressionResultChangeEventArgs<TKey, TResult> e) =>
            ValueResultChanging?.Invoke(this, e);

        protected void OnValueResultChanging(TKey key, TResult result) =>
            OnValueResultChanging(new RangeActiveExpressionResultChangeEventArgs<TKey, TResult>(key, result));

        protected virtual void OnValuesAdded(NotifyDictionaryValuesEventArgs<TKey, TResult> e) =>
            ValuesAdded?.Invoke(this, e);

        protected void OnValuesAdded(IReadOnlyList<KeyValuePair<TKey, TResult>> keyValuePairs) =>
            OnValuesAdded(new NotifyDictionaryValuesEventArgs<TKey, TResult>(keyValuePairs));

        protected virtual void OnValuesRemoved(NotifyDictionaryValuesEventArgs<TKey, TResult> e) =>
            ValuesRemoved?.Invoke(this, e);

        protected void OnValuesRemoved(IReadOnlyList<KeyValuePair<TKey, TResult>> keyValuePairs) =>
            OnValuesRemoved(new NotifyDictionaryValuesEventArgs<TKey, TResult>(keyValuePairs));

        TResult RemoveActiveExpression(TKey key)
        {
            OnPropertyChanging(nameof(Count));
            activeExpressionsAccess.EnterWriteLock();
            try
            {
                var activeExpression = activeExpressions[key];
                var result = activeExpression.Value;
                activeExpressions.Remove(key);
                activeExpression.PropertyChanging -= ActiveExpressionPropertyChanging;
                activeExpression.PropertyChanged -= ActiveExpressionPropertyChanged;
                activeExpression.Dispose();
                return result;
            }
            finally
            {
                activeExpressionsAccess.ExitWriteLock();
                OnPropertyChanged(nameof(Count));
            }
        }

        IReadOnlyList<KeyValuePair<TKey, TResult>> RemoveActiveExpressions(IEnumerable<TKey> keys)
        {
            List<KeyValuePair<TKey, TResult>> result = null;
            activeExpressionsAccess.EnterWriteLock();
            try
            {
                if (keys?.Any() ?? false)
                {
                    result = new List<KeyValuePair<TKey, TResult>>();
                    OnPropertyChanging(nameof(Count));
                    foreach (var key in keys)
                    {
                        var activeExpression = activeExpressions[key];
                        result.Add(new KeyValuePair<TKey, TResult>(key, activeExpression.Value));
                        activeExpressions.Remove(key);
                        activeExpression.PropertyChanging -= ActiveExpressionPropertyChanging;
                        activeExpression.PropertyChanged -= ActiveExpressionPropertyChanged;
                        activeExpression.Dispose();
                    }
                }
            }
            finally
            {
                if (result != null)
                    OnPropertyChanged(nameof(Count));
                activeExpressionsAccess.ExitWriteLock();
            }
            return (result ?? Enumerable.Empty<KeyValuePair<TKey, TResult>>()).ToImmutableArray();
        }

        (TResult oldResult, TResult newResult) ReplaceActiveExpression(TKey key, TValue value)
        {
            activeExpressionsAccess.EnterWriteLock();
            try
            {
                var oldActiveExpression = activeExpressions[key];
                var oldResult = oldActiveExpression.Value;
                oldActiveExpression.PropertyChanging -= ActiveExpressionPropertyChanging;
                oldActiveExpression.PropertyChanged -= ActiveExpressionPropertyChanged;
                oldActiveExpression.Dispose();
                var newActiveExpression = ActiveExpression.Create(expression, key, value, Options);
                newActiveExpression.PropertyChanging += ActiveExpressionPropertyChanging;
                newActiveExpression.PropertyChanged += ActiveExpressionPropertyChanged;
                activeExpressions[key] = newActiveExpression;
                return (oldResult, newActiveExpression.Value);
            }
            finally
            {
                activeExpressionsAccess.ExitWriteLock();
            }
        }

        void SourceFaultChanged(object sender, ElementFaultChangeEventArgs e) => ElementFaultChanged?.Invoke(sender, e);

        void SourceFaultChanging(object sender, ElementFaultChangeEventArgs e) => ElementFaultChanging?.Invoke(sender, e);

        void SourceValueAdded(object sender, NotifyDictionaryValueEventArgs<TKey, TValue> e) => OnValueAdded(e.Key, AddActiveExpression(e.Key, e.Value));

        void SourceValueRemoved(object sender, NotifyDictionaryValueEventArgs<TKey, TValue> e) => OnValueRemoved(e.Key, RemoveActiveExpression(e.Key));

        void SourceValueReplaced(object sender, NotifyDictionaryValueReplacedEventArgs<TKey, TValue> e)
        {
            var key = e.Key;
            var (oldResult, newResult) = ReplaceActiveExpression(key, e.NewValue);
            OnValueReplaced(key, oldResult, newResult);
        }

        void SourceValuesAdded(object sender, NotifyDictionaryValuesEventArgs<TKey, TValue> e) => OnValuesAdded(AddActiveExpressions(e.KeyValuePairs));

        void SourceValuesRemoved(object sender, NotifyDictionaryValuesEventArgs<TKey, TValue> e) => OnValuesRemoved(RemoveActiveExpressions(e.KeyValuePairs.Select(kv => kv.Key)));

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
