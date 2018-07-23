using System;

namespace Gear.ActiveQuery
{
    public class ActiveOrderingDescriptor<T> where T : class
    {
        public ActiveOrderingDescriptor(Func<T, IComparable> selector, bool descending)
        {
            Selector = selector;
            Descending = descending;
        }

        public bool Descending { get; private set; }
        public Func<T, IComparable> Selector { get; private set; }
    }
}
