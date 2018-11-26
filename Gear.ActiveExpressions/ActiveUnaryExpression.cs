using Gear.Components;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace Gear.ActiveExpressions
{
    class ActiveUnaryExpression : ActiveExpression, IEquatable<ActiveUnaryExpression>
    {
        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<(ExpressionType nodeType, ActiveExpression opperand, Type type, MethodInfo method, ActiveExpressionOptions options), ActiveUnaryExpression> instances = new Dictionary<(ExpressionType nodeType, ActiveExpression opperand, Type type, MethodInfo method, ActiveExpressionOptions options), ActiveUnaryExpression>();
        static readonly ConcurrentDictionary<Type, MethodInfo> nullableConversions = new ConcurrentDictionary<Type, MethodInfo>();

        static MethodInfo GetNullableConversionMethodInfo(Type nullableType) => nullableType.GetRuntimeMethod("op_Implicit", new Type[] { nullableType.GenericTypeArguments[0] });

        public static bool operator ==(ActiveUnaryExpression a, ActiveUnaryExpression b) => a?.Equals(b) ?? b is null;

        public static bool operator !=(ActiveUnaryExpression a, ActiveUnaryExpression b) => !(a == b);

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

        ActiveUnaryExpression(ExpressionType nodeType, ActiveExpression operand, Type type, MethodInfo method, ActiveExpressionOptions options, bool deferEvaluation) : base(type, nodeType, options, deferEvaluation)
        {
            this.nodeType = nodeType;
            this.operand = operand;
            this.operand.PropertyChanged += OperandPropertyChanged;
            this.method = method;
            if (this.method != null)
                fastMethod = GetFastMethodInfo(this.method);
            else if (this.nodeType == ExpressionType.Convert && type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                fastMethod = GetFastMethodInfo(nullableConversions.GetOrAdd(type, GetNullableConversionMethodInfo));
            else
                fastMethod = ExpressionOperations.GetFastMethodInfo(nodeType, type, operand.Type);
            EvaluateIfNotDeferred();
        }

        int disposalCount;
        readonly FastMethodInfo fastMethod;
        readonly MethodInfo method;
        readonly ExpressionType nodeType;
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
                    instances.Remove((nodeType, operand, Type, method, options));
                    result = true;
                }
            }
            if (result)
                DisposeValueIfNecessary();
            return result;
        }

        void DisposeValueIfNecessary()
        {
            if (method != null && ApplicableOptions.IsMethodReturnValueDisposed(method))
            {
                var value = Value;
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

        public override bool Equals(object obj) => Equals(obj as ActiveUnaryExpression);

        public bool Equals(ActiveUnaryExpression other) => other?.method == method && other?.nodeType == nodeType && other?.operand == operand && other?.options == options;

        protected override void Evaluate()
        {
            try
            {
                DisposeValueIfNecessary();
                var operandFault = operand.Fault;
                if (operandFault != null)
                    Fault = operandFault;
                else if (fastMethod != null)
                    Value = fastMethod.Invoke(null, new object[] { operand.Value });
                else
                    Value = operand.Value;
            }
            catch (Exception ex)
            {
                Fault = ex;
            }
        }

        public override int GetHashCode() => HashCodes.CombineObjects(typeof(ActiveUnaryExpression), method, nodeType, operand, options);

        void OperandPropertyChanged(object sender, PropertyChangedEventArgs e) => Evaluate();
    }
}
