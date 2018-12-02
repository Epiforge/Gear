using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Gear.ActiveExpressions.Tests
{
    [TestFixture]
    class ExpressionOperations
    {
        static readonly ConcurrentDictionary<Type, object> ones = new ConcurrentDictionary<Type, object>();
        static readonly ConcurrentDictionary<Type, object> twos = new ConcurrentDictionary<Type, object>();

        static object CreateOne(Type type) => Expression.Lambda(Expression.Convert(Expression.Constant(1), type)).Compile().DynamicInvoke();

        static object CreateTwo(Type type) => Expression.Lambda(Expression.Convert(Expression.Constant(2), type)).Compile().DynamicInvoke();

        static object GetOne(Type type)
        {
            if (type == typeof(object))
                return new object();
            var condensedType = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) ? type.GenericTypeArguments[0] : type;
            if (condensedType == typeof(bool))
                return true;
            if (condensedType == typeof(DateTime))
                return new DateTime(2000, 1, 1);
            if (condensedType == typeof(TimeSpan))
                return TimeSpan.FromSeconds(1);
            return ones.GetOrAdd(type, CreateOne);
        }

        static object GetTwo(Type type)
        {
            if (type == typeof(object))
                return new object();
            var condensedType = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) ? type.GenericTypeArguments[0] : type;
            if (condensedType == typeof(bool))
                return false;
            if (condensedType == typeof(DateTime))
                return new DateTime(2001, 1, 1);
            if (condensedType == typeof(TimeSpan))
                return TimeSpan.FromSeconds(2);
            return twos.GetOrAdd(type, CreateTwo);
        }

        [Test]
        public void BinaryImplementations()
        {
            foreach (var (method, parameters, attribute) in typeof(ActiveExpressions.ExpressionOperations)
                .GetRuntimeMethods()
                .Select(method => (method: method, parameters: method.GetParameters()))
                .Where(t => t.parameters.Length == 2)
                .Select(t => (t.method, t.parameters, attribute: t.method.GetCustomAttribute<ExpressionOperationAttribute>()))
                .Where(t => t.attribute != null))
            {
                var firstParameterType = parameters[0].ParameterType;
                var secondParameterType = parameters[1].ParameterType;
                var firstParameter = Expression.Parameter(firstParameterType);
                var secondParameter = Expression.Parameter(secondParameterType);
                var isObjects = firstParameterType == typeof(object) && secondParameterType == typeof(object);
                Expression operation;
                switch (attribute.Type)
                {
                    case ExpressionType.Add:
                        operation = Expression.Add(firstParameter, secondParameter);
                        break;
                    case ExpressionType.AddChecked:
                        operation = Expression.AddChecked(firstParameter, secondParameter);
                        break;
                    case ExpressionType.And:
                        operation = Expression.And(firstParameter, secondParameter);
                        break;
                    case ExpressionType.Divide:
                        operation = Expression.Divide(firstParameter, secondParameter);
                        break;
                    case ExpressionType.Equal:
                        operation = Expression.Equal(firstParameter, secondParameter);
                        break;
                    case ExpressionType.ExclusiveOr:
                        operation = Expression.ExclusiveOr(firstParameter, secondParameter);
                        break;
                    case ExpressionType.GreaterThan:
                        operation = Expression.GreaterThan(firstParameter, secondParameter);
                        break;
                    case ExpressionType.GreaterThanOrEqual:
                        operation = Expression.GreaterThanOrEqual(firstParameter, secondParameter);
                        break;
                    case ExpressionType.LeftShift:
                        operation = Expression.LeftShift(firstParameter, secondParameter);
                        break;
                    case ExpressionType.LessThan:
                        operation = Expression.LessThan(firstParameter, secondParameter);
                        break;
                    case ExpressionType.LessThanOrEqual:
                        operation = Expression.LessThanOrEqual(firstParameter, secondParameter);
                        break;
                    case ExpressionType.Modulo:
                        operation = Expression.Modulo(firstParameter, secondParameter);
                        break;
                    case ExpressionType.Multiply:
                        operation = Expression.Multiply(firstParameter, secondParameter);
                        break;
                    case ExpressionType.MultiplyChecked:
                        operation = Expression.MultiplyChecked(firstParameter, secondParameter);
                        break;
                    case ExpressionType.NotEqual:
                        operation = Expression.NotEqual(firstParameter, secondParameter);
                        break;
                    case ExpressionType.Or:
                        operation = Expression.Or(firstParameter, secondParameter);
                        break;
                    case ExpressionType.Power:
                        operation = Expression.Power(firstParameter, secondParameter);
                        break;
                    case ExpressionType.RightShift:
                        operation = Expression.RightShift(firstParameter, secondParameter);
                        break;
                    case ExpressionType.Subtract:
                        operation = Expression.Subtract(firstParameter, secondParameter);
                        break;
                    case ExpressionType.SubtractChecked:
                        operation = Expression.SubtractChecked(firstParameter, secondParameter);
                        break;
                    default:
                        throw new NotSupportedException();
                }
                var lambda = Expression.Lambda(operation, firstParameter, secondParameter);
                var compiled = lambda.Compile();
                var one = GetOne(firstParameterType);
                var two = GetTwo(secondParameterType);
                using (var expr = ActiveExpression.Create<object>(lambda, one, two))
                {
                    var compiledThrew = false;
                    object compiledValue = null;
                    try
                    {
                        compiledValue = compiled.DynamicInvoke(one, two);
                    }
                    catch (TargetInvocationException ex)
                    {
                        Assert.AreSame(ex.InnerException.GetType(), expr.Fault?.GetType());
                        compiledThrew = true;
                    }
                    if (!compiledThrew)
                        Assert.AreEqual(compiledValue, expr.Value);
                }
                if (firstParameterType.IsGenericType && firstParameterType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    using (var expr = ActiveExpression.Create<object>(lambda, null, two))
                    {
                        var compiledThrew = false;
                        object compiledValue = null;
                        try
                        {
                            compiledValue = compiled.DynamicInvoke(null, two);
                        }
                        catch (TargetInvocationException ex)
                        {
                            Assert.AreSame(ex.InnerException.GetType(), expr.Fault?.GetType());
                            compiledThrew = true;
                        }
                        if (!compiledThrew)
                            Assert.AreEqual(compiledValue, expr.Value);
                    }
                if (secondParameterType.IsGenericType && secondParameterType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    using (var expr = ActiveExpression.Create<object>(lambda, one, null))
                    {
                        var compiledThrew = false;
                        object compiledValue = null;
                        try
                        {
                            compiledValue = compiled.DynamicInvoke(one, null);
                        }
                        catch (TargetInvocationException ex)
                        {
                            Assert.AreSame(ex.InnerException.GetType(), expr.Fault?.GetType());
                            compiledThrew = true;
                        }
                        if (!compiledThrew)
                            Assert.AreEqual(compiledValue, expr.Value);
                    }
            }
        }

        [Test]
        public void UnaryImplementations()
        {
            foreach (var (method, parameters, attribute) in typeof(ActiveExpressions.ExpressionOperations)
                .GetRuntimeMethods()
                .Select(method => (method: method, parameters: method.GetParameters()))
                .Where(t => t.parameters.Length == 1)
                .Select(t => (t.method, t.parameters, attribute: t.method.GetCustomAttribute<ExpressionOperationAttribute>()))
                .Where(t => t.attribute != null && t.attribute.Type != ExpressionType.Convert))
            {
                var parameterType = parameters[0].ParameterType;
                var paramater = Expression.Parameter(parameterType);
                Expression operation;
                switch (attribute.Type)
                {
                    case ExpressionType.Decrement:
                        operation = Expression.Decrement(paramater);
                        break;
                    case ExpressionType.Increment:
                        operation = Expression.Increment(paramater);
                        break;
                    case ExpressionType.Negate:
                        operation = Expression.Negate(paramater);
                        break;
                    case ExpressionType.Not:
                        operation = Expression.Not(paramater);
                        break;
                    case ExpressionType.OnesComplement:
                        operation = Expression.OnesComplement(paramater);
                        break;
                    case ExpressionType.UnaryPlus:
                        operation = Expression.UnaryPlus(paramater);
                        break;
                    default:
                        throw new NotSupportedException();
                }
                var lambda = Expression.Lambda(operation, paramater);
                var compiled = lambda.Compile();
                var one = GetOne(parameterType);
                using (var expr = ActiveExpression.Create<object>(lambda, one))
                {
                    var compiledThrew = false;
                    object compiledValue = null;
                    try
                    {
                        compiledValue = compiled.DynamicInvoke(one);
                    }
                    catch (TargetInvocationException ex)
                    {
                        Assert.AreSame(ex.InnerException.GetType(), expr.Fault?.GetType());
                        compiledThrew = true;
                    }
                    if (!compiledThrew)
                        Assert.AreEqual(compiledValue, expr.Value);
                }
                if (parameterType.IsGenericType && parameterType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    using (var expr = ActiveExpression.Create<object>(lambda, new object[] { null }))
                    {
                        var compiledThrew = false;
                        object compiledValue = null;
                        try
                        {
                            compiledValue = compiled.DynamicInvoke(new object[] { null });
                        }
                        catch (TargetInvocationException ex)
                        {
                            Assert.AreSame(ex.InnerException.GetType(), expr.Fault?.GetType());
                            compiledThrew = true;
                        }
                        if (!compiledThrew)
                            Assert.AreEqual(compiledValue, expr.Value);
                    }
            }
        }
    }
}
