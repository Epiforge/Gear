using System.Collections.Generic;
using System.Linq.Expressions;

namespace Gear.ActiveExpressions
{
    /// <summary>
    /// Defines methods to support the comparison of <see cref="Expression"/> objects for equality
    /// </summary>
    public class ExpressionEqualityComparer : IEqualityComparer<Expression>
    {
        static ExpressionEqualityComparer() => Default = new ExpressionEqualityComparer();

        /// <summary>
        /// Gets the default instance of <see cref="ExpressionEqualityComparer"/>
        /// </summary>
        public static ExpressionEqualityComparer Default { get; }

        /// <summary>
        /// Determines whether the specified <see cref="Expression"/> objects are equal
        /// </summary>
        /// <param name="x">The first <see cref="Expression"/> to compare</param>
        /// <param name="y">The second <see cref="Expression"/> to compare</param>
        /// <returns><c>true</c> if the specified objects are equal; otherwise, <c>false</c></returns>
        public bool Equals(Expression x, Expression y) => new ExpressionEqualityComparisonVisitor(x, y).IsLastVisitedEqual;

        /// <summary>
        /// Returns a hash code for the specified <see cref="Expression"/>
        /// </summary>
        /// <param name="obj">The <see cref="Expression"/> for which a hash code is to be returned</param>
        /// <returns>A hash code for the specified <see cref="Expression"/></returns>
        public int GetHashCode(Expression obj) => new ExpressionHashCodeVisitor(obj).LastVisitedHashCode;
    }
}
