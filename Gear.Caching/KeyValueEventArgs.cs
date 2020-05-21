namespace Gear.Caching
{
    public partial class AsyncCache<TKey, TValue>
    {
        /// <summary>
        /// Event arguments for <see cref="AsyncCache{TKey, TValue}"/> events based on keys and values
        /// </summary>
        public class KeyValueEventArgs : KeyedEventArgs
        {
			/// <summary>
			/// Initializes a new instance of the <see cref="KeyValueEventArgs"/> class
            /// </summary>
            /// <param name="key">The key</param>
            /// <param name="value">The value</param>
            public KeyValueEventArgs(TKey key, TValue value) : base(key) => Value = value;

            /// <summary>
            /// Gets the value
            /// </summary>
            public TValue Value { get; }
        }
    }
}
