using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace Gear.Components
{
    public class FastComparer
    {
        static readonly ConcurrentDictionary<Type, FastComparer> comparers = new ConcurrentDictionary<Type, FastComparer>();

        public static FastComparer Create(Type type) => comparers.GetOrAdd(type, Factory);

        static FastComparer Factory(Type type) => new FastComparer(type);

        FastComparer(Type type)
        {
            var comparerType = typeof(Comparer<>).MakeGenericType(type);
            comparer = comparerType.GetRuntimeProperty(nameof(Comparer<object>.Default)).GetValue(null);
            compare = new FastMethodInfo(comparerType.GetRuntimeMethod(nameof(Comparer<object>.Compare), new Type[] { type }));
        }

        readonly FastMethodInfo compare;
        readonly object comparer;

        public int Compare(object x, object y) => (int)compare.Invoke(comparer, x, y);
    }
}
