using System.Collections.Generic;
using System.Linq.Expressions;

namespace Gear.ActiveExpressions
{
    public class ExpressionEqualityComparer : IEqualityComparer<Expression>
    {
        static ExpressionEqualityComparer() => Default = new ExpressionEqualityComparer();

        public static ExpressionEqualityComparer Default { get; }

        public bool Equals(Expression x, Expression y) => new ExpressionEqualityComparisonVisitor(x, y).IsLastVisitedEqual;

        public int GetHashCode(Expression obj) => new ExpressionHashCodeVisitor(obj).LastVisitedHashCode;
    }
}
