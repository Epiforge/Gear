namespace Gear.Caching
{
    public partial class AsyncCache<TKey, TValue>
    {
        /// <summary>
        /// The result of attempting to get a value from the cache
        /// </summary>
        public class TryGetResult
        {
			/// <summary>
			/// Initializes a new instance of the <see cref="TryGetResult"/> class as a failed attempt
            /// </summary>
            public TryGetResult() => WasFound = false;

            /// <summary>
			/// Initializes a new instance of the <see cref="TryGetResult"/> class as a successful attempt
            /// </summary>
            /// <param name="value">The value</param>
            public TryGetResult(TValue value)
            {
                Value = value;
                WasFound = true;
            }

            /// <summary>
            /// Gets the value
            /// </summary>
            public TValue Value { get; }

            /// <summary>
            /// Gets whether the value was found
            /// </summary>
            public bool WasFound { get; }
        }

        /// <summary>
        /// The result of attempting to get a typed value from the cache
        /// </summary>
        public class TryGetResult<T> where T : TValue
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="TryGetResult{T}"/> class as a failed attempt
            /// </summary>
            public TryGetResult() => WasFound = false;

            /// <summary>
			/// Initializes a new instance of the <see cref="TryGetResult{T}"/> class as a successful attempt
            /// </summary>
            /// <param name="value">The value</param>
            public TryGetResult(T value)
            {
                Value = value;
                WasFound = true;
            }

            /// <summary>
            /// Gets the value
            /// </summary>
            public T Value { get; }

            /// <summary>
            /// Gets whether the value was found
            /// </summary>
            public bool WasFound { get; }
        }
    }
}
