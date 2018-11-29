using Gear.Components;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Gear.ActiveExpressions
{
    class ActiveConstantExpression : ActiveExpression
    {
        static readonly Dictionary<ConstantExpression, string> convertedParameters = new Dictionary<ConstantExpression, string>();
        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<(Type type, object value), ActiveConstantExpression> instances = new Dictionary<(Type type, object value), ActiveConstantExpression>();

        public static void AddConvertedParameter(ConstantExpression constantExpression, string parameterName)
        {
            lock (instanceManagementLock)
                convertedParameters.Add(constantExpression, parameterName);
        }

        public static ActiveConstantExpression Create(ConstantExpression constantExpression, ActiveExpressionOptions options, bool deferEvaluation)
        {
            var type = constantExpression.Type;
            var value = constantExpression.Value;
            var key = (type, value);
            lock (instanceManagementLock)
            {
                if (!instances.TryGetValue(key, out var activeConstantExpression))
                {
                    if (convertedParameters.TryGetValue(constantExpression, out var parameterName))
                    {
                        convertedParameters.Remove(constantExpression);
                        activeConstantExpression = new ActiveConstantExpression(type, parameterName, value, options, deferEvaluation);
                    }
                    else
                        activeConstantExpression = new ActiveConstantExpression(type, value, options, deferEvaluation);
                    instances.Add(key, activeConstantExpression);
                }
                ++activeConstantExpression.disposalCount;
                return activeConstantExpression;
            }
        }

        public static bool operator ==(ActiveConstantExpression a, ActiveConstantExpression b) => a?.Value == b?.Value && a?.options == b?.options;

        public static bool operator !=(ActiveConstantExpression a, ActiveConstantExpression b) => a?.Value != b?.Value || a?.options != b?.options;

        ActiveConstantExpression(Type type, object value, ActiveExpressionOptions options, bool deferEvaluation) : base(type, ExpressionType.Constant, options, deferEvaluation)
        {
            constant = value;
            EvaluateIfNotDeferred();
        }

        ActiveConstantExpression(Type type, string parameterName, object value, ActiveExpressionOptions options, bool deferEvaluation) : this(type, value, options, deferEvaluation) => this.parameterName = parameterName;

        readonly object constant;
        int disposalCount;
        readonly string parameterName;

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

        public override bool Equals(object obj) => obj is ActiveConstantExpression other && (constant?.Equals(other.constant) ?? other.constant is null) && (options?.Equals(other.options) ?? other.options is null);

        protected override void Evaluate() => Value = constant;

        public override int GetHashCode() => HashCodes.CombineObjects(typeof(ActiveConstantExpression), Value);

        public override string ToString()// => $"{parameterName ?? $"{constant}"} {ToStringSuffix}";
        {
            if (parameterName != null)
                return $"{parameterName} {ToStringSuffix}";
            return parameterName != null ? $"{parameterName} {ToStringSuffix}" : $"{GetValueLiteral(null, constant)}";
        }
    }
}
