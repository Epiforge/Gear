using System;

namespace Gear.Caching
{
    public partial class AsyncCache<TKey, TValue>
    {
        /// <summary>
        /// Event arguments for <see cref="AsyncCache{TKey, TValue}"/> exception events based on keys
        /// </summary>
        public class KeyedExceptionEventArgs : KeyedEventArgs
        {
			/// <summary>
			/// Initializes a new instance of the <see cref="KeyedExceptionEventArgs"/> class
            /// </summary>
            /// <param name="key">The key</param>
            /// <param name="ex">The exception</param>
            public KeyedExceptionEventArgs(TKey key, Exception ex) : base(key) => Exception = ex;

            /// <summary>
            /// Gets the exception
            /// </summary>
            public Exception Exception { get; }
        }
    }
}
