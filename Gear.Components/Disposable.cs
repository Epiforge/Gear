using Nito.AsyncEx;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Gear.Components
{
    /// <summary>
    /// Provides a mechanism for releasing unmanaged resources
    /// </summary>
    public abstract class Disposable : IDisposable
    {
        /// <summary>
        /// Executes an action and then asynchronously disposes of an object
        /// </summary>
        /// <param name="disposable">The object to be disposed</param>
        /// <param name="action">The action to execute</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the disposal</param>
		/// <exception cref="ArgumentNullException"><paramref name="action"/> is null</exception>
		/// <exception cref="OperationCanceledException">disposal was interrupted by a cancellation request</exception>
        public static async Task UsingAsync(Disposable disposable, Action action, CancellationToken cancellationToken = default)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            try
            {
                action();
            }
            finally
            {
				if (disposable != null)
					await disposable.DisposeAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Executes an action and then asynchronously disposes of an object
        /// </summary>
        /// <param name="disposable">The object to be disposed</param>
        /// <param name="asyncAction">The action to execute</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the disposal</param>
		/// <exception cref="ArgumentNullException"><paramref name="asyncAction"/> is null</exception>
        /// <exception cref="OperationCanceledException">disposal was interrupted by a cancellation request</exception>
        public static async Task UsingAsync(Disposable disposable, Func<Task> asyncAction, CancellationToken cancellationToken = default)
        {
            if (asyncAction == null)
                throw new ArgumentNullException(nameof(asyncAction));
            try
            {
                await asyncAction().ConfigureAwait(false);
            }
            finally
            {
				if (disposable != null)
					await disposable.DisposeAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the class is reclaimed by garbage collection
        /// </summary>
        ~Disposable()
        {
            if (IsDisposable)
                Dispose(false);
            else if (IsAsyncDisposable)
                DisposeAsync(false).Wait();
        }

		bool isDisposed;
        AsyncLock disposalAccess = new AsyncLock();

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources
        /// </summary>
		/// <exception cref="InvalidOperationException">synchronous disposal is not supported</exception>
        public void Dispose()
        {
            if (!IsDisposable)
                throw new InvalidOperationException();
            using (disposalAccess.Lock())
                if (!isDisposed)
                {
                    Dispose(true);
                    isDisposed = true;
                    GC.SuppressFinalize(this);
                }
        }

		/// <summary>
		/// Frees, releases, or resets unmanaged resources
		/// </summary>
		/// <param name="disposing">false if invoked by the finalizer because the object is being garbage collected; otherwise, true</param>
		/// <exception cref="NotImplementedException">The deriving class has failed to properly override this method</exception>
		protected virtual void Dispose(bool disposing) => throw new NotImplementedException();

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to attempt to cancel disposal</param>
		/// <exception cref="InvalidOperationException">asyncronous disposal is not supported</exception>
        /// <exception cref="OperationCanceledException">disposal was interrupted by a cancellation request</exception>
        public async Task DisposeAsync(CancellationToken cancellationToken = default)
        {
            if (!IsAsyncDisposable)
                throw new InvalidOperationException();
            using (await disposalAccess.LockAsync().ConfigureAwait(false))
                if (!isDisposed)
                {
                    await DisposeAsync(true, cancellationToken).ConfigureAwait(false);
                    isDisposed = true;
                    GC.SuppressFinalize(this);
                }
        }

		/// <summary>
		/// Frees, releases, or resets unmanaged resources
		/// </summary>
		/// <param name="disposing">false if invoked by the finalizer because the object is being garbage collected; otherwise, true</param>
		/// <param name="cancellationToken">A token that can be used to attempt to cancel disposal</param>
		/// <exception cref="NotImplementedException">The deriving class has failed to properly override this method</exception>
		protected virtual Task DisposeAsync(bool disposing, CancellationToken cancellationToken = default) => throw new NotImplementedException();

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
        /// Gets whether this class supports asynchronous disposal
		/// </summary>
        protected virtual bool IsAsyncDisposable => false;

        /// <summary>
        /// Gets whether this class supports synchronous disposal
        /// </summary>
        protected virtual bool IsDisposable => false;
        
        /// <summary>
        /// Gets whether this object has been disposed
        /// </summary>
		protected bool IsDisposed => isDisposed;
    }
}
