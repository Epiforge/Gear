using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;

namespace Gear.Components
{
    public class SynchronizedObservableCollection<T> : ObservableCollection<T>
    {
        public SynchronizedObservableCollection(SynchronizationContext owner, bool isSynchronized = true) : base()
        {
            Owner = owner;
            IsSynchronized = isSynchronized;
        }

        public SynchronizedObservableCollection(SynchronizationContext owner, IEnumerable<T> collection, bool isSynchronized = true) : base(collection)
        {
            Owner = owner;
            IsSynchronized = isSynchronized;
        }

        public bool IsSynchronized { get; set; }

        private readonly SynchronizationContext Owner;

        protected void Execute(Action action)
        {
            if (!IsSynchronized || Owner == null || SynchronizationContext.Current == Owner)
                action();
            else if (IsSynchronized)
                Owner.Send(state => action(), null);
        }

        protected TReturn Execute<TReturn>(Func<TReturn> func)
        {
            var result = default(TReturn);
            if (!IsSynchronized || Owner == null || SynchronizationContext.Current == Owner)
                result = func();
            else if (IsSynchronized)
                Owner.Send(state => result = func(), null);
            return result;
        }

        protected override void ClearItems() => Execute(() => base.ClearItems());

        protected override void InsertItem(int index, T item) => Execute(() => base.InsertItem(index, item));

        protected override void MoveItem(int oldIndex, int newIndex) => Execute(() => base.MoveItem(oldIndex, newIndex));

        protected override void RemoveItem(int index) => Execute(() => base.RemoveItem(index));

        protected override void SetItem(int index, T item) => Execute(() => base.SetItem(index, item));
    }
}
