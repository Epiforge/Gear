using Gear.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Gear.ActiveQuery
{
    public class ActiveEnumerable<T> : SyncDisposablePropertyChangeNotifier, INotifyCollectionChanged, ICollection, ICollection<T>, IEnumerable, IEnumerable<T>, IList, IList<T>, IReadOnlyCollection<T>, IReadOnlyList<T>
    {
        internal ActiveEnumerable(IList<T> list, Action<bool> onDispose = null)
        {
            if (list is ActiveEnumerable<T> activeEnumerable)
                readOnlyObservableCollection = activeEnumerable.readOnlyObservableCollection;
            else if (list is ReadOnlyObservableCollection<T> readOnlyObservableCollection)
                this.readOnlyObservableCollection = readOnlyObservableCollection;
            else if (list is ObservableCollection<T> observableCollection)
                this.readOnlyObservableCollection = new ReadOnlyObservableCollection<T>(observableCollection);
            else
                this.readOnlyObservableCollection = new ReadOnlyObservableCollection<T>(new ObservableCollection<T>(list));
            ((INotifyCollectionChanged)readOnlyObservableCollection).CollectionChanged += CollectionChangedHandler;
            this.onDispose = onDispose;
        }

        Action<bool> onDispose;
        ReadOnlyObservableCollection<T> readOnlyObservableCollection;

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        int IList.Add(object value) => throw new NotSupportedException();

        void ICollection<T>.Add(T item) => throw new NotSupportedException();

        void IList.Clear() => throw new NotSupportedException();

        void ICollection<T>.Clear() => throw new NotSupportedException();

        void CollectionChangedHandler(object sender, NotifyCollectionChangedEventArgs e) => CollectionChanged?.Invoke(this, e);

        public bool Contains(object value) => ((IList)readOnlyObservableCollection).Contains(value);

        public bool Contains(T item) => readOnlyObservableCollection.Contains(item);

        public void CopyTo(Array array, int index) => ((ICollection)readOnlyObservableCollection).CopyTo(array, index);

        public void CopyTo(T[] array, int arrayIndex) => readOnlyObservableCollection.CopyTo(array, arrayIndex);

        protected override void Dispose(bool disposing)
        {
            onDispose?.Invoke(disposing);
            if (disposing)
                ((INotifyCollectionChanged)readOnlyObservableCollection).CollectionChanged -= CollectionChangedHandler;
        }

        IEnumerator IEnumerable.GetEnumerator() => readOnlyObservableCollection.GetEnumerator();

        public IEnumerator<T> GetEnumerator() => readOnlyObservableCollection.GetEnumerator();

        public int IndexOf(object value) => ((IList)readOnlyObservableCollection).IndexOf(value);

        public int IndexOf(T item) => readOnlyObservableCollection.IndexOf(item);

        void IList.Insert(int index, object value) => throw new NotSupportedException();

        void IList<T>.Insert(int index, T item) => throw new NotSupportedException();

        void IList.Remove(object value) => throw new NotSupportedException();

        bool ICollection<T>.Remove(T item) => throw new NotSupportedException();

        void IList.RemoveAt(int index) => throw new NotSupportedException();

        void IList<T>.RemoveAt(int index) => throw new NotSupportedException();

        public T this[int index] => readOnlyObservableCollection[index];

        object IList.this[int index] { get => readOnlyObservableCollection[index]; set => throw new NotSupportedException(); }

        T IList<T>.this[int index] { get => readOnlyObservableCollection[index]; set => throw new NotSupportedException(); }

        public int Count => readOnlyObservableCollection.Count;

        bool ICollection<T>.IsReadOnly => true;

        bool IList.IsReadOnly => true;

        bool IList.IsFixedSize => true;

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => ((ICollection)readOnlyObservableCollection).SyncRoot;
    }
}
