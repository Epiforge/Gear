using Gear.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;

namespace Gear.ActiveQuery
{
    public class ActiveEnumerable<TElement> : SyncDisposablePropertyChangeNotifier, INotifyCollectionChanged, INotifyElementFaultChanges, IReadOnlyList<TElement>, ISynchronizable
    {
        internal ActiveEnumerable(IReadOnlyList<TElement> readOnlyList, INotifyElementFaultChanges faultNotifier = null, Action onDispose = null)
        {
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
            if (this.readOnlyList is INotifyPropertyChanged propertyChangedNotifier)
                propertyChangedNotifier.PropertyChanged += PropertyChangedHandler;
            if (this.readOnlyList is INotifyPropertyChanging propertyChangingNotifier)
                propertyChangingNotifier.PropertyChanging += PropertyChangingHandler;
            this.onDispose = onDispose;
        }

        internal ActiveEnumerable(IReadOnlyList<TElement> readOnlyList, Action onDispose) : this(readOnlyList, null, onDispose)
        {
        }

        readonly INotifyElementFaultChanges faultNotifier;
        readonly Action onDispose;
        readonly IReadOnlyList<TElement> readOnlyList;

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
                if (readOnlyList is INotifyPropertyChanged propertyChangedNotifier)
                    propertyChangedNotifier.PropertyChanged -= PropertyChangedHandler;
                if (readOnlyList is INotifyPropertyChanging propertyChangingNotifier)
                    propertyChangingNotifier.PropertyChanging -= PropertyChangingHandler;
            }
        }

        void FaultNotifierElementFaultChanged(object sender, ElementFaultChangeEventArgs e) => ElementFaultChanged?.Invoke(this, e);

        void FaultNotifierElementFaultChanging(object sender, ElementFaultChangeEventArgs e) => ElementFaultChanging?.Invoke(this, e);

        IEnumerator IEnumerable.GetEnumerator() => readOnlyList.GetEnumerator();

        public IEnumerator<TElement> GetEnumerator() => readOnlyList.GetEnumerator();

        public IReadOnlyList<(object element, Exception fault)> GetElementFaults() => faultNotifier?.GetElementFaults();

        void PropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Count) || e.PropertyName == nameof(ISynchronizable.IsSynchronized))
                OnPropertyChanged(e);
        }

        void PropertyChangingHandler(object sender, PropertyChangingEventArgs e)
        {
            if (e.PropertyName == nameof(Count) || e.PropertyName == nameof(ISynchronizable.IsSynchronized))
                OnPropertyChanging(e);
        }

        public TElement this[int index] => readOnlyList[index];

        public int Count => readOnlyList.Count;

        public bool IsSynchronized => (readOnlyList as ISynchronizable)?.IsSynchronized ?? false;

        public SynchronizationContext SynchronizationContext => (readOnlyList as ISynchronizable)?.SynchronizationContext;
    }
}
