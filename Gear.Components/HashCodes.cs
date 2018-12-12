using System.Collections.Generic;
using System.Linq;

namespace Gear.Components
{
    /// <summary>
    /// Provides utlities for generating hash codes to be returned from overrides of <see cref="object.GetHashCode"/>
    /// </summary>
    public static class HashCodes
    {
        /// <summary>
        /// Combines multiple hash codes into a single hash code
        /// </summary>
        /// <param name="hashCodes">The hash codes to combine</param>
        /// <returns>The combined hash code</returns>
        public static int CombineHashCodes(params int[] hashCodes) => CombineMultiple(hashCodes);

        static int CombineMultiple(IEnumerable<int> hashCodes)
        {
            var enumerator = hashCodes.GetEnumerator();
            if (!enumerator.MoveNext())
                return 0;
            var combined = enumerator.Current;
            while (enumerator.MoveNext())
                combined = CombineTwoHashCodes(combined, enumerator.Current);
            return combined;
        }

        /// <summary>
        /// Gets the hash codes for multiple elements in a sequence and produces a single combined hash code
        /// </summary>
        /// <typeparam name="T">The type of elements in the sequence</typeparam>
        /// <param name="source">The sequence of elements</param>
        /// <returns>The combined hash code</returns>
        public static int CombineElements<T>(IEnumerable<T> source) => CombineMultiple(source.Select(o => o?.GetHashCode() ?? 0));

        /// <summary>
        /// Gets the hash codes for multiple objects and produces a single combined hash code
        /// </summary>
        /// <param name="objects">The objects for which to get hash codes</param>
        /// <returns>The combined hash code</returns>
        public static int CombineObjects(params object[] objects) => CombineMultiple(objects.Select(o => o?.GetHashCode() ?? 0));

        static int CombineTwoHashCodes(int h1, int h2) => ((int)(((uint)h1 << 5) | ((uint)h1 >> 27)) + h1) ^ h2;
    }
}
