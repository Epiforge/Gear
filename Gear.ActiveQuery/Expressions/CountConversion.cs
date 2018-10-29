using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Gear.ActiveQuery.Expressions
{
    internal static class CountConversion
    {
        static readonly ConcurrentDictionary<Type, CountConversionDelegate> converters = new ConcurrentDictionary<Type, CountConversionDelegate>();

        static CountConversionDelegate CreateConverter(Type type)
        {
            var countParameter = Expression.Parameter(typeof(int));
            return Expression.Lambda<CountConversionDelegate>(Expression.Convert(countParameter, type), countParameter).Compile();
        }

        public static CountConversionDelegate GetConverter(Type type) => converters.GetOrAdd(type, CreateConverter);
    }
}