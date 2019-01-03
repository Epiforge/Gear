using Nito.AsyncEx;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Gear.Components
{
    /// <summary>
    /// Provides a mechanism for releasing unmanaged resources asynchronously
    /// </summary>
    public abstract class AsyncDisposable : IAsyncDisposable
    {
        /// <summary>
        /// Executes an action and then asynchronously disposes of an object
        /// </summary>
        /// <param name="asyncDisposable">The object to be disposed</param>
        /// <param name="action">The action to execute</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the disposal</param>
		/// <exception cref="ArgumentNullException"><paramref name="action"/> is null</exception>
		/// <exception cref="OperationCanceledException">disposal was interrupted by a cancellation request</exception>
        public static async Task UsingAsync(IAsyncDisposable asyncDisposable, Action action, CancellationToken cancellationToken = default)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            try
            {
                action();
            }
            finally
            {
                if (asyncDisposable != null)
                    await asyncDisposable.DisposeAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Executes an action and then asynchronously disposes of an object
        /// </summary>
        /// <param name="asyncDisposable">The object to be disposed</param>
        /// <param name="asyncAction">The action to execute</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the disposal</param>
		/// <exception cref="ArgumentNullException"><paramref name="asyncAction"/> is null</exception>
        /// <exception cref="OperationCanceledException">disposal was interrupted by a cancellation request</exception>
        public static async Task UsingAsync(IAsyncDisposable asyncDisposable, Func<Task> asyncAction, CancellationToken cancellationToken = default)
        {
            if (asyncAction == null)
                throw new ArgumentNullException(nameof(asyncAction));
            try
            {
                await asyncAction().ConfigureAwait(false);
            }
            finally
            {
                if (asyncDisposable != null)
                    await asyncDisposable.DisposeAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Finalizes this object
        /// </summary>
        ~AsyncDisposable() => DisposeAsync(false).Wait();

        readonly AsyncLock disposalAccess = new AsyncLock();

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
                    await DisposeAsync(true, cancellationToken).ConfigureAwait(false);
                    IsDisposed = true;
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
