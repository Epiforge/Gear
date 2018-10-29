using System;
using System.Collections.Generic;

namespace Gear.ActiveQuery.Expressions
{
    public class RangeActiveExpressionMembershipEventArgs<TElement, TResult> : EventArgs
    {
        public RangeActiveExpressionMembershipEventArgs(IReadOnlyList<(TElement element, TResult result)> elementResults, int index)
        {
            ElementResults = elementResults;
            Index = index;
        }

        public IReadOnlyList<(TElement element, TResult result)> ElementResults { get; }
        public int Index { get; }
    }
}
