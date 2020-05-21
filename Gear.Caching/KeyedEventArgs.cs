using System;

namespace Gear.Caching
{
    public partial class AsyncCache<TKey, TValue>
    {
        #region EventArgs classes

        /// <summary>
        /// Event arguments for <see cref="AsyncCache{TKey, TValue}"/> events based on keys
        /// </summary>
        public class KeyedEventArgs : EventArgs
        {
			/// <summary>
			/// Initializes a new instance of the <see cref="KeyedEventArgs"/> class
            /// </summary>
            /// <param name="key">The key</param>
            public KeyedEventArgs(TKey key) => Key = key;

            /// <summary>
            /// Gets the key
            /// </summary>
            public TKey Key { get; }
        }

        #endregion ValueSource classes
    }
}
