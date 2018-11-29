using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace Gear.Components
{
    public class FastEqualityComparer
    {
        static readonly ConcurrentDictionary<Type, FastEqualityComparer> equalityComparers = new ConcurrentDictionary<Type, FastEqualityComparer>();

        public static FastEqualityComparer Create(Type type) => equalityComparers.GetOrAdd(type, Factory);

        static FastEqualityComparer Factory(Type type) => new FastEqualityComparer(type);

        FastEqualityComparer(Type type)
        {
            var equalityComparerType = typeof(EqualityComparer<>).MakeGenericType(type);
            equalityComparer = equalityComparerType.GetRuntimeProperty(nameof(EqualityComparer<object>.Default)).GetValue(null);
            equals = new FastMethodInfo(equalityComparerType.GetRuntimeMethod(nameof(EqualityComparer<object>.Equals), new Type[] { type, type }));
            getHashCode = new FastMethodInfo(equalityComparerType.GetRuntimeMethod(nameof(EqualityComparer<object>.GetHashCode), new Type[] { type }));
        }

        readonly object equalityComparer;
        readonly FastMethodInfo equals;
        readonly FastMethodInfo getHashCode;

        public bool Equals(object x, object y) => (bool)equals.Invoke(equalityComparer, x, y);

        public int GetHashCode(object obj) => (int)getHashCode.Invoke(equalityComparer, obj);
    }
}
