using Gear.ActiveExpressions;
using Gear.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Gear.ActiveQuery
{
    public class CachedRangeActiveExpressionKeyEqualityComparer<TResult> : IEqualityComparer<(IEnumerable source, Expression<Func<object, TResult>> expression, ActiveExpressionOptions options)>, IEqualityComparer<(IDictionary source, Expression<Func<object, object, TResult>> expression, ActiveExpressionOptions options)>
    {
        static CachedRangeActiveExpressionKeyEqualityComparer() => Default = new CachedRangeActiveExpressionKeyEqualityComparer<TResult>();

        public static CachedRangeActiveExpressionKeyEqualityComparer<TResult> Default { get; }

        public bool Equals((IEnumerable source, Expression<Func<object, TResult>> expression, ActiveExpressionOptions options) x, (IEnumerable source, Expression<Func<object, TResult>> expression, ActiveExpressionOptions options) y) =>
            ReferenceEquals(x.source, y.source) && ExpressionEqualityComparer.Default.Equals(x.expression, y.expression) && x.options == y.options;

        public bool Equals((IDictionary source, Expression<Func<object, object, TResult>> expression, ActiveExpressionOptions options) x, (IDictionary source, Expression<Func<object, object, TResult>> expression, ActiveExpressionOptions options) y) =>
            ReferenceEquals(x.source, y.source) && ExpressionEqualityComparer.Default.Equals(x.expression, y.expression) && x.options == y.options;

        public int GetHashCode((IEnumerable source, Expression<Func<object, TResult>> expression, ActiveExpressionOptions options) obj) =>
            HashCodes.CombineHashCodes(obj.source?.GetHashCode() ?? 0, ExpressionEqualityComparer.Default.GetHashCode(obj.expression), obj.options?.GetHashCode() ?? 0);

        public int GetHashCode((IDictionary source, Expression<Func<object, object, TResult>> expression, ActiveExpressionOptions options) obj) =>
            HashCodes.CombineHashCodes(obj.source?.GetHashCode() ?? 0, ExpressionEqualityComparer.Default.GetHashCode(obj.expression), obj.options?.GetHashCode() ?? 0);
    }

    public class CachedRangeActiveExpressionKeyEqualityComparer<TElement, TResult> : IEqualityComparer<(IEnumerable<TElement> source, Expression<Func<TElement, TResult>> expression, ActiveExpressionOptions options)>
    {
        static CachedRangeActiveExpressionKeyEqualityComparer() => Default = new CachedRangeActiveExpressionKeyEqualityComparer<TElement, TResult>();

        public static CachedRangeActiveExpressionKeyEqualityComparer<TElement, TResult> Default { get; }

        public bool Equals((IEnumerable<TElement> source, Expression<Func<TElement, TResult>> expression, ActiveExpressionOptions options) x, (IEnumerable<TElement> source, Expression<Func<TElement, TResult>> expression, ActiveExpressionOptions options) y) =>
            ReferenceEquals(x.source, y.source) && ExpressionEqualityComparer.Default.Equals(x.expression, y.expression) && x.options == y.options;

        public int GetHashCode((IEnumerable<TElement> source, Expression<Func<TElement, TResult>> expression, ActiveExpressionOptions options) obj) =>
            HashCodes.CombineHashCodes(obj.source?.GetHashCode() ?? 0, ExpressionEqualityComparer.Default.GetHashCode(obj.expression), obj.options?.GetHashCode() ?? 0);
    }

    public class CachedRangeActiveExpressionKeyEqualityComparer<TKey, TValue, TResult> : IEqualityComparer<(IDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, TResult>> expression, ActiveExpressionOptions options)>, IEqualityComparer<(IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, TResult>> expression, ActiveExpressionOptions options)>
    {
        static CachedRangeActiveExpressionKeyEqualityComparer() => Default = new CachedRangeActiveExpressionKeyEqualityComparer<TKey, TValue, TResult>();

        public static CachedRangeActiveExpressionKeyEqualityComparer<TKey, TValue, TResult> Default { get; }

        public bool Equals((IDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, TResult>> expression, ActiveExpressionOptions options) x, (IDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, TResult>> expression, ActiveExpressionOptions options) y) =>
            ReferenceEquals(x.source, y.source) && ExpressionEqualityComparer.Default.Equals(x.expression, y.expression) && x.options == y.options;

        public bool Equals((IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, TResult>> expression, ActiveExpressionOptions options) x, (IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, TResult>> expression, ActiveExpressionOptions options) y) =>
            ReferenceEquals(x.source, y.source) && ExpressionEqualityComparer.Default.Equals(x.expression, y.expression) && x.options == y.options;

        public int GetHashCode((IDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, TResult>> expression, ActiveExpressionOptions options) obj) =>
            HashCodes.CombineHashCodes(obj.source?.GetHashCode() ?? 0, ExpressionEqualityComparer.Default.GetHashCode(obj.expression), obj.options?.GetHashCode() ?? 0);

        public int GetHashCode((IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, TResult>> expression, ActiveExpressionOptions options) obj) =>
            HashCodes.CombineHashCodes(obj.source?.GetHashCode() ?? 0, ExpressionEqualityComparer.Default.GetHashCode(obj.expression), obj.options?.GetHashCode() ?? 0);
    }
}
