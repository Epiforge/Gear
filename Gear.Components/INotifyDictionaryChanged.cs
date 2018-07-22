using System;

namespace Gear.Components
{
    /// <summary>
    /// Notifies listeners of changes to values in a dictionary
    /// </summary>
    /// <typeparam name="TKey">The type of the dictionary's keys</typeparam>
    /// <typeparam name="TValue">The type of the dictionary's values</typeparam>
    public interface INotifyDictionaryChanged<TKey, TValue>
    {
        /// <summary>
        /// Occurs when a value is added
        /// </summary>
        event EventHandler<NotifyDictionaryValueEventArgs<TKey, TValue>> ValueAdded;

        /// <summary>
        /// Occurs when a value is removed
        /// </summary>
        event EventHandler<NotifyDictionaryValueEventArgs<TKey, TValue>> ValueRemoved;

        /// <summary>
        /// Occurs when a value is replaced
        /// </summary>
        event EventHandler<NotifyDictionaryValueReplacedEventArgs<TKey, TValue>> ValueReplaced;

        /// <summary>
        /// Occurs when multiple values are added
        /// </summary>
        event EventHandler<NotifyDictionaryValuesEventArgs<TKey, TValue>> ValuesAdded;

        /// <summary>
        /// Occurs when multiple values are removed
        /// </summary>
        event EventHandler<NotifyDictionaryValuesEventArgs<TKey, TValue>> ValuesRemoved;
    }
}
