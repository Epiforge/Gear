using System;
using System.Collections.Generic;
using System.Text;

namespace Gear.ActiveQuery
{
    public class ActiveOrderingDescriptor<T> where T : class
    {
        public bool Descending { get; private set; }
        public Func<T, IComparable> Selector { get; private set; }

        public ActiveOrderingDescriptor(Func<T, IComparable> selector, bool descending)
        {
            Selector = selector;
            Descending = descending;
        }
    }
}
