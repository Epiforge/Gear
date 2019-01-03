using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace Gear.Components
{
    /// <summary>
    /// Provides a method for getting the default value of a type that is not known at compile time
    /// </summary>
    public static class FastDefault
    {
        static readonly ConcurrentDictionary<Type, object> defaults = new ConcurrentDictionary<Type, object>();
        static readonly MethodInfo getDefaultMethod = typeof(FastDefault).GetRuntimeMethods().Single(method => method.Name == nameof(GetDefault));

        static object CreateDefault(Type type) => getDefaultMethod.MakeGenericMethod(type).Invoke(null, null);

        static T GetDefault<T>() => default;

        /// <summary>
        /// Gets the default value for the specified type
        /// </summary>
        /// <param name="type">The type</param>
        /// <returns>The default value</returns>
        public static object Get(Type type) => defaults.GetOrAdd(type, CreateDefault);
    }
}
