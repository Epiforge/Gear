using System.Collections.Generic;

namespace Gear.ActiveQuery
{
    public static class ReadOnlyListExtensions
    {
        public static int IndexOf<T>(this IReadOnlyList<T> readOnlyList, T element)
        {
            var comparer = EqualityComparer<T>.Default;
            for (int i = 0, ii = readOnlyList.Count; i < ii; ++i)
                if (comparer.Equals(readOnlyList[i], element))
                    return i;
            return -1;
        }
    }
}
