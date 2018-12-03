using Gear.Components;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace Gear.ActiveExpressions
{
    class ActiveUnaryExpression : ActiveExpression
    {
        static readonly Dictionary<(ExpressionType nodeType, Type operandType, Type returnValueType, MethodInfo method), UnaryOperationDelegate> implementations = new Dictionary<(ExpressionType nodeType, Type operandType, Type returnValueType, MethodInfo method), UnaryOperationDelegate>();
        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<(ExpressionType nodeType, ActiveExpression opperand, Type type, MethodInfo method, ActiveExpressionOptions options), ActiveUnaryExpression> instances = new Dictionary<(ExpressionType nodeType, ActiveExpression opperand, Type type, MethodInfo method, ActiveExpressionOptions options), ActiveUnaryExpression>();

        public static ActiveUnaryExpression Create(UnaryExpression unaryExpression, ActiveExpressionOptions options, bool deferEvaluation)
        {
            var nodeType = unaryExpression.NodeType;
            var operand = Create(unaryExpression.Operand, options, deferEvaluation);
            var type = unaryExpression.Type;
            var method = unaryExpression.Method;
            var key = (nodeType, operand, type, method, options);
            lock (instanceManagementLock)
            {
                if (!instances.TryGetValue(key, out var activeUnaryExpression))
                {
                    activeUnaryExpression = new ActiveUnaryExpression(nodeType, operand, type, method, options, deferEvaluation);
                    instances.Add(key, activeUnaryExpression);
                }
                ++activeUnaryExpression.disposalCount;
                return activeUnaryExpression;
            }
        }

        public static bool operator ==(ActiveUnaryExpression a, ActiveUnaryExpression b) => a?.method == b?.method && a?.NodeType == b?.NodeType && a?.operand == b?.operand && a?.options == b?.options;

        public static bool operator !=(ActiveUnaryExpression a, ActiveUnaryExpression b) => a?.method != b?.method || a?.NodeType != b?.NodeType || a?.operand != b?.operand || a?.options != b?.options;

        ActiveUnaryExpression(ExpressionType nodeType, ActiveExpression operand, Type type, MethodInfo method, ActiveExpressionOptions options, bool deferEvaluation) : base(type, nodeType, options, deferEvaluation)
        {
            this.operand = operand;
            this.operand.PropertyChanged += OperandPropertyChanged;
            this.method = method;
            var implementationKey = (NodeType, this.operand.Type, Type, this.method);
            if (!implementations.TryGetValue(implementationKey, out var @delegate))
            {
                var operandParameter = Expression.Parameter(typeof(object));
                var operandConversion = Expression.Convert(operandParameter, this.operand.Type);
                @delegate = Expression.Lambda<UnaryOperationDelegate>(Expression.Convert(this.method == null ? Expression.MakeUnary(NodeType, operandConversion, Type) : Expression.MakeUnary(NodeType, operandConversion, Type, this.method), typeof(object)), operandParameter).Compile();
                implementations.Add(implementationKey, @delegate);
            }
            this.@delegate = @delegate;
            EvaluateIfNotDeferred();
        }

        readonly UnaryOperationDelegate @delegate;
        int disposalCount;
        readonly MethodInfo method;
        readonly ActiveExpression operand;

        protected override bool Dispose(bool disposing)
        {
            var result = false;
            lock (instanceManagementLock)
            {
                if (--disposalCount == 0)
                {
                    operand.PropertyChanged -= OperandPropertyChanged;
                    operand.Dispose();
                    instances.Remove((NodeType, operand, Type, method, options));
                    result = true;
                }
            }
            if (result)
                DisposeValueIfNecessary();
            return result;
        }

        void DisposeValueIfNecessary()
        {
            if (method != null && ApplicableOptions.IsMethodReturnValueDisposed(method) && TryGetUndeferredValue(out var value))
            {
                try
                {
                    if (value is IDisposable disposable)
                        disposable.Dispose();
                    else if (value is IAsyncDisposable asyncDisposable)
                        asyncDisposable.DisposeAsync().Wait();
                }
                catch (Exception ex)
                {
                    throw new Exception("Disposal of method return value failed", ex);
                }
            }
        }

        public override bool Equals(object obj) => obj is ActiveUnaryExpression other && (method?.Equals(other.method) ?? other.method is null) && NodeType.Equals(other.NodeType) && operand.Equals(other.operand) && (options?.Equals(other.options) ?? other.options is null);

        protected override void Evaluate()
        {
            try
            {
                DisposeValueIfNecessary();
                var operandFault = operand.Fault;
                if (operandFault != null)
                    Fault = operandFault;
                else
                    Value = @delegate.Invoke(operand.Value);
            }
            catch (Exception ex)
            {
                Fault = ex;
            }
        }

        public override int GetHashCode() => HashCodes.CombineObjects(typeof(ActiveUnaryExpression), method, NodeType, operand, options);

        void OperandPropertyChanged(object sender, PropertyChangedEventArgs e) => Evaluate();

        public override string ToString() => $"{GetOperatorExpressionSyntax(NodeType, Type, operand)} {ToStringSuffix}";
    }
}
