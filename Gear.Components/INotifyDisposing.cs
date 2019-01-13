using System;

namespace Gear.Components
{
    /// <summary>
    /// Notifies clients that the object is being disposed
    /// </summary>
    public interface INotifyDisposing
    {
        /// <summary>
        /// Occurs when this object is being disposed
        /// </summary>
        event EventHandler<DisposalNotificationEventArgs> Disposing;
    }
}
