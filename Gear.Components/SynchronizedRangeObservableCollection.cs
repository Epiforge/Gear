using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

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

        public Task AddRangeAsync(IEnumerable<T> items) => InsertRangeAsync(Items.Count, items);

        public Task AddRangeAsync(IList<T> items) => AddRangeAsync((IEnumerable<T>)items);

        public IReadOnlyList<T> GetRange(int index, int count) => this.Execute(() =>
        {
            var result = new List<T>();
            for (int i = index, ii = index + count; i < ii; ++i)
                result.Add(this[i]);
            return result;
        });

        public Task<IReadOnlyList<T>> GetRangeAsync(int index, int count) => this.ExecuteAsync(() => GetRange(index, count));

        public void InsertRange(int index, IEnumerable<T> items) => this.Execute(() =>
        {
            var originalIndex = index;
            --index;
            var list = new List<T>();
            foreach (var item in items)
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

        public Task InsertRangeAsync(int index, IEnumerable<T> items) => this.ExecuteAsync(() => InsertRange(index, items));

        public Task InsertRangeAsync(int index, IList<T> items) => InsertRangeAsync(index, (IEnumerable<T>)items);

        public void MoveRange(int oldStartIndex, int newStartIndex, int count) => this.Execute(() =>
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

        public Task MoveRangeAsync(int oldStartIndex, int newStartIndex, int count) => this.ExecuteAsync(() => MoveRange(oldStartIndex, newStartIndex, count));

        public int RemoveAll(Func<T, bool> predicate) => this.Execute(() =>
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

        public Task<int> RemoveAllAsync(Func<T, bool> predicate) => this.ExecuteAsync(() => RemoveAll(predicate));

        public void RemoveRange(IEnumerable<T> items) => this.Execute(() =>
        {
            foreach (var item in items)
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

        public void RemoveRange(int index, int count) => this.Execute(() =>
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

        public Task RemoveRangeAsync(IEnumerable<T> items) => this.ExecuteAsync(() => RemoveRange(items));

        public Task RemoveRangeAsync(IList<T> items) => this.ExecuteAsync(() => RemoveRange(items));

        public Task RemoveRangeAsync(int index, int count) => this.ExecuteAsync(() => RemoveRange(index, count));

        public void ReplaceAll(IEnumerable<T> collection) => this.Execute(() =>
        {
            var oldItems = new T[Items.Count];
            Items.CopyTo(oldItems, 0);
            Items.Clear();
            var list = new List<T>();
            foreach (var element in collection)
            {
                Items.Add(element);
                list.Add(element);
            }
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, list, oldItems, 0));
            if (oldItems.Length != list.Count)
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        });

        public void ReplaceAll(IList<T> list) => ReplaceAll((IEnumerable<T>)list);

        public Task ReplaceAllAsync(IEnumerable<T> collection) => this.ExecuteAsync(() => ReplaceAll(collection));

        public Task ReplaceAllAsync(IList<T> list) => this.ExecuteAsync(() => ReplaceAll(list));

        public IReadOnlyList<T> ReplaceRange(int index, int count, IEnumerable<T> collection = null) => this.Execute(() =>
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
            if (collection != null)
                foreach (var element in collection)
                {
                    Items.Insert(++index, element);
                    list.Add(element);
                }
            if (list.Count > 0)
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, list, oldItems, originalIndex));
            else
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldItems, originalIndex));
            if (oldItems.Length != list.Count)
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            return oldItems;
        });

        public IReadOnlyList<T> ReplaceRange(int index, int count, IList<T> list) => ReplaceRange(index, count, (IEnumerable<T>)list);

        public Task ReplaceRangeAsync(int index, int count, IEnumerable<T> collection) => this.ExecuteAsync(() => ReplaceRange(index, count, collection));

        public Task ReplaceRangeAsync(int index, int count, IList<T> list) => this.ExecuteAsync(() => ReplaceRange(index, count, list));

        public void Reset(IEnumerable<T> newCollection) => this.Execute(() =>
        {
            var previousCount = Items.Count;
            Items.Clear();
            foreach (var element in newCollection)
                Items.Add(element);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            if (previousCount != Items.Count)
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        });

        public Task ResetAsync(IEnumerable<T> newCollection) => this.ExecuteAsync(() => Reset(newCollection));
    }
}
