using Gear.Components;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace Gear.ActiveExpressions
{
    class ActiveBinaryExpression : ActiveExpression
    {
        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<(ExpressionType nodeType, ActiveExpression left, ActiveExpression right, ActiveExpressionOptions options), ActiveBinaryExpression> factoryInstances = new Dictionary<(ExpressionType nodeType, ActiveExpression left, ActiveExpression right, ActiveExpressionOptions options), ActiveBinaryExpression>();
        static readonly Dictionary<(ExpressionType nodeType, ActiveExpression left, ActiveExpression right, bool isLiftedToNull, MethodInfo method, ActiveExpressionOptions options), ActiveBinaryExpression> implementationInstances = new Dictionary<(ExpressionType nodeType, ActiveExpression left, ActiveExpression right, bool isLiftedToNull, MethodInfo method, ActiveExpressionOptions options), ActiveBinaryExpression>();
        static readonly FastMethodInfo referenceEqualsFastMethod = new FastMethodInfo(typeof(object).GetRuntimeMethod(nameof(object.ReferenceEquals), new Type[] { typeof(object), typeof(object) }));

        public static ActiveBinaryExpression Create(BinaryExpression binaryExpression, ActiveExpressionOptions options, bool deferEvaluation)
        {
            DisallowConversions(binaryExpression.Conversion);
            var type = binaryExpression.Type;
            var nodeType = binaryExpression.NodeType;
            var left = Create(binaryExpression.Left, options, deferEvaluation);
            var method = binaryExpression.Method;
            ActiveExpression right;
            if (method == null)
            {
                switch (nodeType)
                {
                    case ExpressionType.AndAlso when type == typeof(bool):
                    case ExpressionType.Coalesce:
                    case ExpressionType.OrElse when type == typeof(bool):
                        right = Create(binaryExpression.Right, options, true);
                        break;
                    default:
                        right = Create(binaryExpression.Right, options, deferEvaluation);
                        break;
                }
                var key = (nodeType, left, right, options);
                lock (instanceManagementLock)
                {
                    if (!factoryInstances.TryGetValue(key, out var activeBinaryExpression))
                    {
                        switch (nodeType)
                        {
                            case ExpressionType.AndAlso when type == typeof(bool):
                                activeBinaryExpression = new ActiveAndAlsoExpression(left, right, options, deferEvaluation);
                                break;
                            case ExpressionType.Coalesce:
                                activeBinaryExpression = new ActiveCoalesceExpression(type, left, right, binaryExpression.Conversion, options, deferEvaluation);
                                break;
                            case ExpressionType.OrElse when type == typeof(bool):
                                activeBinaryExpression = new ActiveOrElseExpression(left, right, options, deferEvaluation);
                                break;
                            default:
                                activeBinaryExpression = new ActiveBinaryExpression(type, nodeType, left, right, options, deferEvaluation);
                                break;
                        }
                        factoryInstances.Add(key, activeBinaryExpression);
                    }
                    ++activeBinaryExpression.disposalCount;
                    return activeBinaryExpression;
                }
            }
            else
            {
                right = Create(binaryExpression.Right, options, deferEvaluation);
                var isLiftedToNull = binaryExpression.IsLiftedToNull;
                var key = (nodeType, left, right, isLiftedToNull, method, options);
                lock (instanceManagementLock)
                {
                    if (!implementationInstances.TryGetValue(key, out var activeBinaryExpression))
                    {
                        activeBinaryExpression = new ActiveBinaryExpression(type, nodeType, left, right, isLiftedToNull, method, options, deferEvaluation);
                        implementationInstances.Add(key, activeBinaryExpression);
                    }
                    ++activeBinaryExpression.disposalCount;
                    return activeBinaryExpression;
                }
            }
        }

        public static bool operator ==(ActiveBinaryExpression a, ActiveBinaryExpression b) => a?.left == b?.left && a?.method == b?.method && a?.NodeType == b?.NodeType && a?.right == b?.right && a?.options == b?.options;

        public static bool operator !=(ActiveBinaryExpression a, ActiveBinaryExpression b) => a?.left != b?.left || a?.method != b?.method || a?.NodeType != b?.NodeType || a?.right != b?.right || a?.options != b?.options;

        protected ActiveBinaryExpression(Type type, ExpressionType nodeType, ActiveExpression left, ActiveExpression right, ActiveExpressionOptions options, bool deferEvaluation, bool getOperation = true) : base(type, nodeType, options, deferEvaluation)
        {
            this.left = left;
            this.left.PropertyChanged += LeftPropertyChanged;
            this.right = right;
            this.right.PropertyChanged += RightPropertyChanged;
            if (NodeType == ExpressionType.Equal && !left.Type.GetTypeInfo().IsValueType)
                fastMethod = referenceEqualsFastMethod;
            else if (getOperation)
                fastMethod = ExpressionOperations.GetFastMethodInfo(nodeType, type, left.Type, right.Type) ?? throw new NotSupportedException($"There is no implementation of {nodeType} available that accepts a(n) {left.Type.Name} left-hand operand and a(n) {right.Type.Name} right-hand operand and which returns a(n) {type.Name}");
            EvaluateIfNotDeferred();
        }

        ActiveBinaryExpression(Type type, ExpressionType nodeType, ActiveExpression left, ActiveExpression right, bool isLiftedToNull, MethodInfo method, ActiveExpressionOptions options, bool deferEvaluation) : base(type, nodeType, options, deferEvaluation)
        {
            this.left = left;
            this.left.PropertyChanged += LeftPropertyChanged;
            this.right = right;
            this.right.PropertyChanged += RightPropertyChanged;
            this.isLiftedToNull = isLiftedToNull;
            this.method = method;
            fastMethod = GetFastMethodInfo(this.method);
            EvaluateIfNotDeferred();
        }

        int disposalCount;
        readonly FastMethodInfo fastMethod;
        readonly bool isLiftedToNull;
        protected readonly ActiveExpression left;
        readonly MethodInfo method;
        protected readonly ActiveExpression right;

        protected override bool Dispose(bool disposing)
        {
            var result = false;
            lock (instanceManagementLock)
            {
                if (--disposalCount == 0)
                {
                    left.PropertyChanged -= LeftPropertyChanged;
                    left.Dispose();
                    right.PropertyChanged -= RightPropertyChanged;
                    right.Dispose();
                    if (method == null)
                        factoryInstances.Remove((NodeType, left, right, options));
                    else
                        implementationInstances.Remove((NodeType, left, right, isLiftedToNull, method, options));
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

        public override bool Equals(object obj) => obj is ActiveBinaryExpression other && left.Equals(other.left) && (method?.Equals(other.method) ?? other.method is null) && NodeType.Equals(other.NodeType) && right.Equals(other.right) && (options?.Equals(other.options) ?? other.options is null);

        protected override void Evaluate()
        {
            var leftFault = left.Fault;
            var leftValue = left.Value;
            var rightFault = right.Fault;
            var rightValue = right.Value;
            try
            {
                DisposeValueIfNecessary();
                if (leftFault != null)
                    Fault = leftFault;
                else if (rightFault != null)
                    Fault = rightFault;
                else
                    Value = fastMethod.Invoke(null, new object[] { leftValue, rightValue });
            }
            catch (Exception ex)
            {
                Fault = ex;
            }
        }

        public override int GetHashCode() => HashCodes.CombineObjects(typeof(ActiveBinaryExpression), left, method, NodeType, right, options);

        void LeftPropertyChanged(object sender, PropertyChangedEventArgs e) => Evaluate();

        void RightPropertyChanged(object sender, PropertyChangedEventArgs e) => Evaluate();

        public override string ToString() => $"{ExpressionOperations.GetExpressionSyntax(NodeType, Type, left, right)} {ToStringSuffix}";
    }
}
