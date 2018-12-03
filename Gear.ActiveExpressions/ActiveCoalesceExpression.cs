using Gear.Components;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Gear.ActiveExpressions
{
    class ActiveCoalesceExpression : ActiveBinaryExpression
    {
        static readonly object conversionDelegateManagementLock = new object();
        static readonly Dictionary<(Type convertFrom, Type convertTo), UnaryOperationDelegate> conversionDelegates = new Dictionary<(Type convertFrom, Type convertTo), UnaryOperationDelegate>();

        public static bool operator ==(ActiveCoalesceExpression a, ActiveCoalesceExpression b) => a?.left == b?.left && a?.right == b?.right && a?.options == b?.options;

        public static bool operator !=(ActiveCoalesceExpression a, ActiveCoalesceExpression b) => a?.left != b?.left || a?.right != b?.right || a?.options != b?.options;

        public ActiveCoalesceExpression(Type type, ActiveExpression left, ActiveExpression right, LambdaExpression conversion, ActiveExpressionOptions options, bool deferEvaluation) : base(type, ExpressionType.Coalesce, left, right, false, null, options, deferEvaluation, false, false)
        {
            if (conversion != null)
            {
                var key = (convertFrom: conversion.Parameters[0].Type, convertTo: conversion.Body.Type);
                lock (conversionDelegateManagementLock)
                {
                    if (!conversionDelegates.TryGetValue(key, out var conversionDelegate))
                    {
                        var parameter = Expression.Parameter(typeof(object));
                        conversionDelegate = Expression.Lambda<UnaryOperationDelegate>(Expression.Convert(Expression.Invoke(conversion, Expression.Convert(parameter, key.convertFrom)), typeof(object)), parameter).Compile();
                        conversionDelegates.Add(key, conversionDelegate);
                    }
                    this.conversionDelegate = conversionDelegate;
                }
            }
            EvaluateIfNotDeferred();
        }

        readonly UnaryOperationDelegate conversionDelegate;

        public override bool Equals(object obj) => obj is ActiveCoalesceExpression other && left.Equals(other.left) && right.Equals(other.right) && (options?.Equals(other.options) ?? other.options is null);

        protected override void Evaluate()
        {
            try
            {
                var leftFault = left.Fault;
                if (leftFault != null)
                    Fault = leftFault;
                else
                {
                    var leftValue = left.Value;
                    if (leftValue != null)
                        Value = conversionDelegate == null ? leftValue : conversionDelegate(leftValue);
                    else
                    {
                        var rightFault = right.Fault;
                        if (rightFault != null)
                            Fault = rightFault;
                        else
                            Value = right.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                Fault = ex;
            }
        }

        public override int GetHashCode() => HashCodes.CombineObjects(typeof(ActiveCoalesceExpression), left, right);

        public override string ToString() => $"({left} ?? {right}) {ToStringSuffix}";
    }
}
