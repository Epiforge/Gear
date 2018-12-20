using Gear.Components;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Gear.ActiveExpressions
{
    /// <summary>
    /// Represents certain options governing the behavior of active expressions
    /// </summary>
    public class ActiveExpressionOptions
    {
        static ActiveExpressionOptions() => Default = new ActiveExpressionOptions();

        static readonly ConcurrentDictionary<MethodInfo, PropertyInfo> propertyGetMethodToProperty = new ConcurrentDictionary<MethodInfo, PropertyInfo>();

        static PropertyInfo GetPropertyFromGetMethod(MethodInfo getMethod) => getMethod.DeclaringType.GetRuntimeProperties().FirstOrDefault(property => property.GetMethod == getMethod);

        /// <summary>
        /// Determines whether two active expression options are the same
        /// </summary>
        /// <param name="a">The first options to compare, or null</param>
        /// <param name="b">The second options to compare, or null</param>
        /// <returns><c>true</c> is <paramref name="a"/> is the same as <paramref name="b"/>; otherwise, <c>false</c></returns>
        public static bool operator ==(ActiveExpressionOptions a, ActiveExpressionOptions b) =>
            a?.disposeConstructedObjects == b?.disposeConstructedObjects &&
            a?.disposeStaticMethodReturnValues == b?.disposeStaticMethodReturnValues &&
            (a?.disposeConstructedTypes.OrderBy(kv => $"{kv.Key.type}({string.Join(", ", kv.Key.constuctorParameterTypes.Select(p => p))})").Select(kv => (key: kv.Key, value: kv.Value)) ?? Enumerable.Empty<((Type type, EquatableList<Type> constuctorParameterTypes) key, bool value)>()).SequenceEqual(b?.disposeConstructedTypes.OrderBy(kv => $"{kv.Key.type}({string.Join(", ", kv.Key.constuctorParameterTypes.Select(p => p))})").Select(kv => (key: kv.Key, value: kv.Value)) ?? Enumerable.Empty<((Type type, EquatableList<Type> constuctorParameterTypes) key, bool value)>()) &&
            (a?.disposeMethodReturnValues.OrderBy(kv => $"{kv.Key.DeclaringType.FullName}.{kv.Key.Name}({string.Join(", ", kv.Key.GetParameters().Select(p => p.ParameterType))})").Select(kv => (key: kv.Key, value: kv.Value)) ?? Enumerable.Empty<(MethodInfo key, bool value)>()).SequenceEqual(b?.disposeMethodReturnValues.OrderBy(kv => $"{kv.Key.DeclaringType.FullName}.{kv.Key.Name}({string.Join(", ", kv.Key.GetParameters().Select(p => p.ParameterType))})").Select(kv => (key: kv.Key, value: kv.Value)) ?? Enumerable.Empty<(MethodInfo key, bool value)>());

        /// <summary>
        /// Determines whether two active expression options are different
        /// </summary>
        /// <param name="a">The first options to compare, or null</param>
        /// <param name="b">The second options to compare, or null</param>
        /// <returns><c>true</c> is <paramref name="a"/> is different from <paramref name="b"/>; otherwise, <c>false</c></returns>
        public static bool operator !=(ActiveExpressionOptions a, ActiveExpressionOptions b) =>
            a?.DisposeConstructedObjects != b?.DisposeConstructedObjects ||
            a?.DisposeStaticMethodReturnValues != b?.DisposeStaticMethodReturnValues ||
            !(a?.disposeConstructedTypes.OrderBy(kv => $"{kv.Key.type}({string.Join(", ", kv.Key.constuctorParameterTypes.Select(p => p))})").Select(kv => (key: kv.Key, value: kv.Value)) ?? Enumerable.Empty<((Type type, EquatableList<Type> constuctorParameterTypes) key, bool value)>()).SequenceEqual(b?.disposeConstructedTypes.OrderBy(kv => $"{kv.Key.type}({string.Join(", ", kv.Key.constuctorParameterTypes.Select(p => p))})").Select(kv => (key: kv.Key, value: kv.Value)) ?? Enumerable.Empty<((Type type, EquatableList<Type> constuctorParameterTypes) key, bool value)>()) ||
            !(a?.disposeMethodReturnValues.OrderBy(kv => $"{kv.Key.DeclaringType.FullName}.{kv.Key.Name}({string.Join(", ", kv.Key.GetParameters().Select(p => p.ParameterType))})").Select(kv => (key: kv.Key, value: kv.Value)) ?? Enumerable.Empty<(MethodInfo key, bool value)>()).SequenceEqual(b?.disposeMethodReturnValues.OrderBy(kv => $"{kv.Key.DeclaringType.FullName}.{kv.Key.Name}({string.Join(", ", kv.Key.GetParameters().Select(p => p.ParameterType))})").Select(kv => (key: kv.Key, value: kv.Value)) ?? Enumerable.Empty<(MethodInfo key, bool value)>());

        /// <summary>
        /// Gets the default active expression options, which are used in lieu of specified options when an active expression is created
        /// </summary>
        public static ActiveExpressionOptions Default { get; }

        public ActiveExpressionOptions()
        {
            DisposeConstructedObjects = true;
            DisposeStaticMethodReturnValues = true;
        }

        bool disposeConstructedObjects;
        readonly ConcurrentDictionary<(Type type, EquatableList<Type> constuctorParameterTypes), bool> disposeConstructedTypes = new ConcurrentDictionary<(Type type, EquatableList<Type> constuctorParameterTypes), bool>();
        readonly ConcurrentDictionary<MethodInfo, bool> disposeMethodReturnValues = new ConcurrentDictionary<MethodInfo, bool>();
        bool disposeStaticMethodReturnValues;
        bool isFrozen;

        /// <summary>
        /// Specifies that active expressions using these options should dispose of objects they have created of the specified type and using constructor arguments of the specified types when the objects are replaced or otherwise discarded
        /// </summary>
        /// <param name="type">The type of object created</param>
        /// <param name="constuctorParameterTypes">The types of the arguments passed to the constructor, in order</param>
        /// <returns><c>true</c> if this has resulted in a change in the options; otherwise, <c>false</c></returns>
        public bool AddConstructedTypeDisposal(Type type, params Type[] constuctorParameterTypes)
        {
            RequireUnfrozen();
            return disposeConstructedTypes.TryAdd((type, new EquatableList<Type>(constuctorParameterTypes)), true);
        }

        /// <summary>
        /// Specifies that active expressions using these options should dispose of objects they have created using the specified constructor when the objects are replaced or otherwise discarded
        /// </summary>
        /// <param name="constructor">The constructor</param>
        /// <returns><c>true</c> if this has resulted in a change in the options; otherwise, <c>false</c></returns>
        public bool AddConstructedTypeDisposal(ConstructorInfo constructor)
        {
            RequireUnfrozen();
            return disposeConstructedTypes.TryAdd((constructor.DeclaringType, new EquatableList<Type>(constructor.GetParameters().Select(parameterInfo => parameterInfo.ParameterType).ToList())), true);
        }

        /// <summary>
        /// Specifies that active expressions using these options should dispose of objects they have received as a result of invoking a constructor, operator, or method, or getting the value of a property or indexer when the objects are replaced or otherwise discarded
        /// </summary>
        /// <typeparam name="T">The type of the objects</typeparam>
        /// <param name="lambda">An expression indicating the kind of behavior that is yielding the objects</param>
        /// <returns><c>true</c> if this has resulted in a change in the options; otherwise, <c>false</c></returns>
        public bool AddExpressionValueDisposal<T>(Expression<Func<T>> lambda)
        {
            RequireUnfrozen();
            switch (lambda.Body)
            {
                case BinaryExpression binary:
                    return AddMethodReturnValueDisposal(binary.Method);
                case IndexExpression index:
                    return AddPropertyValueDisposal(index.Indexer);
                case NewExpression @new:
                    return AddConstructedTypeDisposal(@new.Constructor);
                case MemberExpression member when member.Member is PropertyInfo property:
                    return AddPropertyValueDisposal(property);
                case MethodCallExpression methodCallExpressionForPropertyGet when propertyGetMethodToProperty.GetOrAdd(methodCallExpressionForPropertyGet.Method, GetPropertyFromGetMethod) is PropertyInfo property:
                    return AddPropertyValueDisposal(property);
                case MethodCallExpression methodCall:
                    return AddMethodReturnValueDisposal(methodCall.Method);
                case UnaryExpression unary:
                    return AddMethodReturnValueDisposal(unary.Method);
                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Specifies that active expressions using these options should dispose of objects they have received as a result of invoking a specified method when the objects are replaced or otherwise discarded
        /// </summary>
        /// <param name="method">The method yielding the objects</param>
        /// <returns><c>true</c> if this has resulted in a change in the options; otherwise, <c>false</c></returns>
        public bool AddMethodReturnValueDisposal(MethodInfo method)
        {
            RequireUnfrozen();
            return disposeMethodReturnValues.TryAdd(method, true);
        }

        /// <summary>
        /// Specifies that active expressions using these options should dispose of objects they have received as a result of getting the value of a specified property when the objects are replaced or otherwise discarded
        /// </summary>
        /// <param name="property">The property yielding the objects</param>
        /// <returns><c>true</c> if this has resulted in a change in the options; otherwise, <c>false</c></returns>
        public bool AddPropertyValueDisposal(PropertyInfo property)
        {
            RequireUnfrozen();
            return AddMethodReturnValueDisposal(property.GetMethod);
        }

        public override bool Equals(object obj) => obj is ActiveExpressionOptions other && this == other;

        internal void Freeze() => isFrozen = true;

        public override int GetHashCode()
        {
            if (Default == this)
                return base.GetHashCode();
            Freeze();
            var objects = new List<object>() { disposeConstructedObjects, disposeStaticMethodReturnValues };
            objects.AddRange(disposeConstructedTypes.OrderBy(kv => $"{kv.Key.type}({string.Join(", ", kv.Key.constuctorParameterTypes.Select(p => p))})").Select(kv => (key: kv.Key, value: kv.Value)).Cast<object>());
            objects.AddRange(disposeMethodReturnValues.OrderBy(kv => $"{kv.Key.DeclaringType.FullName}.{kv.Key.Name}({string.Join(", ", kv.Key.GetParameters().Select(p => p.ParameterType))})").Select(kv => (key: kv.Key, value: kv.Value)).Cast<object>());
            return HashCodes.CombineObjects(objects.ToArray());
        }

        internal bool IsConstructedTypeDisposed(Type type, EquatableList<Type> constructorParameterTypes) => DisposeConstructedObjects || disposeConstructedTypes.ContainsKey((type, constructorParameterTypes));

        /// <summary>
        /// Gets whether active expressions using these options should dispose of objects they have created of the specified type and using constructor arguments of the specified types when the objects are replaced or otherwise discarded
        /// </summary>
        /// <param name="type">The type of object created</param>
        /// <param name="constuctorParameterTypes">The types of the arguments passed to the constructor, in order</param>
        /// <returns><c>true</c> if objects from this source should be disposed; otherwise, <c>false</c></returns>
        public bool IsConstructedTypeDisposed(Type type, params Type[] constructorParameterTypes) => DisposeConstructedObjects || IsConstructedTypeDisposed(type, new EquatableList<Type>(constructorParameterTypes));

        /// <summary>
        /// Gets whether active expressions using these options should dispose of objects they have created using the specified constructor when the objects are replaced or otherwise discarded
        /// </summary>
        /// <param name="constructor">The constructor</param>
        /// <returns><c>true</c> if objects from this source should be disposed; otherwise, <c>false</c></returns>
        public bool IsConstructedTypeDisposed(ConstructorInfo constructor) => disposeConstructedTypes.ContainsKey((constructor.DeclaringType, new EquatableList<Type>(constructor.GetParameters().Select(parameterInfo => parameterInfo.ParameterType).ToList())));

        /// <summary>
        /// Gets whether active expressions using these options should dispose of objects they have received as a result of invoking a constructor, operator, or method, or getting the value of a property or indexer when the objects are replaced or otherwise discarded
        /// </summary>
        /// <typeparam name="T">The type of the objects</typeparam>
        /// <param name="lambda">An expression indicating the kind of behavior that is yielding the objects</param>
        /// <returns><c>true</c> if objects from this source should be disposed; otherwise, <c>false</c></returns>
        public bool IsExpressionValueDisposed<T>(Expression<Func<T>> lambda)
        {
            switch (lambda.Body)
            {
                case BinaryExpression binary:
                    return IsMethodReturnValueDisposed(binary.Method);
                case IndexExpression index:
                    return IsPropertyValueDisposed(index.Indexer);
                case NewExpression @new:
                    return IsConstructedTypeDisposed(@new.Constructor);
                case MemberExpression member when member.Member is PropertyInfo property:
                    return IsPropertyValueDisposed(property);
                case MethodCallExpression methodCallExpressionForPropertyGet when propertyGetMethodToProperty.GetOrAdd(methodCallExpressionForPropertyGet.Method, GetPropertyFromGetMethod) is PropertyInfo property:
                    return IsPropertyValueDisposed(property);
                case MethodCallExpression methodCall:
                    return IsMethodReturnValueDisposed(methodCall.Method);
                case UnaryExpression unary:
                    return IsMethodReturnValueDisposed(unary.Method);
                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Gets whether active expressions using these options should dispose of objects they have received as a result of invoking a specified method when the objects are replaced or otherwise discarded
        /// </summary>
        /// <param name="method">The method yielding the objects</param>
        /// <returns><c>true</c> if objects from this source should be disposed; otherwise, <c>false</c></returns>
        public bool IsMethodReturnValueDisposed(MethodInfo method) => (method.IsStatic && DisposeStaticMethodReturnValues) || disposeMethodReturnValues.ContainsKey(method);

        /// <summary>
        /// Gets whether active expressions using these options should dispose of objects they have received as a result of getting the value of a specified property when the objects are replaced or otherwise discarded
        /// </summary>
        /// <param name="property">The property yielding the objects</param>
        /// <returns><c>true</c> if objects from this source should be disposed; otherwise, <c>false</c></returns>
        public bool IsPropertyValueDisposed(PropertyInfo property) => IsMethodReturnValueDisposed(property.GetMethod);

        /// <summary>
        /// Specifies that active expressions using these options should not dispose of objects they have created of the specified type and using constructor arguments of the specified types when the objects are replaced or otherwise discarded
        /// </summary>
        /// <param name="type">The type of object created</param>
        /// <param name="constuctorParameterTypes">The types of the arguments passed to the constructor, in order</param>
        /// <returns><c>true</c> if this has resulted in a change in the options; otherwise, <c>false</c></returns>
        public bool RemoveConstructedTypeDisposal(Type type, params Type[] constuctorParameterTypes)
        {
            RequireUnfrozen();
            return disposeConstructedTypes.TryRemove((type, new EquatableList<Type>(constuctorParameterTypes)), out var discard);
        }

        /// <summary>
        /// Specifies that active expressions using these options should not dispose of objects they have created using the specified constructor when the objects are replaced or otherwise discarded
        /// </summary>
        /// <param name="constructor">The constructor</param>
        /// <returns><c>true</c> if this has resulted in a change in the options; otherwise, <c>false</c></returns>
        public bool RemoveConstructedTypeDisposal(ConstructorInfo constructor)
        {
            RequireUnfrozen();
            return disposeConstructedTypes.TryRemove((constructor.DeclaringType, new EquatableList<Type>(constructor.GetParameters().Select(parameterInfo => parameterInfo.ParameterType).ToList())), out var discard);
        }

        /// <summary>
        /// Specifies that active expressions using these options should not dispose of objects they have received as a result of invoking a constructor, operator, or method, or getting the value of a property or indexer when the objects are replaced or otherwise discarded
        /// </summary>
        /// <typeparam name="T">The type of the objects</typeparam>
        /// <param name="lambda">An expression indicating the kind of behavior that is yielding the objects</param>
        /// <returns><c>true</c> if this has resulted in a change in the options; otherwise, <c>false</c></returns>
        public bool RemoveExpressionValueDisposal<T>(Expression<Func<T>> lambda)
        {
            RequireUnfrozen();
            switch (lambda.Body)
            {
                case BinaryExpression binary:
                    return RemoveMethodReturnValueDisposal(binary.Method);
                case IndexExpression index:
                    return RemovePropertyValueDisposal(index.Indexer);
                case NewExpression @new:
                    return RemoveConstructedTypeDisposal(@new.Constructor);
                case MemberExpression member when member.Member is PropertyInfo property:
                    return RemovePropertyValueDisposal(property);
                case MethodCallExpression methodCallExpressionForPropertyGet when propertyGetMethodToProperty.GetOrAdd(methodCallExpressionForPropertyGet.Method, GetPropertyFromGetMethod) is PropertyInfo property:
                    return RemovePropertyValueDisposal(property);
                case MethodCallExpression methodCall:
                    return RemoveMethodReturnValueDisposal(methodCall.Method);
                case UnaryExpression unary:
                    return RemoveMethodReturnValueDisposal(unary.Method);
                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Specifies that active expressions using these options should not dispose of objects they have received as a result of invoking a specified method when the objects are replaced or otherwise discarded
        /// </summary>
        /// <param name="method">The method yielding the objects</param>
        /// <returns><c>true</c> if this has resulted in a change in the options; otherwise, <c>false</c></returns>
        public bool RemoveMethodReturnValueDisposal(MethodInfo method)
        {
            RequireUnfrozen();
            return disposeMethodReturnValues.TryRemove(method, out var discard);
        }

        /// <summary>
        /// Specifies that active expressions using these options should not dispose of objects they have received as a result of getting the value of a specified property when the objects are replaced or otherwise discarded
        /// </summary>
        /// <param name="property">The property yielding the objects</param>
        /// <returns><c>true</c> if this has resulted in a change in the options; otherwise, <c>false</c></returns>
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

        /// <summary>
        /// Gets/sets whether active expressions using these options should dispose of objects they have constructed when the objects are replaced or otherwise discarded; the default is <c>true</c>
        /// </summary>
        public bool DisposeConstructedObjects
        {
            get => disposeConstructedObjects;
            set
            {
                RequireUnfrozen();
                disposeConstructedObjects = value;
            }
        }

        /// <summary>
        /// Gets/sets whether active expressions using these options should dispose of objects they have received as a result of invoking static (Shared in Visual Basic) methods when the objects are replaced or otherwise discarded; the default is <c>true</c>
        /// </summary>
        public bool DisposeStaticMethodReturnValues
        {
            get => disposeStaticMethodReturnValues;
            set
            {
                RequireUnfrozen();
                disposeStaticMethodReturnValues = value;
            }
        }
    }
}
