using Gear.Components;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace Gear.ActiveExpressions
{
    class ActiveUnaryExpression : ActiveExpression
    {
        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<(ExpressionType nodeType, ActiveExpression opperand, Type type, MethodInfo method), ActiveUnaryExpression> instances = new Dictionary<(ExpressionType nodeType, ActiveExpression opperand, Type type, MethodInfo method), ActiveUnaryExpression>();
        static readonly ConcurrentDictionary<Type, MethodInfo> nullableConversions = new ConcurrentDictionary<Type, MethodInfo>();

        static MethodInfo GetNullableConversionMethodInfo(Type nullableType) => nullableType.GetRuntimeMethod("op_Implicit", new Type[] { nullableType.GenericTypeArguments[0] });

        public static ActiveUnaryExpression Create(UnaryExpression unaryExpression)
        {
            var nodeType = unaryExpression.NodeType;
            var operand = Create(unaryExpression.Operand);
            var type = unaryExpression.Type;
            var method = unaryExpression.Method;
            var key = (nodeType, operand, type, method);
            lock (instanceManagementLock)
            {
                if (!instances.TryGetValue(key, out var activeUnaryExpression))
                {
                    activeUnaryExpression = new ActiveUnaryExpression(nodeType, operand, type, method);
                    instances.Add(key, activeUnaryExpression);
                }
                ++activeUnaryExpression.disposalCount;
                return activeUnaryExpression;
            }
        }

        ActiveUnaryExpression(ExpressionType nodeType, ActiveExpression operand, Type type, MethodInfo method) : base(type, nodeType)
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
            Evaluate();
        }

        int disposalCount;
        readonly FastMethodInfo fastMethod;
        readonly MethodInfo method;
        readonly ExpressionType nodeType;
        readonly ActiveExpression operand;

        protected override bool Dispose(bool disposing)
        {
            lock (instanceManagementLock)
            {
                if (--disposalCount > 0)
                    return false;
                operand.PropertyChanged -= OperandPropertyChanged;
                operand.Dispose();
                instances.Remove((nodeType, operand, Type, method));
                return true;
            }
        }

        void Evaluate()
        {
            try
            {
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

        void OperandPropertyChanged(object sender, PropertyChangedEventArgs e) => Evaluate();
    }
}
