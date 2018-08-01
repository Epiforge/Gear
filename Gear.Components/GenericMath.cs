using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Gear.Components
{
    public static class GenericMath
    {
        static ConcurrentDictionary<(MathBinaryOperation operation, Type type), Delegate> compiledBinaryOperationMethods = new ConcurrentDictionary<(MathBinaryOperation operation, Type type), Delegate>();

        public static T Add<T>(T a, T b) => ((Func<T, T, T>)compiledBinaryOperationMethods.GetOrAdd((MathBinaryOperation.Add, typeof(T)), CompiledBinaryOperationMethodsValueFactory))(a, b);

        public static T Divide<T>(T a, T b) => ((Func<T, T, T>)compiledBinaryOperationMethods.GetOrAdd((MathBinaryOperation.Divide, typeof(T)), CompiledBinaryOperationMethodsValueFactory))(a, b);

        public static T Multiply<T>(T a, T b) => ((Func<T, T, T>)compiledBinaryOperationMethods.GetOrAdd((MathBinaryOperation.Multiply, typeof(T)), CompiledBinaryOperationMethodsValueFactory))(a, b);

        public static T Subtract<T>(T a, T b) => ((Func<T, T, T>)compiledBinaryOperationMethods.GetOrAdd((MathBinaryOperation.Subtract, typeof(T)), CompiledBinaryOperationMethodsValueFactory))(a, b);

        static Delegate CompiledBinaryOperationMethodsValueFactory((MathBinaryOperation operation, Type type) key)
        {
            var (operation, type) = key;
            var leftHand = Expression.Parameter(type);
            var rightHand = Expression.Parameter(type);
            BinaryExpression math;
            switch (operation)
            {
                case MathBinaryOperation.Add:
                    math = Expression.Add(leftHand, rightHand);
                    break;
                case MathBinaryOperation.Divide:
                    math = Expression.Divide(leftHand, rightHand);
                    break;
                case MathBinaryOperation.Multiply:
                    math = Expression.Multiply(leftHand, rightHand);
                    break;
                case MathBinaryOperation.Subtract:
                    math = Expression.Subtract(leftHand, rightHand);
                    break;
                default:
                    throw new NotSupportedException();
            }
            return Expression.Lambda(math, leftHand, rightHand).Compile();
        }
    }
}
