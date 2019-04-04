using Nito.AsyncEx;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Gear.Components
{
    /// <summary>
    /// Provides an overridable mechanism for releasing unmanaged resources asynchronously
    /// </summary>
    public abstract class OverridableAsyncDisposable : IAsyncDisposable, INotifyDisposalOverridden, IDisposalStatus, INotifyDisposed, INotifyDisposing
    {
        /// <summary>
        /// Finalizes this object
        /// </summary>
        ~OverridableAsyncDisposable()
        {
            var e = new DisposalNotificationEventArgs(true);
            OnDisposing(e);
            DisposeAsync(false).Wait();
            IsDisposed = true;
            OnDisposed(e);
        }

        readonly AsyncLock disposalAccess = new AsyncLock();

        /// <summary>
        /// Occurs when this object's disposal has been overridden
        /// </summary>
        public event EventHandler<DisposalNotificationEventArgs> DisposalOverridden;

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
                if (!IsDisposed)
                {
                    var e = new DisposalNotificationEventArgs(false);
                    OnDisposing(e);
                    if (IsDisposed = await DisposeAsync(true, cancellationToken).ConfigureAwait(false))
                    {
                        OnDisposed(e);
                        Disposing = null;
                        DisposalOverridden = null;
                        Disposed = null;
                        GC.SuppressFinalize(this);
                    }
                    else
                        OnDisposalOverridden(e);
                }
        }

        /// <summary>
        /// Frees, releases, or resets unmanaged resources
        /// </summary>
        /// <param name="disposing">false if invoked by the finalizer because the object is being garbage collected; otherwise, true</param>
        /// <param name="cancellationToken">A token that can be used to attempt to cancel disposal</param>
        /// <returns>true if disposal completed; otherwise, false</returns>
        protected abstract Task<bool> DisposeAsync(bool disposing, CancellationToken cancellationToken = default);

        /// <summary>
        /// Raises the <see cref="DisposalOverridden"/> event with the specified arguments
        /// </summary>
        /// <param name="e">The event arguments</param>
        protected virtual void OnDisposalOverridden(DisposalNotificationEventArgs e) => DisposalOverridden?.Invoke(this, e);

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
        public bool IsDisposed { get; private set; }
    }
}
