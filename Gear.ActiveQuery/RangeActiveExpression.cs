using Gear.ActiveExpressions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Gear.ActiveQuery
{
    static class RangeActiveExpression
    {
        public static DictionaryRangeActiveExpression<TResult> Create<TResult>(IDictionary source, Expression<Func<object, object, TResult>> expression, ActiveExpressionOptions options = null) =>
            new DictionaryRangeActiveExpression<TResult>(source, expression, options);

        public static DictionaryRangeActiveExpression<TKey, TValue, TResult> Create<TKey, TValue, TResult>(IDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, TResult>> expression, ActiveExpressionOptions options = null) =>
            new DictionaryRangeActiveExpression<TKey, TValue, TResult>(source, expression, options);

        public static ReadOnlyDictionaryRangeActiveExpression<TKey, TValue, TResult> Create<TKey, TValue, TResult>(IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, TResult>> expression, ActiveExpressionOptions options = null) =>
            new ReadOnlyDictionaryRangeActiveExpression<TKey, TValue, TResult>(source, expression, options);

        public static EnumerableRangeActiveExpression<TResult> Create<TResult>(IEnumerable source, Expression<Func<object, TResult>> expression, ActiveExpressionOptions options = null) =>
            new EnumerableRangeActiveExpression<TResult>(source, expression, options);

        public static EnumerableRangeActiveExpression<TElement, TResult> Create<TElement, TResult>(IEnumerable<TElement> source, Expression<Func<TElement, TResult>> expression, ActiveExpressionOptions options = null) =>
            new EnumerableRangeActiveExpression<TElement, TResult>(source, expression, options);
    }
}
