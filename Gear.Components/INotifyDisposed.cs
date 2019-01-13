using System;

namespace Gear.Components
{
    /// <summary>
    /// Notifies clients that the object has been disposed
    /// </summary>
    public interface INotifyDisposed
    {
        /// <summary>
        /// Occurs when this object has been disposed
        /// </summary>
        event EventHandler<DisposalNotificationEventArgs> Disposed;
    }
}
