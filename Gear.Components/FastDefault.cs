using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Gear.Components
{
    public static class FastDefault
    {
        static readonly ConcurrentDictionary<Type, object> defaults = new ConcurrentDictionary<Type, object>();
        static readonly MethodInfo getDefaultMethod = typeof(FastDefault).GetRuntimeMethod(nameof(GetDefault), new Type[0]);

        static object CreateDefault(Type type) => getDefaultMethod.MakeGenericMethod(type).Invoke(null, null);

        static T GetDefault<T>() => default;

        public static object Get(Type type) => defaults.GetOrAdd(type, CreateDefault);
    }
}
