using Nito.AsyncEx;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Gear.Components
{
    /// <summary>
    /// Provides a mechanism for releasing unmanaged resources asynchronously and notifying about property changes
    /// </summary>
    public abstract class AsyncDisposablePropertyChangeNotifier : PropertyChangeNotifier, IAsyncDisposable, IDisposeStatus, INotifyDisposed, INotifyDisposing
    {
        /// <summary>
        /// Finalizes this object
        /// </summary>
        ~AsyncDisposablePropertyChangeNotifier()
        {
            var e = new DisposalNotificationEventArgs(true);
            OnDisposing(e);
            DisposeAsync(false).Wait();
            IsDisposed = true;
            OnDisposed(e);
        }

        readonly AsyncLock disposalAccess = new AsyncLock();
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
        /// <param name="cancellationToken">A token that can be used to attempt to cancel disposal</param>
        /// <exception cref="OperationCanceledException">Disposal was interrupted by a cancellation request</exception>
        public async Task DisposeAsync(CancellationToken cancellationToken = default)
        {
            using (await disposalAccess.LockAsync(cancellationToken).ConfigureAwait(false))
                if (!isDisposed)
                {
                    var e = new DisposalNotificationEventArgs(false);
                    OnDisposing(e);
                    await DisposeAsync(true, cancellationToken).ConfigureAwait(false);
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
        /// <param name="cancellationToken">A token that can be used to attempt to cancel disposal</param>
        protected abstract Task DisposeAsync(bool disposing, CancellationToken cancellationToken = default);

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
            if (isDisposed)
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
