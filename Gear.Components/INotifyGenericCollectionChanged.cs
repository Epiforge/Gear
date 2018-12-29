using System;

namespace Gear.Components
{
    public interface INotifyGenericCollectionChanged<T>
    {
        event EventHandler<NotifyGenericCollectionChangedEventArgs<T>> GenericCollectionChanged;
    }
}
