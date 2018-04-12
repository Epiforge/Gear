using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Gear.Components
{
    public class SynchronizedObservableCollection<T> : ObservableCollection<T>, IsSynchronizable
    {
        public SynchronizedObservableCollection(SynchronizationContext owner, bool isSynchronized = true) : base()
        {
            SynchronizationContext = owner;
            IsSynchronized = isSynchronized;
        }

        public SynchronizedObservableCollection(SynchronizationContext synchronizationContext, IEnumerable<T> collection, bool isSynchronized = true) : base(collection)
        {
            SynchronizationContext = synchronizationContext;
            IsSynchronized = isSynchronized;
        }

        public Task AddAsync(T item) => ExecuteAsync(() => Add(item));

        public Task ClearAsync() => ExecuteAsync(() => Clear());

        protected override void ClearItems() => Execute(() => base.ClearItems());

        public Task<bool> ContainsAsync(T item) => ExecuteAsync(() => Contains(item));

        public Task CopyToAsync(T[] array, int index) => ExecuteAsync(() => CopyTo(array, index));

        protected void Execute(Action action)
        {
            if (!IsSynchronized || SynchronizationContext == null || SynchronizationContext.Current == SynchronizationContext)
            {
                action();
                return;
            }
            ExceptionDispatchInfo edi = null;
            SynchronizationContext.Send(state =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    edi = ExceptionDispatchInfo.Capture(ex);
                }
            }, null);
            edi?.Throw();
        }

        protected TReturn Execute<TReturn>(Func<TReturn> func)
        {
            if (!IsSynchronized || SynchronizationContext == null || SynchronizationContext.Current == SynchronizationContext)
                return func();
            TReturn result = default;
            ExceptionDispatchInfo edi = null;
            SynchronizationContext.Send(state =>
            {
                try
                {
                    result = func();
                }
                catch (Exception ex)
                {
                    edi = ExceptionDispatchInfo.Capture(ex);
                }
            }, null);
            edi?.Throw();
            return result;
        }

        protected Task ExecuteAsync(Action action)
        {
            if (!IsSynchronized || SynchronizationContext == null || SynchronizationContext.Current == SynchronizationContext)
            {
                action();
                return Task.CompletedTask;
            }
            var completion = new TaskCompletionSource<object>();
            SynchronizationContext.Post(state =>
            {
                try
                {
                    action();
                    completion.SetResult(null);
                }
                catch (Exception ex)
                {
                    completion.SetException(ex);
                }
            }, null);
            return completion.Task;
        }

        protected Task<TResult> ExecuteAsync<TResult>(Func<TResult> func)
        {
            if (!IsSynchronized || SynchronizationContext == null || SynchronizationContext.Current == SynchronizationContext)
                return Task.FromResult(func());
            var completion = new TaskCompletionSource<TResult>();
            SynchronizationContext.Post(state =>
            {
                try
                {
                    completion.SetResult(func());
                }
                catch (Exception ex)
                {
                    completion.SetException(ex);
                }
            }, null);
            return completion.Task;
        }

        public Task<T> GetItemAsync(int index) => ExecuteAsync(() => this[index]);

        public Task<int> IndexOfAsync(T item) => ExecuteAsync(() => IndexOf(item));

        public Task InsertAsync(int index, T item) => ExecuteAsync(() => Insert(index, item));

        protected override void InsertItem(int index, T item) => Execute(() => base.InsertItem(index, item));

        public Task MoveAsync(int oldIndex, int newIndex) => ExecuteAsync(() => Move(oldIndex, newIndex));

        protected override void MoveItem(int oldIndex, int newIndex) => Execute(() => base.MoveItem(oldIndex, newIndex));

        public Task<bool> RemoveAsync(T item) => ExecuteAsync(() => Remove(item));

        public Task RemoveAtAsync(int index) => ExecuteAsync(() => RemoveAt(index));

        protected override void RemoveItem(int index) => Execute(() => base.RemoveItem(index));

        protected override void SetItem(int index, T item) => Execute(() => base.SetItem(index, item));

        public Task SetItemAsync(int index, T item) => ExecuteAsync(() => this[index] = item);

        public Task<int> CountAsync => ExecuteAsync(() => Count);

        public bool IsSynchronized { get; set; }

        public SynchronizationContext SynchronizationContext { get; }
    }
}
