using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Gear.Components
{
    public class SynchronizedObservableCollection<T> : ObservableCollection<T>, ISynchronizable
    {
        public SynchronizedObservableCollection(SynchronizationContext synchronizationContext, bool isSynchronized = true) : base()
        {
            SynchronizationContext = synchronizationContext;
            this.isSynchronized = isSynchronized;
        }

        public SynchronizedObservableCollection(SynchronizationContext synchronizationContext, IEnumerable<T> collection, bool isSynchronized = true) : base(collection)
        {
            SynchronizationContext = synchronizationContext;
            this.isSynchronized = isSynchronized;
        }

        bool isSynchronized;

        public Task AddAsync(T item) => this.ExecuteAsync(() => Add(item));

        public Task ClearAsync() => this.ExecuteAsync(() => Clear());

        protected override void ClearItems() => this.Execute(() => base.ClearItems());

        public Task<bool> ContainsAsync(T item) => this.ExecuteAsync(() => Contains(item));

        public Task CopyToAsync(T[] array, int index) => this.ExecuteAsync(() => CopyTo(array, index));

        public T GetAndRemoveAt(int index) => this.Execute(() =>
        {
            var item = Items[index];
            RemoveAt(index);
            return item;
        });

        public Task<T> GetAndRemoveAtAsync(int index) => this.ExecuteAsync(() => GetAndRemoveAt(index));

        public Task<T> GetItemAsync(int index) => this.ExecuteAsync(() => this[index]);

        public Task<int> IndexOfAsync(T item) => this.ExecuteAsync(() => IndexOf(item));

        public Task InsertAsync(int index, T item) => this.ExecuteAsync(() => Insert(index, item));

        protected override void InsertItem(int index, T item) => this.Execute(() => base.InsertItem(index, item));

        public Task MoveAsync(int oldIndex, int newIndex) => this.ExecuteAsync(() => Move(oldIndex, newIndex));

        protected override void MoveItem(int oldIndex, int newIndex) => this.Execute(() => base.MoveItem(oldIndex, newIndex));

        public Task<bool> RemoveAsync(T item) => this.ExecuteAsync(() => Remove(item));

        public Task RemoveAtAsync(int index) => this.ExecuteAsync(() => RemoveAt(index));

        protected override void RemoveItem(int index) => this.Execute(() => base.RemoveItem(index));

        public T Replace(int index, T item) => this.Execute(() =>
        {
            var replacedItem = Items[index];
            Items[index] = item;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item, replacedItem, index));
            return replacedItem;
        });

        public Task<T> ReplaceAsync(int index, T item) => this.ExecuteAsync(() => Replace(index, item));

        protected override void SetItem(int index, T item) => this.Execute(() => base.SetItem(index, item));

        public Task SetItemAsync(int index, T item) => this.ExecuteAsync(() => this[index] = item);

        public Task<int> CountAsync => this.ExecuteAsync(() => Count);

        public bool IsSynchronized
        {
            get => isSynchronized;
            set
            {
                if (isSynchronized != value)
                {
                    isSynchronized = value;
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsSynchronized)));
                }
            }
        }

        public SynchronizationContext SynchronizationContext { get; }
    }
}
