using System;

namespace Gear.Components
{
    /// <summary>
    /// Provides a mechanism for releasing unmanaged resources synchronously and notifying about property changes
    /// </summary>
    public abstract class SyncDisposablePropertyChangeNotifier : PropertyChangeNotifier, IDisposable, IDisposeStatus, INotifyDisposed, INotifyDisposing
    {
        /// <summary>
        /// Finalizes this object
        /// </summary>
        ~SyncDisposablePropertyChangeNotifier()
        {
            var e = new DisposalNotificationEventArgs(true);
            OnDisposing(e);
            Dispose(false);
            IsDisposed = true;
            OnDisposed(e);
        }

        readonly object disposalAccess = new object();
        bool isDisposed;

        /// <summary>
        /// Occurs when this object has been disposed
        /// </summary>
        public event EventHandler<DisposalNotificationEventArgs> Disposed;

        /// <summary>
        /// Occurs when this object is being disposed
        /// </summary>
        public event EventHandler<DisposalNotificationEventArgs> Disposing;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources
        /// </summary>
        public void Dispose()
        {
            lock (disposalAccess)
                if (!IsDisposed)
                {
                    var e = new DisposalNotificationEventArgs(false);
                    OnDisposing(e);
                    Dispose(true);
                    IsDisposed = true;
                    OnDisposed(e);
                    Disposing = null;
                    Disposed = null;
                    GC.SuppressFinalize(this);
                }
        }

        /// <summary>
        /// Frees, releases, or resets unmanaged resources
        /// </summary>
        /// <param name="disposing">false if invoked by the finalizer because the object is being garbage collected; otherwise, true</param>
        protected abstract void Dispose(bool disposing);

        /// <summary>
        /// Raises the <see cref="Disposed"/> event with the specified arguments
        /// </summary>
        /// <param name="e">The event arguments</param>
        protected virtual void OnDisposed(DisposalNotificationEventArgs e) => Disposed?.Invoke(this, e);

        /// <summary>
        /// Raises the <see cref="Disposing"/> event with the specified arguments
        /// </summary>
        /// <param name="e">The event arguments</param>
        protected virtual void OnDisposing(DisposalNotificationEventArgs e) => Disposing?.Invoke(this, e);

        /// <summary>
        /// Ensure the object has not been disposed
        /// </summary>
		/// <exception cref="ObjectDisposedException">The object has already been disposed</exception>
		protected void ThrowIfDisposed()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        /// <summary>
        /// Gets whether this object has been disposed
        /// </summary>
        public bool IsDisposed
        {
            get => isDisposed;
            private set => SetBackedProperty(ref isDisposed, in value);
        }
    }
}
