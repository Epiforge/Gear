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
        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<(ExpressionType nodeType, ActiveExpression opperand, bool isLifted, bool isLiftedToNull, Type type, MethodInfo method, ActiveExpressionOptions options), ActiveUnaryExpression> instances = new Dictionary<(ExpressionType nodeType, ActiveExpression opperand, bool isLifted, bool isLiftedToNull, Type type, MethodInfo method, ActiveExpressionOptions options), ActiveUnaryExpression>();

        public static ActiveUnaryExpression Create(UnaryExpression unaryExpression, ActiveExpressionOptions options, bool deferEvaluation)
        {
            var nodeType = unaryExpression.NodeType;
            var operand = Create(unaryExpression.Operand, options, deferEvaluation);
            var isLifted = unaryExpression.IsLifted;
            var isLiftedToNull = unaryExpression.IsLiftedToNull;
            var type = unaryExpression.Type;
            var method = unaryExpression.Method;
            var key = (nodeType, operand, isLifted, isLiftedToNull, type, method, options);
            lock (instanceManagementLock)
            {
                if (!instances.TryGetValue(key, out var activeUnaryExpression))
                {
                    activeUnaryExpression = new ActiveUnaryExpression(nodeType, operand, isLifted, isLiftedToNull, type, method, options, deferEvaluation);
                    instances.Add(key, activeUnaryExpression);
                }
                ++activeUnaryExpression.disposalCount;
                return activeUnaryExpression;
            }
        }

        public static bool operator ==(ActiveUnaryExpression a, ActiveUnaryExpression b) => a?.method == b?.method && a?.NodeType == b?.NodeType && a?.operand == b?.operand && a?.options == b?.options;

        public static bool operator !=(ActiveUnaryExpression a, ActiveUnaryExpression b) => a?.method != b?.method || a?.NodeType != b?.NodeType || a?.operand != b?.operand || a?.options != b?.options;

        ActiveUnaryExpression(ExpressionType nodeType, ActiveExpression operand, bool isLifted, bool isLiftedToNull, Type type, MethodInfo method, ActiveExpressionOptions options, bool deferEvaluation) : base(type, nodeType, options, deferEvaluation)
        {
            this.operand = operand;
            this.operand.PropertyChanged += OperandPropertyChanged;
            this.isLifted = isLifted;
            if (this.isLiftedToNull = isLiftedToNull)
                fastLiftToNull = GetConvertNullableFastMethodInfo(type);
            this.method = method;
            if (this.method != null)
                fastMethod = GetFastMethodInfo(this.method);
            else if (NodeType == ExpressionType.Convert && type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                fastMethod = GetConvertNullableFastMethodInfo(type);
            else
                fastMethod = ExpressionOperations.GetFastMethodInfo(nodeType, type, operand.Type) ?? throw new NotSupportedException($"There is no implementation of {nodeType} available that accepts a(n) {operand.Type.Name} operand and which returns a(n) {type.Name}");
            EvaluateIfNotDeferred();
        }

        int disposalCount;
        readonly FastMethodInfo fastLiftToNull;
        readonly FastMethodInfo fastMethod;
        readonly bool isLifted;
        readonly bool isLiftedToNull;
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
                    instances.Remove((NodeType, operand, isLifted, isLiftedToNull, Type, method, options));
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
                var operandValue = operand.Value;
                if (operandFault != null)
                    Fault = operandFault;
                else if (isLifted && operandValue is null)
                    Value = null;
                else
                {
                    var newValue = fastMethod.Invoke(null, new object[] { operandValue });
                    if (isLiftedToNull)
                        newValue = fastLiftToNull.Invoke(this, new object[] { newValue });
                    Value = newValue;
                }
            }
            catch (Exception ex)
            {
                Fault = ex;
            }
        }

        public override int GetHashCode() => HashCodes.CombineObjects(typeof(ActiveUnaryExpression), method, NodeType, operand, options);

        void OperandPropertyChanged(object sender, PropertyChangedEventArgs e) => Evaluate();

        public override string ToString() => $"{ExpressionOperations.GetExpressionSyntax(NodeType, Type, operand)} {ToStringSuffix}";
    }
}
