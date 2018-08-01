using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Gear.Components
{
    public static class GenericMath
    {
        internal static ConcurrentDictionary<(MathBinaryOperation operation, Type type), Delegate> CompiledBinaryOperationMethods = new ConcurrentDictionary<(MathBinaryOperation operation, Type type), Delegate>();

        public static T Add<T>(T a, T b) => ((Func<T, T, T>)CompiledBinaryOperationMethods.GetOrAdd((MathBinaryOperation.Add, typeof(T)), CompiledBinaryOperationMethodsValueFactory))(a, b);

        public static T Divide<T>(T a, T b) => ((Func<T, T, T>)CompiledBinaryOperationMethods.GetOrAdd((MathBinaryOperation.Divide, typeof(T)), CompiledBinaryOperationMethodsValueFactory))(a, b);

        public static T Multiply<T>(T a, T b) => ((Func<T, T, T>)CompiledBinaryOperationMethods.GetOrAdd((MathBinaryOperation.Multiply, typeof(T)), CompiledBinaryOperationMethodsValueFactory))(a, b);

        public static T Subtract<T>(T a, T b) => ((Func<T, T, T>)CompiledBinaryOperationMethods.GetOrAdd((MathBinaryOperation.Subtract, typeof(T)), CompiledBinaryOperationMethodsValueFactory))(a, b);

        internal static Delegate CompiledBinaryOperationMethodsValueFactory((MathBinaryOperation operation, Type type) key)
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

    public class GenericMath<T>
    {
        public GenericMath()
        {
            var type = typeof(T);
            add = (Func<T, T, T>)GenericMath.CompiledBinaryOperationMethods.GetOrAdd((MathBinaryOperation.Add, type), GenericMath.CompiledBinaryOperationMethodsValueFactory);
            divide = (Func<T, T, T>)GenericMath.CompiledBinaryOperationMethods.GetOrAdd((MathBinaryOperation.Divide, type), GenericMath.CompiledBinaryOperationMethodsValueFactory);
            multiply = (Func<T, T, T>)GenericMath.CompiledBinaryOperationMethods.GetOrAdd((MathBinaryOperation.Multiply, type), GenericMath.CompiledBinaryOperationMethodsValueFactory);
            subtract = (Func<T, T, T>)GenericMath.CompiledBinaryOperationMethods.GetOrAdd((MathBinaryOperation.Subtract, type), GenericMath.CompiledBinaryOperationMethodsValueFactory);
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
