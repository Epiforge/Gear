using System;

namespace Gear.Components
{
    public interface INotifyDictionaryChanged<TKey, TValue>
    {
        event EventHandler<NotifyDictionaryChangedEventArgs<TKey, TValue>> DictionaryChanged;
    }
}
