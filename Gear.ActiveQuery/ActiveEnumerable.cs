using Gear.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;

namespace Gear.ActiveQuery
{
    public class ActiveEnumerable<TElement> : SyncDisposablePropertyChangeNotifier, INotifyCollectionChanged, INotifyElementFaultChanges, IReadOnlyList<TElement>, ISynchronized
    {
        internal ActiveEnumerable(IReadOnlyList<TElement> readOnlyList, INotifyElementFaultChanges faultNotifier = null, Action onDispose = null)
        {
            synchronized = readOnlyList as ISynchronized ?? throw new ArgumentException($"{nameof(readOnlyList)} must implement {nameof(ISynchronized)}", nameof(readOnlyList));
            this.faultNotifier = faultNotifier ?? (readOnlyList as INotifyElementFaultChanges);
            if (this.faultNotifier != null)
            {
                this.faultNotifier.ElementFaultChanged += FaultNotifierElementFaultChanged;
                this.faultNotifier.ElementFaultChanging += FaultNotifierElementFaultChanging;
            }
            if (readOnlyList is ActiveEnumerable<TElement> activeEnumerable)
                this.readOnlyList = activeEnumerable.readOnlyList;
            else
                this.readOnlyList = readOnlyList;
            if (this.readOnlyList is INotifyCollectionChanged collectionNotifier)
                collectionNotifier.CollectionChanged += CollectionChangedHandler;
            this.onDispose = onDispose;
        }

        internal ActiveEnumerable(IReadOnlyList<TElement> readOnlyList, Action onDispose) : this(readOnlyList, null, onDispose)
        {
        }

        readonly INotifyElementFaultChanges faultNotifier;
        readonly Action onDispose;
        readonly IReadOnlyList<TElement> readOnlyList;
        readonly ISynchronized synchronized;

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event EventHandler<ElementFaultChangeEventArgs> ElementFaultChanged;
        public event EventHandler<ElementFaultChangeEventArgs> ElementFaultChanging;

        void CollectionChangedHandler(object sender, NotifyCollectionChangedEventArgs e) => CollectionChanged?.Invoke(this, e);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                onDispose?.Invoke();
                if (faultNotifier != null)
                {
                    faultNotifier.ElementFaultChanged -= FaultNotifierElementFaultChanged;
                    faultNotifier.ElementFaultChanging -= FaultNotifierElementFaultChanging;
                }
                if (readOnlyList is INotifyCollectionChanged collectionNotifier)
                    collectionNotifier.CollectionChanged -= CollectionChangedHandler;
            }
        }

        void FaultNotifierElementFaultChanged(object sender, ElementFaultChangeEventArgs e) => ElementFaultChanged?.Invoke(this, e);

        void FaultNotifierElementFaultChanging(object sender, ElementFaultChangeEventArgs e) => ElementFaultChanging?.Invoke(this, e);

        IEnumerator IEnumerable.GetEnumerator() => readOnlyList.GetEnumerator();

        public IEnumerator<TElement> GetEnumerator() => readOnlyList.GetEnumerator();

        public IReadOnlyList<(object element, Exception fault)> GetElementFaults() => faultNotifier?.GetElementFaults() ?? Enumerable.Empty<(object element, Exception fault)>().ToImmutableArray();

        public TElement this[int index] => readOnlyList[index];

        public int Count => readOnlyList.Count;

        public SynchronizationContext SynchronizationContext => synchronized.SynchronizationContext;
    }
}
