using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Gear.Components
{
    public static class GenericOperations
    {
        internal static ConcurrentDictionary<(BinaryOperation operation, Type type), Delegate> CompiledBinaryOperationMethods = new ConcurrentDictionary<(BinaryOperation operation, Type type), Delegate>();

        public static T Add<T>(T a, T b) => ((Func<T, T, T>)CompiledBinaryOperationMethods.GetOrAdd((BinaryOperation.Add, typeof(T)), CompiledBinaryOperationMethodsValueFactory))(a, b);

        public static T Divide<T>(T a, T b) => ((Func<T, T, T>)CompiledBinaryOperationMethods.GetOrAdd((BinaryOperation.Divide, typeof(T)), CompiledBinaryOperationMethodsValueFactory))(a, b);

        public static T Multiply<T>(T a, T b) => ((Func<T, T, T>)CompiledBinaryOperationMethods.GetOrAdd((BinaryOperation.Multiply, typeof(T)), CompiledBinaryOperationMethodsValueFactory))(a, b);

        public static T Subtract<T>(T a, T b) => ((Func<T, T, T>)CompiledBinaryOperationMethods.GetOrAdd((BinaryOperation.Subtract, typeof(T)), CompiledBinaryOperationMethodsValueFactory))(a, b);

        internal static Delegate CompiledBinaryOperationMethodsValueFactory((BinaryOperation operation, Type type) key)
        {
            var (operation, type) = key;
            var leftHand = Expression.Parameter(type);
            var rightHand = Expression.Parameter(type);
            BinaryExpression math;
            switch (operation)
            {
                case BinaryOperation.Add:
                    math = Expression.Add(leftHand, rightHand);
                    break;
                case BinaryOperation.Divide:
                    math = Expression.Divide(leftHand, rightHand);
                    break;
                case BinaryOperation.Multiply:
                    math = Expression.Multiply(leftHand, rightHand);
                    break;
                case BinaryOperation.Subtract:
                    math = Expression.Subtract(leftHand, rightHand);
                    break;
                default:
                    throw new NotSupportedException();
            }
            return Expression.Lambda(math, leftHand, rightHand).Compile();
        }
    }

    public class GenericOperations<T>
    {
        public GenericOperations()
        {
            var type = typeof(T);
            add = (Func<T, T, T>)GenericOperations.CompiledBinaryOperationMethods.GetOrAdd((BinaryOperation.Add, type), GenericOperations.CompiledBinaryOperationMethodsValueFactory);
            divide = (Func<T, T, T>)GenericOperations.CompiledBinaryOperationMethods.GetOrAdd((BinaryOperation.Divide, type), GenericOperations.CompiledBinaryOperationMethodsValueFactory);
            multiply = (Func<T, T, T>)GenericOperations.CompiledBinaryOperationMethods.GetOrAdd((BinaryOperation.Multiply, type), GenericOperations.CompiledBinaryOperationMethodsValueFactory);
            subtract = (Func<T, T, T>)GenericOperations.CompiledBinaryOperationMethods.GetOrAdd((BinaryOperation.Subtract, type), GenericOperations.CompiledBinaryOperationMethodsValueFactory);
        }

        Func<T, T, T> add;
        Func<T, T, T> divide;
        Func<T, T, T> multiply;
        Func<T, T, T> subtract;

        public T Add(T a, T b) => add(a, b);

        public T Divide(T a, T b) => divide(a, b);

        public T Multiply(T a, T b) => multiply(a, b);

        public T Subtract(T a, T b) => subtract(a, b);
    }
}
