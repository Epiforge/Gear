using Gear.Components;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Gear.ActiveExpressions
{
    class ActiveConstantExpression : ActiveExpression
    {
        class ExpressionInstanceKeyComparer : IEqualityComparer<(Expression expression, ActiveExpressionOptions options)>
        {
            public static ExpressionInstanceKeyComparer Default { get; } = new ExpressionInstanceKeyComparer();

            public bool Equals((Expression expression, ActiveExpressionOptions options) x, (Expression expression, ActiveExpressionOptions options) y) => ExpressionEqualityComparer.Default.Equals(x.expression, y.expression) && x.options == y.options;

            public int GetHashCode((Expression expression, ActiveExpressionOptions options) obj) => HashCodes.CombineHashCodes(ExpressionEqualityComparer.Default.GetHashCode(obj.expression), obj.options?.GetHashCode() ?? 0);
        }

        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<(Expression expression, ActiveExpressionOptions options), ActiveConstantExpression> expressionInstances = new Dictionary<(Expression expression, ActiveExpressionOptions options), ActiveConstantExpression>(ExpressionInstanceKeyComparer.Default);
        static readonly Dictionary<(Type type, object value, ActiveExpressionOptions options), ActiveConstantExpression> instances = new Dictionary<(Type type, object value, ActiveExpressionOptions options), ActiveConstantExpression>();

        public static ActiveConstantExpression Create(ConstantExpression constantExpression, ActiveExpressionOptions options)
        {
            var type = constantExpression.Type;
            var value = constantExpression.Value;
            if (typeof(Expression).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
            {
                var key = ((Expression)value, options);
                lock (instanceManagementLock)
                {
                    if (!expressionInstances.TryGetValue(key, out var activeConstantExpression))
                    {
                        activeConstantExpression = new ActiveConstantExpression(type, value, options);
                        expressionInstances.Add(key, activeConstantExpression);
                    }
                    ++activeConstantExpression.disposalCount;
                    return activeConstantExpression;
                }
            }
            else
            {
                var key = (type, value, options);
                lock (instanceManagementLock)
                {
                    if (!instances.TryGetValue(key, out var activeConstantExpression))
                    {
                        activeConstantExpression = new ActiveConstantExpression(type, value, options);
                        instances.Add(key, activeConstantExpression);
                    }
                    ++activeConstantExpression.disposalCount;
                    return activeConstantExpression;
                }
            }
        }

        public static bool operator ==(ActiveConstantExpression a, ActiveConstantExpression b) => a?.Value == b?.Value && a?.options == b?.options;

        public static bool operator !=(ActiveConstantExpression a, ActiveConstantExpression b) => a?.Value != b?.Value || a?.options != b?.options;

        ActiveConstantExpression(Type type, object value, ActiveExpressionOptions options) : base(type, ExpressionType.Constant, options, value)
        {
        }

        int disposalCount;

        protected override bool Dispose(bool disposing)
        {
            lock (instanceManagementLock)
            {
                if (--disposalCount > 0)
                    return false;
                if (typeof(Expression).GetTypeInfo().IsAssignableFrom(Type.GetTypeInfo()))
                    expressionInstances.Remove(((Expression)Value, options));
                else
                    instances.Remove((Type, Value, options));
                return true;
            }
        }

        public override bool Equals(object obj) => obj is ActiveConstantExpression other && Type == other.Type && FastEqualityComparer.Create(Type).Equals(Value, other.Value) && (options?.Equals(other.options) ?? other.options is null);

        public override int GetHashCode() => HashCodes.CombineObjects(typeof(ActiveConstantExpression), Value);

        public override string ToString() => $"{{C}} {ToStringSuffix}";
    }
}
