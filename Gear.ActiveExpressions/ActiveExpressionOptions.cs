using Gear.Components;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace Gear.ActiveExpressions
{
    public class ActiveExpressionOptions
    {
        static ActiveExpressionOptions() => Default = new ActiveExpressionOptions();

        public static ActiveExpressionOptions Default { get; }

        public ActiveExpressionOptions()
        {
            DisposeConstructedObjects = true;
            DisposeStaticMethodReturnValues = true;
        }

        readonly ConcurrentDictionary<(Type type, EquatableList<Type> constuctorParameterTypes), bool> disposeConstructedTypes = new ConcurrentDictionary<(Type type, EquatableList<Type> constuctorParameterTypes), bool>();
        readonly ConcurrentDictionary<MethodInfo, bool> disposeMethodReturnValues = new ConcurrentDictionary<MethodInfo, bool>();

        public bool AddConstructedTypeDisposal(Type type, params Type[] constuctorParameterTypes) => disposeConstructedTypes.TryAdd((type, new EquatableList<Type>(constuctorParameterTypes)), false);

        public bool AddConstructedTypeDisposal(ConstructorInfo constructor) => disposeConstructedTypes.TryAdd((constructor.DeclaringType, new EquatableList<Type>(constructor.GetParameters().Select(parameterInfo => parameterInfo.ParameterType).ToList())), false);

        public bool AddMethodReturnValueDisposal(MethodInfo method) => disposeMethodReturnValues.TryAdd(method, false);

        public bool AddPropertyValueDisposal(PropertyInfo property) => AddMethodReturnValueDisposal(property.GetMethod);

        internal bool IsConstructedTypeDisposed(Type type, EquatableList<Type> constructorParameterTypes) => DisposeConstructedObjects || disposeConstructedTypes.ContainsKey((type, constructorParameterTypes));

        public bool IsConstructedTypeDisposed(Type type, params Type[] constructorParameterTypes) => DisposeConstructedObjects || IsConstructedTypeDisposed(type, new EquatableList<Type>(constructorParameterTypes));

        public bool IsConstructedTypeDisposed(ConstructorInfo constructor) => disposeConstructedTypes.ContainsKey((constructor.DeclaringType, new EquatableList<Type>(constructor.GetParameters().Select(parameterInfo => parameterInfo.ParameterType).ToList())));

        public bool IsMethodReturnValueDisposed(MethodInfo method) => (method.IsStatic && DisposeStaticMethodReturnValues) || disposeMethodReturnValues.ContainsKey(method);

        public bool IsPropertyValueDisposed(PropertyInfo property) => IsMethodReturnValueDisposed(property.GetMethod);

        public bool RemoveConstructedTypeDisposal(Type type, params Type[] constuctorParameterTypes) => disposeConstructedTypes.TryRemove((type, new EquatableList<Type>(constuctorParameterTypes)), out var discard);

        public bool RemoveConstructedTypeDisposal(ConstructorInfo constructor) => disposeConstructedTypes.TryRemove((constructor.DeclaringType, new EquatableList<Type>(constructor.GetParameters().Select(parameterInfo => parameterInfo.ParameterType).ToList())), out var discard);

        public bool RemoveMethodReturnValueDisposal(MethodInfo method) => disposeMethodReturnValues.TryRemove(method, out var discard);

        public bool RemovePropertyValueDisposal(PropertyInfo property) => IsMethodReturnValueDisposed(property.GetMethod);

        public bool DisposeConstructedObjects { get; set; }

        public bool DisposeStaticMethodReturnValues { get; set; }
    }
}
