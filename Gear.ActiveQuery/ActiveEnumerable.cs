using Gear.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Gear.ActiveQuery
{
    public class ActiveEnumerable<T> : SyncDisposablePropertyChangeNotifier, INotifyCollectionChanged, IReadOnlyList<T>
    {
        internal ActiveEnumerable(IReadOnlyList<T> readOnlyList, Action<bool> onDispose = null)
        {
            if (readOnlyList is ActiveEnumerable<T> activeEnumerable)
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

        readonly IReadOnlyList<T> readOnlyList;
        readonly Action<bool> onDispose;

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        void CollectionChangedHandler(object sender, NotifyCollectionChangedEventArgs e) => CollectionChanged?.Invoke(this, e);

        protected override void Dispose(bool disposing)
        {
            onDispose?.Invoke(disposing);
            if (disposing)
            {
                if (readOnlyList is INotifyCollectionChanged collectionNotifier)
                    collectionNotifier.CollectionChanged -= CollectionChangedHandler;
                if (readOnlyList is INotifyPropertyChanged propertyChangedNotifier)
                    propertyChangedNotifier.PropertyChanged -= PropertyChangedHandler;
                if (readOnlyList is INotifyPropertyChanging propertyChangingNotifier)
                    propertyChangingNotifier.PropertyChanging -= PropertyChangingHandler;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => readOnlyList.GetEnumerator();

        public IEnumerator<T> GetEnumerator() => readOnlyList.GetEnumerator();

        void PropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Count))
                OnPropertyChanged(e);
        }

        void PropertyChangingHandler(object sender, PropertyChangingEventArgs e)
        {
            if (e.PropertyName == nameof(Count))
                OnPropertyChanging(e);
        }

        public T this[int index] => readOnlyList[index];

        public int Count => readOnlyList.Count;
    }
}
