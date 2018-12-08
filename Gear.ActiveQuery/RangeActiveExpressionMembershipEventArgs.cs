using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Gear.ActiveQuery
{
    public class RangeActiveExpressionMembershipEventArgs<TElement, TResult> : EventArgs
    {
        public RangeActiveExpressionMembershipEventArgs(IReadOnlyList<(TElement element, TResult result)> elementResults, int index)
        {
            ElementResults = elementResults.ToImmutableArray();
            Index = index;
        }

        public IReadOnlyList<(TElement element, TResult result)> ElementResults { get; }
        public int Index { get; }
    }
}
