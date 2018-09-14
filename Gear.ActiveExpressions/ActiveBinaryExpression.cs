using Gear.Components;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace Gear.ActiveExpressions
{
    class ActiveBinaryExpression : ActiveExpression, IEquatable<ActiveBinaryExpression>
    {
        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<(ExpressionType nodeType, ActiveExpression left, ActiveExpression right), ActiveBinaryExpression> factoryInstances = new Dictionary<(ExpressionType nodeType, ActiveExpression left, ActiveExpression right), ActiveBinaryExpression>();
        static readonly Dictionary<(ExpressionType nodeType, ActiveExpression left, ActiveExpression right, bool isLiftedToNull, MethodInfo method), ActiveBinaryExpression> implementationInstances = new Dictionary<(ExpressionType nodeType, ActiveExpression left, ActiveExpression right, bool isLiftedToNull, MethodInfo method), ActiveBinaryExpression>();

        public static ActiveBinaryExpression Create(BinaryExpression binaryExpression)
        {
            var type = binaryExpression.Type;
            var nodeType = binaryExpression.NodeType;
            var left = Create(binaryExpression.Left);
            var right = Create(binaryExpression.Right);
            var method = binaryExpression.Method;
            if (method == null)
            {
                var key = (nodeType, left, right);
                lock (instanceManagementLock)
                {
                    if (!factoryInstances.TryGetValue(key, out var activeBinaryExpression))
                    {
                        switch (nodeType)
                        {
                            case ExpressionType.AndAlso when type == typeof(bool):
                                activeBinaryExpression = new ActiveAndAlsoExpression(left, right);
                                break;
                            case ExpressionType.Coalesce:
                                activeBinaryExpression = new ActiveCoalesceExpression(type, left, right, binaryExpression.Conversion);
                                break;
                            case ExpressionType.OrElse when type == typeof(bool):
                                activeBinaryExpression = new ActiveOrElseExpression(left, right);
                                break;
                            default:
                                activeBinaryExpression = new ActiveBinaryExpression(type, nodeType, left, right);
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
                var isLiftedToNull = binaryExpression.IsLiftedToNull;
                var conversion = binaryExpression.Conversion;
                if (conversion == null)
                {
                    var key = (nodeType, left, right, isLiftedToNull, method);
                    lock (instanceManagementLock)
                    {
                        if (!implementationInstances.TryGetValue(key, out var activeBinaryExpression))
                        {
                            activeBinaryExpression = new ActiveBinaryExpression(type, nodeType, left, right, isLiftedToNull, method);
                            implementationInstances.Add(key, activeBinaryExpression);
                        }
                        ++activeBinaryExpression.disposalCount;
                        return activeBinaryExpression;
                    }
                }
            }
            throw new NotSupportedException("ActiveBinaryExpressions do not yet support BinaryExpressions using Conversions");
        }

        public static bool operator ==(ActiveBinaryExpression a, ActiveBinaryExpression b) => a?.Equals(b) ?? b == null;

        public static bool operator !=(ActiveBinaryExpression a, ActiveBinaryExpression b) => !(a == b);

        protected ActiveBinaryExpression(Type type, ExpressionType nodeType, ActiveExpression left, ActiveExpression right, bool getOperation = true) : base(type, nodeType)
        {
            this.nodeType = nodeType;
            this.left = left;
            this.left.PropertyChanged += LeftPropertyChanged;
            this.right = right;
            this.right.PropertyChanged += RightPropertyChanged;
            if (getOperation)
                fastMethod = ExpressionOperations.GetFastMethodInfo(nodeType, type, left.Type, right.Type) ?? throw new NotSupportedException($"There is no implementation of {nodeType} available that accepts a(n) {left.Type.Name} left-hand operand and a(n) {right.Type.Name} right-hand operand and which returns a(n) {type.Name}");
            Evaluate();
        }

        ActiveBinaryExpression(Type type, ExpressionType nodeType, ActiveExpression left, ActiveExpression right, bool isLiftedToNull, MethodInfo method) : base(type, nodeType)
        {
            this.left = left;
            this.left.PropertyChanged += LeftPropertyChanged;
            this.right = right;
            this.right.PropertyChanged += RightPropertyChanged;
            this.isLiftedToNull = isLiftedToNull;
            this.method = method;
            fastMethod = GetFastMethodInfo(this.method);
            Evaluate();
        }

        int disposalCount;
        readonly FastMethodInfo fastMethod;
        readonly bool isLiftedToNull;
        protected readonly ActiveExpression left;
        readonly MethodInfo method;
        readonly ExpressionType nodeType;
        protected readonly ActiveExpression right;

        protected override bool Dispose(bool disposing)
        {
            lock (instanceManagementLock)
            {
                if (--disposalCount > 0)
                    return false;
                left.PropertyChanged -= LeftPropertyChanged;
                left.Dispose();
                right.PropertyChanged -= RightPropertyChanged;
                right.Dispose();
                if (method == null)
                    factoryInstances.Remove((nodeType, left, right));
                else
                    implementationInstances.Remove((nodeType, left, right, isLiftedToNull, method));
                return true;
            }
        }

        public override bool Equals(object obj) => Equals(obj as ActiveBinaryExpression);

        public bool Equals(ActiveBinaryExpression other) => other?.left == left && other?.method == method && other?.nodeType == nodeType && other?.right == right;

        protected virtual void Evaluate()
        {
            var leftFault = left.Fault;
            var leftValue = left.Value;
            var rightFault = right.Fault;
            var rightValue = right.Value;
            try
            {
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

        public override int GetHashCode() => HashCodes.CombineObjects(typeof(ActiveBinaryExpression), left, method, nodeType, right);

        void LeftPropertyChanged(object sender, PropertyChangedEventArgs e) => Evaluate();

        void RightPropertyChanged(object sender, PropertyChangedEventArgs e) => Evaluate();
    }
}
