using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;

namespace Gear.Components
{
    public class SynchronizedRangeObservableCollection<T> : SynchronizedObservableCollection<T>
    {
        public SynchronizedRangeObservableCollection(SynchronizationContext owner, bool isSynchronized = true) : base(owner, isSynchronized)
        {
        }

        public SynchronizedRangeObservableCollection(SynchronizationContext owner, IEnumerable<T> collection, bool isSynchronized = true) : base(owner, collection, isSynchronized)
        {
        }

        public void AddRange(IEnumerable<T> items) => InsertRange(Items.Count, items);

        public void AddRange(IList<T> items) => AddRange((IEnumerable<T>)items);

        public void InsertRange(int index, IEnumerable<T> items) => Execute(() =>
        {
            var originalIndex = index;
            --index;
            var list = new List<T>();
            foreach (T item in items)
            {
                Items.Insert(++index, item);
                list.Add(item);
            }
            if (list.Count > 0)
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, list, originalIndex));
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            }
        });

        public void InsertRange(int index, IList<T> items) => InsertRange(index, (IEnumerable<T>)items);

        public void MoveRange(int oldStartIndex, int newStartIndex, int count) => Execute(() =>
        {
            if (oldStartIndex != newStartIndex && count > 0)
            {
                var movedItems = new List<T>();
                for (var i = 0; i < count; ++i)
                {
                    var item = Items[oldStartIndex];
                    Items.RemoveAt(oldStartIndex);
                    movedItems.Add(item);
                }
                var insertionIndex = newStartIndex;
                if (newStartIndex > oldStartIndex)
                    insertionIndex -= (count + 1);
                foreach (var item in movedItems)
                    Items.Insert(++insertionIndex, item);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, movedItems, newStartIndex, oldStartIndex));
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            }
        });

        public int RemoveAll(Func<T, bool> predicate) => Execute(() =>
        {
            var removed = 0;
            for (var i = 0; i < Items.Count;)
            {
                var item = Items[i];
                if (predicate(item))
                {
                    Items.RemoveAt(i);
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, i));
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
                    ++removed;
                }
                else
                    ++i;
            }
            return removed;
        });

        public void RemoveRange(IEnumerable<T> items) => Execute(() =>
        {
            foreach (T item in items)
            {
                var index = Items.IndexOf(item);
                if (index >= 0)
                {
                    Items.RemoveAt(index);
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
                }
            }
        });

        public void RemoveRange(IList<T> items) => RemoveRange((IEnumerable<T>)items);

        public void RemoveRange(int index, int count) => Execute(() =>
        {
            if (count > 0)
            {
                var removedItems = new T[count];
                for (var removalIndex = 0; removalIndex < count; ++removalIndex)
                {
                    removedItems[removalIndex] = Items[index];
                    Items.RemoveAt(index);
                }
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItems, index));
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            }
        });

        public void ReplaceAll(IEnumerable<T> collection) => Execute(() =>
        {
            var oldItems = new T[Items.Count];
            Items.CopyTo(oldItems, 0);
            Items.Clear();
            var list = new List<T>();
            foreach (T element in collection)
            {
                Items.Add(element);
                list.Add(element);
            }
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, list, oldItems, 0));
            if (oldItems.Length != list.Count)
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        });

        public void ReplaceAll(IList<T> list) => ReplaceAll((IEnumerable<T>)list);

        public void ReplaceRange(int index, int count, IEnumerable<T> collection) => Execute(() =>
        {
            var originalIndex = index;
            var oldItems = new T[count];
            for (var i = 0; i < count; ++i)
            {
                oldItems[i] = Items[index];
                Items.RemoveAt(index);
            }
            var list = new List<T>();
            index -= 1;
            foreach (T element in collection)
            {
                Items.Insert(++index, element);
                list.Add(element);
            }
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, list, oldItems, originalIndex));
            if (oldItems.Length != list.Count)
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        });

        public void ReplaceRange(int index, int count, IList<T> list) => ReplaceRange(index, count, (IEnumerable<T>)list);

        public void Reset(IEnumerable<T> newCollection) => Execute(() =>
        {
            var previousCount = Items.Count;
            Items.Clear();
            foreach (T element in newCollection)
                Items.Add(element);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            if (previousCount != Items.Count)
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        });
    }
}
