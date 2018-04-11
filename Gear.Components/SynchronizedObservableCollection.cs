using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;

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

        protected void Execute(Action action)
        {
            if (!IsSynchronized || SynchronizationContext == null || SynchronizationContext.Current == SynchronizationContext)
                action();
            else if (IsSynchronized)
                SynchronizationContext.Send(state => action(), null);
        }

        protected TReturn Execute<TReturn>(Func<TReturn> func)
        {
            var result = default(TReturn);
            if (!IsSynchronized || SynchronizationContext == null || SynchronizationContext.Current == SynchronizationContext)
                result = func();
            else if (IsSynchronized)
                SynchronizationContext.Send(state => result = func(), null);
            return result;
        }

        protected override void ClearItems() => Execute(() => base.ClearItems());

        protected override void InsertItem(int index, T item) => Execute(() => base.InsertItem(index, item));

        protected override void MoveItem(int oldIndex, int newIndex) => Execute(() => base.MoveItem(oldIndex, newIndex));

        protected override void RemoveItem(int index) => Execute(() => base.RemoveItem(index));

        protected override void SetItem(int index, T item) => Execute(() => base.SetItem(index, item));

        public bool IsSynchronized { get; set; }

        public SynchronizationContext SynchronizationContext { get; }
    }
}
