using System;

namespace Gear.Caching
{
    public partial class AsyncCache<TKey, TValue>
    {
        /// <summary>
        /// Event arguments for the <see cref="AsyncCache{TKey, TValue}.ValueExpired"/> event
        /// </summary>
        public class ValueExpiredEventArgs : KeyValueEventArgs
        {
			/// <summary>
			/// Initializes a new instance of the <see cref="ValueExpiredEventArgs"/> class
            /// </summary>
            /// <param name="key">The key</param>
            /// <param name="value">The value</param>
            /// <param name="expired">When the value expired</param>
            public ValueExpiredEventArgs(TKey key, TValue value, DateTime expired) : base(key, value) => Expired = expired;
            
            /// <summary>
            /// Gets when the value expired
            /// </summary>
            public DateTime Expired { get; }
        }
    }
}
