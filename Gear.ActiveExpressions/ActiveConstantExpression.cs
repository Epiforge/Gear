using Gear.Components;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Gear.ActiveExpressions
{
    class ActiveConstantExpression : ActiveExpression
    {
        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<(Type type, object value, ActiveExpressionOptions options), ActiveConstantExpression> instances = new Dictionary<(Type type, object value, ActiveExpressionOptions options), ActiveConstantExpression>();

        public static ActiveConstantExpression Create(ConstantExpression constantExpression, ActiveExpressionOptions options)
        {
            var type = constantExpression.Type;
            var value = constantExpression.Value;
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

        public static bool operator ==(ActiveConstantExpression a, ActiveConstantExpression b) => a?.Value == b?.Value && a?.options == b?.options;

        public static bool operator !=(ActiveConstantExpression a, ActiveConstantExpression b) => a?.Value != b?.Value || a?.options != b?.options;

        ActiveConstantExpression(Type type, object value, ActiveExpressionOptions options) : base(type, ExpressionType.Constant, options, value)
        {
        }

        readonly object constant;
        int disposalCount;

        protected override bool Dispose(bool disposing)
        {
            lock (instanceManagementLock)
            {
                if (--disposalCount > 0)
                    return false;
                instances.Remove((Type, Value, options));
                return true;
            }
        }

        public override bool Equals(object obj) => obj is ActiveConstantExpression other && Type == other.Type && FastEqualityComparer.Create(Type).Equals(Value, other.Value) && (options?.Equals(other.options) ?? other.options is null);

        protected override void Evaluate() => Value = constant;

        public override int GetHashCode() => HashCodes.CombineObjects(typeof(ActiveConstantExpression), Value);

        public override string ToString() => $"{{C}} {ToStringSuffix}";
    }
}
