using Gear.Components;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Gear.ActiveExpressions
{
    class ActiveUnaryExpression : ActiveExpression
    {
        static readonly ConcurrentDictionary<Type, MethodInfo> convertNullableGenericMethods = new ConcurrentDictionary<Type, MethodInfo>();
        static readonly MethodInfo convertNullableMethod = typeof(ActiveUnaryExpression).GetRuntimeMethods().Single(m => m.Name == nameof(ConvertNullable));
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

        static T? ConvertNullable<T>(object value) where T : struct => value is null ? null : (T?)value;

        static MethodInfo GetConvertNullableGenericMethod(Type type) => convertNullableMethod.MakeGenericMethod(type);

        public static bool operator ==(ActiveUnaryExpression a, ActiveUnaryExpression b) => a?.method == b?.method && a?.NodeType == b?.NodeType && a?.operand == b?.operand && a?.options == b?.options;

        public static bool operator !=(ActiveUnaryExpression a, ActiveUnaryExpression b) => a?.method != b?.method || a?.NodeType != b?.NodeType || a?.operand != b?.operand || a?.options != b?.options;

        ActiveUnaryExpression(ExpressionType nodeType, ActiveExpression operand, Type type, MethodInfo method, ActiveExpressionOptions options, bool deferEvaluation) : base(type, nodeType, options, deferEvaluation)
        {
            this.operand = operand;
            this.operand.PropertyChanged += OperandPropertyChanged;
            this.method = method;
            if (this.method != null)
                fastMethod = GetFastMethodInfo(this.method);
            else if (NodeType == ExpressionType.Convert && type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                fastMethod = GetFastMethodInfo(convertNullableGenericMethods.GetOrAdd(type.GenericTypeArguments[0], GetConvertNullableGenericMethod));
            else
                fastMethod = ExpressionOperations.GetFastMethodInfo(nodeType, type, operand.Type) ?? throw new NotSupportedException($"There is no implementation of {nodeType} available that accepts a(n) {operand.Type.Name} operand and which returns a(n) {type.Name}");
            EvaluateIfNotDeferred();
        }

        int disposalCount;
        readonly FastMethodInfo fastMethod;
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
                    Value = fastMethod.Invoke(null, new object[] { operand.Value });
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
