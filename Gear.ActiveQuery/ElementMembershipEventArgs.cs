using System;
using System.Collections.Generic;

namespace Gear.ActiveQuery
{
    public class ElementMembershipEventArgs<T> : EventArgs where T : class
    {
        public ElementMembershipEventArgs(IEnumerable<T> elements, int index, int count)
        {
            Count = count;
            Elements = elements;
            Index = index;
        }

        public int Count { get; }
        public IEnumerable<T> Elements { get; }
        public int Index { get; }
    }
}
