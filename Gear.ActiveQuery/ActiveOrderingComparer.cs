using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Gear.ActiveQuery
{
    class ActiveOrderingComparer<T> : IComparer<T> where T : class
    {
        public ActiveOrderingComparer(IEnumerable<ActiveOrderingDescriptor<T>> orderingDescriptors)
        {
            var nullConstant = Expression.Constant(null, typeof(object));
            var firstParameter = Expression.Parameter(typeof(T));
            var secondParameter = Expression.Parameter(typeof(T));
            Expression expression = Expression.Constant(0);
            foreach (var orderingDescriptor in orderingDescriptors.Reverse())
            {
                var selector = orderingDescriptor.Selector;
                var firstSelection = Expression.Invoke((Expression<Func<T, IComparable>>)(f => selector(f)), firstParameter);
                var secondSelection = Expression.Invoke((Expression<Func<T, IComparable>>)(s => selector(s)), secondParameter);
                Expression comparisonExpression = Expression.Condition
                (
                    Expression.ReferenceEqual(firstSelection, nullConstant),
                    Expression.Condition
                    (
                        Expression.ReferenceEqual(secondSelection, nullConstant),
                        Expression.Constant(0, typeof(int)),
                        Expression.Constant(-1, typeof(int))
                    ),
                    Expression.Invoke((Expression<Func<IComparable, IComparable, int>>)((f, s) => f.CompareTo(s)), firstSelection, secondSelection)
                );
                if (orderingDescriptor.Descending)
                    comparisonExpression = Expression.Multiply(comparisonExpression, Expression.Constant(-1));
                expression = Expression.Condition(Expression.Equal(comparisonExpression, Expression.Constant(0)), expression, comparisonExpression);
            }
            comparisonFunction = Expression.Lambda<Func<T, T, int>>(expression, firstParameter, secondParameter).Compile();
        }

        readonly Func<T, T, int> comparisonFunction;

        public int Compare(T x, T y) => comparisonFunction(x, y);
    }
}
