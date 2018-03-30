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
        /// <param name="continueFromDisposalOnCapturedContext"><see cref="true"/> to attempt to marshal the continuation from disposal back to the original context captured; otherwise, <see cref="false"/></param>
        /// <param name="cancellationToken">The cancellation token used to cancel the disposal</param>
        public static async Task UsingAsync(Disposable disposable, Action action, bool continueFromDisposalOnCapturedContext = true, CancellationToken cancellationToken = default)
        {
            if (disposable == null)
                throw new ArgumentNullException(nameof(disposable));
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                action();
            }
            finally
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (continueFromDisposalOnCapturedContext)
                    await disposable.DisposeAsync(cancellationToken);
                else
                    await disposable.DisposeAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Executes an action and then asynchronously disposes of an object
        /// </summary>
        /// <param name="disposable">The object to be disposed</param>
        /// <param name="cancelableAction">The action to execute</param>
        /// <param name="continueFromDisposalOnCapturedContext"><see cref="true"/> to attempt to marshal the continuation from disposal back to the original context captured; otherwise, <see cref="false"/></param>
        /// <param name="cancellationToken">The cancellation token used to cancel the disposal</param>
        public static async Task UsingAsync(Disposable disposable, Action<CancellationToken> cancelableAction, bool continueFromDisposalOnCapturedContext = true, CancellationToken cancellationToken = default)
        {
            if (disposable == null)
                throw new ArgumentNullException(nameof(disposable));
            if (cancelableAction == null)
                throw new ArgumentNullException(nameof(cancelableAction));
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                cancelableAction(cancellationToken);
            }
            finally
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (continueFromDisposalOnCapturedContext)
                    await disposable.DisposeAsync(cancellationToken);
                else
                    await disposable.DisposeAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Executes an action and then asynchronously disposes of an object
        /// </summary>
        /// <param name="disposable">The object to be disposed</param>
        /// <param name="asyncAction">The action to execute</param>
        /// <param name="continueFromActionOnCapturedContext"><see cref="true"/> to attempt to marshal the continuation from executing the action back to the original context captured; otherwise, <see cref="false"/></param>
        /// <param name="continueFromDisposalOnCapturedContext"><see cref="true"/> to attempt to marshal the continuation from disposal back to the original context captured; otherwise, <see cref="false"/></param>
        /// <param name="cancellationToken">The cancellation token used to cancel the disposal</param>
        public static async Task UsingAsync(Disposable disposable, Func<Task> asyncAction, bool continueFromActionOnCapturedContext = true, bool continueFromDisposalOnCapturedContext = true, CancellationToken cancellationToken = default)
        {
            if (disposable == null)
                throw new ArgumentNullException(nameof(disposable));
            if (asyncAction == null)
                throw new ArgumentNullException(nameof(asyncAction));
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                if (continueFromActionOnCapturedContext)
                    await asyncAction();
                else
                    await asyncAction().ConfigureAwait(false);
            }
            finally
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (continueFromDisposalOnCapturedContext)
                    await disposable.DisposeAsync(cancellationToken);
                else
                    await disposable.DisposeAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Executes an action and then asynchronously disposes of an object
        /// </summary>
        /// <param name="disposable">The object to be disposed</param>
        /// <param name="cancelableAsyncAction">The action to execute</param>
        /// <param name="continueFromActionOnCapturedContext"><see cref="true"/> to attempt to marshal the continuation from executing the action back to the original context captured; otherwise, <see cref="false"/></param>
        /// <param name="continueFromDisposalOnCapturedContext"><see cref="true"/> to attempt to marshal the continuation from disposal back to the original context captured; otherwise, <see cref="false"/></param>
        /// <param name="cancellationToken">The cancellation token used to cancel the disposal</param>
        public static async Task UsingAsync(Disposable disposable, Func<CancellationToken, Task> cancelableAsyncAction, bool continueFromActionOnCapturedContext = true, bool continueFromDisposalOnCapturedContext = true, CancellationToken cancellationToken = default)
        {
            if (disposable == null)
                throw new ArgumentNullException(nameof(disposable));
            if (cancelableAsyncAction == null)
                throw new ArgumentNullException(nameof(cancelableAsyncAction));
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                if (continueFromActionOnCapturedContext)
                    await cancelableAsyncAction(cancellationToken);
                else
                    await cancelableAsyncAction(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (continueFromDisposalOnCapturedContext)
                    await disposable.DisposeAsync(cancellationToken);
                else
                    await disposable.DisposeAsync(cancellationToken).ConfigureAwait(false);
            }
        }

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
        /// <param name="disposing"><see cref="false"/> if invoked by the finalizer because the object is being garbage collected; otherwise, <see cref="true"/></param>
        protected virtual void Dispose(bool disposing)
        {
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to attempt to cancel disposal</param>
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
        /// <param name="disposing"><see cref="false"/> if invoked by the finalizer because the object is being garbage collected; otherwise, <see cref="true"/></param>
        /// <param name="cancellationToken">A token that can be used to attempt to cancel disposal</param>
		protected virtual Task DisposeAsync(bool disposing, CancellationToken cancellationToken = default) => Task.CompletedTask;

        /// <summary>
        /// Gets whether this class supports asynchronous disposal
		/// </summary>
        protected virtual bool IsAsyncDisposable => false;

        /// <summary>
        /// Gets whether this class supports synchronous disposal
        /// </summary>
        protected virtual bool IsDisposable => true;
        
        /// <summary>
        /// Gets whether this object has been disposed
        /// </summary>
		protected bool IsDisposed => isDisposed;
    }
}
