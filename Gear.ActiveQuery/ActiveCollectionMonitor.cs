using Gear.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Gear.ActiveQuery
{
    public class ActiveCollectionMonitor : SyncDisposable
    {
        static readonly Dictionary<IList, ActiveCollectionMonitor> monitors = new Dictionary<IList, ActiveCollectionMonitor>();
        static readonly object instanceManagementLock = new object();

        public static ActiveCollectionMonitor Monitor(IList collection)
        {
            lock (instanceManagementLock)
            {
                if (!monitors.TryGetValue(collection, out var monitor))
                {
                    monitor = new ActiveCollectionMonitor(collection);
                    monitors.Add(collection, monitor);
                }
                ++monitor.instances;
                return monitor;
            }
        }

        ActiveCollectionMonitor(IList collection)
        {
            this.collection = collection;
            elementList = collection.Cast<object>().ToList();
            instances = 0;
            if (this.collection is INotifyCollectionChanged)
                ((INotifyCollectionChanged)this.collection).CollectionChanged += CollectionChangedHandler;
        }

        IList collection;
        List<object> elementList;
        int instances;

        public event EventHandler<ElementMembershipEventArgs<object>> ElementsAdded;
        public event EventHandler<ElementsMovedEventArgs<object>> ElementsMoved;
        public event EventHandler<ElementMembershipEventArgs<object>> ElementsRemoved;

        void CollectionChangedHandler(object sender, NotifyCollectionChangedEventArgs e)
        {
            var oldItems = e.OldItems != null ? (IEnumerable<object>)e.OldItems : new object[0];
            var oldItemsCount = e.OldItems != null ? e.OldItems.Count : 0;
            var newItems = e.NewItems != null ? (IEnumerable<object>)e.NewItems : new object[0];
            var newItemsCount = e.NewItems != null ? e.NewItems.Count : 0;
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                var oldElementList = elementList;
                elementList = collection.Cast<object>().ToList();
                if (oldElementList.Count > 0)
                    OnElementsRemoved(oldElementList, 0, oldElementList.Count);
                if (elementList.Count > 0)
                    OnElementsAdded(elementList, 0, elementList.Count);
            }
            else if (e.Action == NotifyCollectionChangedAction.Replace)
            {
                elementList.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
                elementList.InsertRange(e.NewStartingIndex, newItems);
                OnElementsRemoved(oldItems, e.OldStartingIndex, oldItemsCount);
                OnElementsAdded(newItems, e.NewStartingIndex, newItemsCount);
            }
            else if (e.Action == NotifyCollectionChangedAction.Move && oldItems.SequenceEqual(newItems))
            {
                elementList.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
                elementList.InsertRange(e.NewStartingIndex, newItems);
                OnElementsMoved(newItems, e.OldStartingIndex, e.NewStartingIndex, newItemsCount);
            }
            else
            {
                if (e.OldItems != null && e.OldStartingIndex >= 0)
                {
                    elementList.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
                    OnElementsRemoved(oldItems, e.OldStartingIndex, oldItemsCount);
                }
                if (e.NewItems != null && e.NewStartingIndex >= 0)
                {
                    elementList.InsertRange(e.NewStartingIndex, newItems);
                    OnElementsAdded(newItems, e.NewStartingIndex, oldItemsCount);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            lock (instanceManagementLock)
            {
                if (--instances > 0)
                    return;
                monitors.Remove(collection);
                if (disposing && collection is INotifyCollectionChanged notifyingCollection)
                    notifyingCollection.CollectionChanged -= CollectionChangedHandler;
            }
        }

        protected virtual void OnElementsAdded(ElementMembershipEventArgs<object> e) => ElementsAdded?.Invoke(this, e);

        void OnElementsAdded(IEnumerable<object> elements, int index, int count) => OnElementsAdded(new ElementMembershipEventArgs<object>(elements, index, count));

        protected virtual void OnElementsMoved(ElementsMovedEventArgs<object> e) => ElementsMoved?.Invoke(this, e);

        void OnElementsMoved(IEnumerable<object> elements, int fromIndex, int toIndex, int count) => OnElementsMoved(new ElementsMovedEventArgs<object>(elements, fromIndex, toIndex, count));

        protected virtual void OnElementsRemoved(ElementMembershipEventArgs<object> e) => ElementsRemoved?.Invoke(this, e);

        void OnElementsRemoved(IEnumerable<object> elements, int index, int count) => OnElementsRemoved(new ElementMembershipEventArgs<object>(elements, index, count));
    }

    public class ActiveCollectionMonitor<T> : SyncDisposable where T : class
    {
        static Dictionary<(IList<T> collection, string relevantProperties), object> monitors = new Dictionary<(IList<T> collection, string relevantProperties), object>();
        static object instanceManagementLock = new object();

        public static ActiveCollectionMonitor<T> Monitor(IList<T> collection, params string[] relevantPropertyNames)
        {
            string relevantProperties;
            if (relevantPropertyNames?.Any() ?? false)
                relevantProperties = string.Join("|", relevantPropertyNames.OrderBy(s => s));
            else
                relevantProperties = null;
            var key = (collection, relevantProperties);
            lock (instanceManagementLock)
            {
                if (!monitors.TryGetValue(key, out var obj))
                {
                    obj = new ActiveCollectionMonitor<T>(collection, relevantPropertyNames);
                    monitors.Add(key, obj);
                }
                var monitor = obj as ActiveCollectionMonitor<T>;
                ++monitor.instances;
                return monitor;
            }
        }

        IList<T> collection;
        List<T> elementList;
        int instances;
        List<string> relevantPropertyNames;

        public event EventHandler<ElementPropertyChangeEventArgs<T>> ElementPropertyChanged;
        public event EventHandler<ElementPropertyChangeEventArgs<T>> ElementPropertyChanging;
        public event EventHandler<ElementMembershipEventArgs<T>> ElementsAdded;
        public event EventHandler<ElementsMovedEventArgs<T>> ElementsMoved;
        public event EventHandler<ElementMembershipEventArgs<T>> ElementsRemoved;

        ActiveCollectionMonitor(IList<T> collection, params string[] relevantPropertyNames)
        {
            var elementTypeInfo = typeof(T).GetTypeInfo();
            ElementsNotifyChanging = typeof(INotifyPropertyChanging).GetTypeInfo().IsAssignableFrom(elementTypeInfo);
            ElementsNotifyChanged = typeof(INotifyPropertyChanged).GetTypeInfo().IsAssignableFrom(elementTypeInfo);
            this.relevantPropertyNames = relevantPropertyNames.ToList();

            this.collection = collection;
            elementList = this.collection.ToList();
            if (this.collection is INotifyCollectionChanged)
                ((INotifyCollectionChanged)this.collection).CollectionChanged += CollectionChangedHandler;
            AttachToElements(elementList);
        }

        void AttachToElements(IEnumerable<T> elements)
        {
            if (ElementsNotifyChanging && ElementsNotifyChanged)
                foreach (var element in elements)
                {
                    ((INotifyPropertyChanging)element).PropertyChanging += ElementPropertyChangingHandler;
                    ((INotifyPropertyChanged)element).PropertyChanged += ElementPropertyChangedHandler;
                }
            else if (ElementsNotifyChanging)
                foreach (var element in elements)
                    ((INotifyPropertyChanging)element).PropertyChanging += ElementPropertyChangingHandler;
            else if (ElementsNotifyChanged)
                foreach (var element in elements)
                    ((INotifyPropertyChanged)element).PropertyChanged += ElementPropertyChangedHandler;
        }

        void CollectionChangedHandler(object sender, NotifyCollectionChangedEventArgs e)
        {
            var oldItems = e.OldItems != null ? e.OldItems.Cast<T>() : new T[0];
            var oldItemsCount = e.OldItems != null ? e.OldItems.Count : 0;
            var newItems = e.NewItems != null ? e.NewItems.Cast<T>() : new T[0];
            var newItemsCount = e.NewItems != null ? e.NewItems.Count : 0;
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                DetachFromElements(elementList);
                var oldElementList = elementList;
                elementList = collection.ToList();
                AttachToElements(elementList);
                if (oldElementList.Count > 0)
                    OnElementsRemoved(oldElementList, 0, oldElementList.Count);
                if (elementList.Count > 0)
                    OnElementsAdded(elementList, 0, elementList.Count);
            }
            else if (e.Action == NotifyCollectionChangedAction.Replace)
            {
                DetachFromElements(oldItems);
                elementList.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
                elementList.InsertRange(e.NewStartingIndex, newItems);
                AttachToElements(newItems);
                OnElementsRemoved(oldItems, e.OldStartingIndex, oldItemsCount);
                OnElementsAdded(newItems, e.NewStartingIndex, newItemsCount);
            }
            else if (e.Action == NotifyCollectionChangedAction.Move && oldItems.SequenceEqual(newItems))
            {
                elementList.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
                elementList.InsertRange(e.NewStartingIndex, newItems);
                OnElementsMoved(newItems, e.OldStartingIndex, e.NewStartingIndex, newItemsCount);
            }
            else
            {
                if (e.OldItems != null && e.OldStartingIndex >= 0)
                {
                    DetachFromElements(oldItems);
                    elementList.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
                    OnElementsRemoved(oldItems, e.OldStartingIndex, oldItemsCount);
                }
                if (e.NewItems != null && e.NewStartingIndex >= 0)
                {
                    elementList.InsertRange(e.NewStartingIndex, newItems);
                    AttachToElements(newItems);
                    OnElementsAdded(newItems, e.NewStartingIndex, newItemsCount);
                }
            }
        }

        void DetachFromElements(IEnumerable<T> elements)
        {
            if (ElementsNotifyChanging && ElementsNotifyChanged)
                foreach (var element in elements)
                {
                    ((INotifyPropertyChanging)element).PropertyChanging -= ElementPropertyChangingHandler;
                    ((INotifyPropertyChanged)element).PropertyChanged -= ElementPropertyChangedHandler;
                }
            else if (ElementsNotifyChanging)
                foreach (var element in elements)
                    ((INotifyPropertyChanging)element).PropertyChanging -= ElementPropertyChangingHandler;
            else if (ElementsNotifyChanged)
                foreach (var element in elements)
                    ((INotifyPropertyChanged)element).PropertyChanged -= ElementPropertyChangedHandler;
        }

        protected override void Dispose(bool disposing)
        {
            lock (instanceManagementLock)
            {
                if (--instances > 0)
                    return;
                string relevantProperties;
                if (relevantPropertyNames?.Any() ?? false)
                    relevantProperties = string.Join("|", relevantPropertyNames.OrderBy(s => s));
                else
                    relevantProperties = null;
                monitors.Remove((collection, relevantProperties));
                if (disposing)
                {
                    if (collection is INotifyCollectionChanged notifyCollection)
                        notifyCollection.CollectionChanged -= CollectionChangedHandler;
                    DetachFromElements(elementList);
                }
            }
        }

        void ElementPropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            if (sender is T element)
                OnElementPropertyChanged(element, e.PropertyName);
        }

        void ElementPropertyChangingHandler(object sender, PropertyChangingEventArgs e)
        {
            if (sender is T element)
                OnElementPropertyChanged(element, e.PropertyName);
        }

        protected virtual void OnElementPropertyChanged(ElementPropertyChangeEventArgs<T> e) => ElementPropertyChanged?.Invoke(this, e);

        void OnElementPropertyChanged(T element, string propertyName)
        {
            if (relevantPropertyNames.Count == 0 || relevantPropertyNames.Contains(propertyName))
                OnElementPropertyChanged(new ElementPropertyChangeEventArgs<T>(element, propertyName));
        }

        protected virtual void OnElementPropertyChanging(ElementPropertyChangeEventArgs<T> e) => ElementPropertyChanging?.Invoke(this, e);

        void OnElementPropertyChanging(T element, string propertyName)
        {
            if (relevantPropertyNames.Count == 0 || relevantPropertyNames.Contains(propertyName))
                OnElementPropertyChanging(new ElementPropertyChangeEventArgs<T>(element, propertyName));
        }

        protected virtual void OnElementsAdded(ElementMembershipEventArgs<T> e) => ElementsAdded?.Invoke(this, e);

        void OnElementsAdded(IEnumerable<T> elements, int index, int count) => OnElementsAdded(new ElementMembershipEventArgs<T>(elements, index, count));

        protected virtual void OnElementsMoved(ElementsMovedEventArgs<T> e) => ElementsMoved?.Invoke(this, e);

        void OnElementsMoved(IEnumerable<T> elements, int fromIndex, int toIndex, int count) => OnElementsMoved(new ElementsMovedEventArgs<T>(elements, fromIndex, toIndex, count));

        protected virtual void OnElementsRemoved(ElementMembershipEventArgs<T> e) => ElementsRemoved?.Invoke(this, e);

        void OnElementsRemoved(IEnumerable<T> elements, int index, int count) => OnElementsRemoved(new ElementMembershipEventArgs<T>(elements, index, count));

        public bool ElementsNotifyChanged { get; }

        public bool ElementsNotifyChanging { get; }
    }
}
