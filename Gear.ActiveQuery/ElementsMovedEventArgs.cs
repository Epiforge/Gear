using System;
using System.Collections.Generic;

namespace Gear.ActiveQuery
{
    public class ElementsMovedEventArgs<T> : EventArgs
    {
        public ElementsMovedEventArgs(IEnumerable<T> elements, int fromIndex, int toIndex, int count)
        {
            Count = count;
            Elements = elements;
            FromIndex = fromIndex;
            ToIndex = toIndex;
        }

        public int Count { get; }
        public IEnumerable<T> Elements { get; }
        public int FromIndex { get; }
        public int ToIndex { get; }
    }
}
