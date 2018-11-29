using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Gear.Components
{
    public static class Default
    {
        static readonly ConcurrentDictionary<Type, object> defaults = new ConcurrentDictionary<Type, object>();
        static readonly MethodInfo getDefaultMethod = typeof(Default).GetRuntimeMethod(nameof(GetDefault), new Type[0]);

        static object CreateForType(Type type) => getDefaultMethod.MakeGenericMethod(type).Invoke(null, null);

        static T GetDefault<T>() => default;

        public static object GetForType(Type type) => defaults.GetOrAdd(type, CreateForType);
    }
}
