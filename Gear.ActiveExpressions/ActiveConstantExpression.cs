using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Gear.ActiveExpressions
{
    class ActiveConstantExpression : ActiveExpression
    {
        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<(Type type, object value), ActiveConstantExpression> instances = new Dictionary<(Type type, object value), ActiveConstantExpression>();

        public static ActiveConstantExpression Create(ConstantExpression constantExpression)
        {
            var type = constantExpression.Type;
            var value = constantExpression.Value;
            var key = (type, value);
            lock (instanceManagementLock)
            {
                if (!instances.TryGetValue(key, out var activeConstantExpression))
                {
                    activeConstantExpression = new ActiveConstantExpression(type, value);
                    instances.Add(key, activeConstantExpression);
                }
                ++activeConstantExpression.disposalCount;
                return activeConstantExpression;
            }
        }

        ActiveConstantExpression(Type type, object value) : base(type, ExpressionType.Constant) => Value = value;

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
    }
}
