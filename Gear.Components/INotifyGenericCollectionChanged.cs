using System;

namespace Gear.Components
{
    /// <summary>
    /// Notifies listeners of dynamic changes, such as when an item is added and removed or the whole list is cleared
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence</typeparam>
    public interface INotifyGenericCollectionChanged<T>
    {
        /// <summary>
        /// Occurs when the collection changes
        /// </summary>
        event EventHandler<NotifyGenericCollectionChangedEventArgs<T>> GenericCollectionChanged;
    }
}
