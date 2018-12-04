using Gear.Components;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Gear.ActiveExpressions
{
    public class ActiveExpressionOptions
    {
        static ActiveExpressionOptions() => Default = new ActiveExpressionOptions();

        public static ActiveExpressionOptions Default { get; }

        public static bool operator ==(ActiveExpressionOptions a, ActiveExpressionOptions b) =>
            a?.DisposeConstructedObjects == b?.DisposeConstructedObjects &&
            a?.DisposeStaticMethodReturnValues == b?.DisposeStaticMethodReturnValues &&
            (a?.disposeConstructedTypes.OrderBy(kv => kv.Key).Select(kv => (key: kv.Key, value: kv.Value)) ?? Enumerable.Empty<((Type type, EquatableList<Type> constuctorParameterTypes) key, bool value)>()).SequenceEqual(b?.disposeConstructedTypes.OrderBy(kv => kv.Key).Select(kv => (key: kv.Key, value: kv.Value)) ?? Enumerable.Empty<((Type type, EquatableList<Type> constuctorParameterTypes) key, bool value)>()) &&
            (a?.disposeMethodReturnValues.OrderBy(kv => kv.Key).Select(kv => (key: kv.Key, value: kv.Value)) ?? Enumerable.Empty<(MethodInfo key, bool value)>()).SequenceEqual(b?.disposeMethodReturnValues.OrderBy(kv => kv.Key).Select(kv => (key: kv.Key, value: kv.Value)) ?? Enumerable.Empty<(MethodInfo key, bool value)>());

        public static bool operator !=(ActiveExpressionOptions a, ActiveExpressionOptions b) =>
            a?.DisposeConstructedObjects != b?.DisposeConstructedObjects ||
            a?.DisposeStaticMethodReturnValues != b?.DisposeStaticMethodReturnValues ||
            !(a?.disposeConstructedTypes.OrderBy(kv => kv.Key).Select(kv => (key: kv.Key, value: kv.Value)) ?? Enumerable.Empty<((Type type, EquatableList<Type> constuctorParameterTypes) key, bool value)>()).SequenceEqual(b?.disposeConstructedTypes.OrderBy(kv => kv.Key).Select(kv => (key: kv.Key, value: kv.Value)) ?? Enumerable.Empty<((Type type, EquatableList<Type> constuctorParameterTypes) key, bool value)>()) ||
            !(a?.disposeMethodReturnValues.OrderBy(kv => kv.Key).Select(kv => (key: kv.Key, value: kv.Value)) ?? Enumerable.Empty<(MethodInfo key, bool value)>()).SequenceEqual(b?.disposeMethodReturnValues.OrderBy(kv => kv.Key).Select(kv => (key: kv.Key, value: kv.Value)) ?? Enumerable.Empty<(MethodInfo key, bool value)>());

        public ActiveExpressionOptions()
        {
            DisposeConstructedObjects = true;
            DisposeStaticMethodReturnValues = true;
        }

        readonly ConcurrentDictionary<(Type type, EquatableList<Type> constuctorParameterTypes), bool> disposeConstructedTypes = new ConcurrentDictionary<(Type type, EquatableList<Type> constuctorParameterTypes), bool>();
        readonly ConcurrentDictionary<MethodInfo, bool> disposeMethodReturnValues = new ConcurrentDictionary<MethodInfo, bool>();
        bool isFrozen;

        public bool AddConstructedTypeDisposal(Type type, params Type[] constuctorParameterTypes)
        {
            RequireUnfrozen();
            return disposeConstructedTypes.TryAdd((type, new EquatableList<Type>(constuctorParameterTypes)), true);
        }

        public bool AddConstructedTypeDisposal(ConstructorInfo constructor)
        {
            RequireUnfrozen();
            return disposeConstructedTypes.TryAdd((constructor.DeclaringType, new EquatableList<Type>(constructor.GetParameters().Select(parameterInfo => parameterInfo.ParameterType).ToList())), true);
        }

        public bool AddExpressionValueDisposal<T>(Expression<Func<T>> lambda)
        {
            RequireUnfrozen();
            switch (lambda.Body)
            {
                case BinaryExpression binary:
                    return AddMethodReturnValueDisposal(binary.Method);
                case MethodCallExpression methodCall:
                    return AddMethodReturnValueDisposal(methodCall.Method);
                case UnaryExpression unary:
                    return AddMethodReturnValueDisposal(unary.Method);
                default:
                    throw new ArgumentException("Expression type not supported", nameof(lambda));
            }
        }

        public bool AddMethodReturnValueDisposal(MethodInfo method)
        {
            RequireUnfrozen();
            return disposeMethodReturnValues.TryAdd(method, true);
        }

        public bool AddPropertyValueDisposal(PropertyInfo property)
        {
            RequireUnfrozen();
            return AddMethodReturnValueDisposal(property.GetMethod);
        }

        public override bool Equals(object obj) => obj is ActiveExpressionOptions other && this == other;

        internal void Freeze() => isFrozen = true;

        public override int GetHashCode()
        {
            if (!isFrozen)
                return base.GetHashCode();
            var objects = new List<object>() { DisposeConstructedObjects, DisposeStaticMethodReturnValues };
            objects.AddRange(disposeConstructedTypes.OrderBy(kv => kv.Key).Select(kv => (key: kv.Key, value: kv.Value)).Cast<object>());
            objects.AddRange(disposeConstructedTypes.OrderBy(kv => kv.Key).Select(kv => (key: kv.Key, value: kv.Value)).Cast<object>());
            return HashCodes.CombineObjects(objects.ToArray());
        }

        internal bool IsConstructedTypeDisposed(Type type, EquatableList<Type> constructorParameterTypes) => DisposeConstructedObjects || disposeConstructedTypes.ContainsKey((type, constructorParameterTypes));

        public bool IsConstructedTypeDisposed(Type type, params Type[] constructorParameterTypes) => DisposeConstructedObjects || IsConstructedTypeDisposed(type, new EquatableList<Type>(constructorParameterTypes));

        public bool IsConstructedTypeDisposed(ConstructorInfo constructor) => disposeConstructedTypes.ContainsKey((constructor.DeclaringType, new EquatableList<Type>(constructor.GetParameters().Select(parameterInfo => parameterInfo.ParameterType).ToList())));

        public bool IsExpressionValueDisposal<T>(Expression<Func<T>> lambda)
        {
            switch (lambda.Body)
            {
                case BinaryExpression binary:
                    return IsMethodReturnValueDisposed(binary.Method);
                case MethodCallExpression methodCall:
                    return IsMethodReturnValueDisposed(methodCall.Method);
                case UnaryExpression unary:
                    return IsMethodReturnValueDisposed(unary.Method);
                default:
                    throw new ArgumentException("Expression type not supported", nameof(lambda));
            }
        }

        public bool IsMethodReturnValueDisposed(MethodInfo method) => (method.IsStatic && DisposeStaticMethodReturnValues) || disposeMethodReturnValues.ContainsKey(method);

        public bool IsPropertyValueDisposed(PropertyInfo property) => IsMethodReturnValueDisposed(property.GetMethod);

        public bool RemoveConstructedTypeDisposal(Type type, params Type[] constuctorParameterTypes)
        {
            RequireUnfrozen();
            return disposeConstructedTypes.TryRemove((type, new EquatableList<Type>(constuctorParameterTypes)), out var discard);
        }

        public bool RemoveConstructedTypeDisposal(ConstructorInfo constructor)
        {
            RequireUnfrozen();
            return disposeConstructedTypes.TryRemove((constructor.DeclaringType, new EquatableList<Type>(constructor.GetParameters().Select(parameterInfo => parameterInfo.ParameterType).ToList())), out var discard);
        }

        public bool RemoveExpressionValueDisposal<T>(Expression<Func<T>> lambda)
        {
            RequireUnfrozen();
            switch (lambda.Body)
            {
                case BinaryExpression binary:
                    return RemoveMethodReturnValueDisposal(binary.Method);
                case MethodCallExpression methodCall:
                    return RemoveMethodReturnValueDisposal(methodCall.Method);
                case UnaryExpression unary:
                    return RemoveMethodReturnValueDisposal(unary.Method);
                default:
                    throw new ArgumentException("Expression type not supported", nameof(lambda));
            }
        }

        public bool RemoveMethodReturnValueDisposal(MethodInfo method)
        {
            RequireUnfrozen();
            return disposeMethodReturnValues.TryRemove(method, out var discard);
        }

        public bool RemovePropertyValueDisposal(PropertyInfo property)
        {
            RequireUnfrozen();
            return IsMethodReturnValueDisposed(property.GetMethod);
        }

        void RequireUnfrozen()
        {
            if (isFrozen)
                throw new InvalidOperationException();
        }

        public bool DisposeConstructedObjects { get; set; }

        public bool DisposeStaticMethodReturnValues { get; set; }
    }
}
