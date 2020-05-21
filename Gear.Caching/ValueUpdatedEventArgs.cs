namespace Gear.Caching
{
    public partial class AsyncCache<TKey, TValue>
    {
        /// <summary>
        /// Event arguments for the <see cref="AsyncCache{TKey, TValue}.ValueUpdated"/> event
        /// </summary>
        public class ValueUpdatedEventArgs : KeyedEventArgs
        {
			/// <summary>
			/// Initializes a new instance of the <see cref="ValueUpdatedEventArgs"/> class
            /// </summary>
            /// <param name="key">The key</param>
            /// <param name="oldValue">The old value</param>
            /// <param name="newValue">The new value</param>
			/// <param name="isRefresh">Whether the update was the result of a refresh</param>
            public ValueUpdatedEventArgs(TKey key, TValue oldValue, TValue newValue, bool isRefresh) : base(key)
            {
                OldValue = oldValue;
                NewValue = NewValue;
                IsRefresh = isRefresh;
            }

            /// <summary>
            /// Gets whether the update was the result of a refresh
            /// </summary>
            public bool IsRefresh { get; }

            /// <summary>
            /// Gets the new value
            /// </summary>
            public TValue NewValue { get; }

            /// <summary>
            /// Gets the old value
            /// </summary>
            public TValue OldValue { get; }
        }
    }
}
