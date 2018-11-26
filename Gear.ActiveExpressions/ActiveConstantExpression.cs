using Gear.Components;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Gear.ActiveExpressions
{
    class ActiveConstantExpression : ActiveExpression, IEquatable<ActiveConstantExpression>
    {
        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<(Type type, object value), ActiveConstantExpression> instances = new Dictionary<(Type type, object value), ActiveConstantExpression>();

        public static ActiveConstantExpression Create(ConstantExpression constantExpression, ActiveExpressionOptions options, bool deferEvaluation)
        {
            var type = constantExpression.Type;
            var value = constantExpression.Value;
            var key = (type, value);
            lock (instanceManagementLock)
            {
                if (!instances.TryGetValue(key, out var activeConstantExpression))
                {
                    activeConstantExpression = new ActiveConstantExpression(type, value, options, deferEvaluation);
                    instances.Add(key, activeConstantExpression);
                }
                ++activeConstantExpression.disposalCount;
                return activeConstantExpression;
            }
        }

        public static bool operator ==(ActiveConstantExpression a, ActiveConstantExpression b) => a?.Equals(b) ?? b is null;

        public static bool operator !=(ActiveConstantExpression a, ActiveConstantExpression b) => !(a == b);

        ActiveConstantExpression(Type type, object value, ActiveExpressionOptions options, bool deferEvaluation) : base(type, ExpressionType.Constant, options, deferEvaluation)
        {
            constant = value;
            EvaluateIfNotDeferred();
        }

        readonly object constant;
        int disposalCount;

        protected override bool Dispose(bool disposing)
        {
            lock (instanceManagementLock)
            {
                if (--disposalCount > 0)
                    return false;
                instances.Remove((Type, Value));
                return true;
            }
        }

        public override bool Equals(object obj) => Equals(obj as ActiveConstantExpression);

        public bool Equals(ActiveConstantExpression other) => other?.Value == Value;

        protected override void Evaluate() => Value = constant;

        public override int GetHashCode() => HashCodes.CombineObjects(typeof(ActiveConstantExpression), Value);
    }
}
