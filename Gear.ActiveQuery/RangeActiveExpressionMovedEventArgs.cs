using System;
using System.Collections.Generic;

namespace Gear.ActiveQuery
{
    public class RangeActiveExpressionMovedEventArgs<TElement, TResult> : EventArgs
    {
        public RangeActiveExpressionMovedEventArgs(IReadOnlyList<(TElement element, TResult result)> elementResults, int fromIndex, int toIndex)
        {
            ElementResults = elementResults;
            FromIndex = fromIndex;
            ToIndex = toIndex;
        }

        public IReadOnlyList<(TElement element, TResult result)> ElementResults { get; }
        public int FromIndex { get; }
        public int ToIndex { get; }
    }
}
