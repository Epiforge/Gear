using Nito.AsyncEx;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Gear.Components
{
    /// <summary>
    /// Provides an overridable mechanism for releasing unmanaged resources asynchronously
    /// </summary>
    public abstract class OverridableAsyncDisposable : IAsyncDisposable
    {
        /// <summary>
        /// Finalizes this object
        /// </summary>
        ~OverridableAsyncDisposable() => DisposeAsync(false).Wait();

        readonly AsyncLock disposalAccess = new AsyncLock();

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to attempt to cancel disposal</param>
        /// <exception cref="OperationCanceledException">Disposal was interrupted by a cancellation request</exception>
        public async Task DisposeAsync(CancellationToken cancellationToken = default)
        {
            using (await disposalAccess.LockAsync(cancellationToken).ConfigureAwait(false))
                if (!IsDisposed && (IsDisposed = await DisposeAsync(true, cancellationToken).ConfigureAwait(false)))
                    GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Frees, releases, or resets unmanaged resources
        /// </summary>
        /// <param name="disposing">false if invoked by the finalizer because the object is being garbage collected; otherwise, true</param>
        /// <param name="cancellationToken">A token that can be used to attempt to cancel disposal</param>
        /// <returns>true if disposal completed; otherwise, false</returns>
        protected abstract Task<bool> DisposeAsync(bool disposing, CancellationToken cancellationToken = default);

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
