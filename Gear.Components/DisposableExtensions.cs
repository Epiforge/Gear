using System;
using System.Threading;
using System.Threading.Tasks;

namespace Gear.Components
{
	/// <summary>
	/// Provides extensions for dealing with instances of <see cref="Disposable"/>
    /// </summary>
    public static class DisposableExtensions
	{
		/// <summary>
        /// Executes an action and then asynchronously disposes of an object
        /// </summary>
        /// <param name="asyncDisposable">The object to be disposed</param>
        /// <param name="action">The action to execute</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the disposal</param>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is null</exception>
        /// <exception cref="OperationCanceledException">disposal was interrupted by a cancellation request</exception>
		public static Task UsingAsync(this IAsyncDisposable asyncDisposable, Action action, CancellationToken cancellationToken = default) => Disposable.UsingAsync(asyncDisposable, action, cancellationToken);

        /// <summary>
        /// Executes an action and then asynchronously disposes of an object
        /// </summary>
        /// <param name="asyncDisposable">The object to be disposed</param>
        /// <param name="asyncAction">The action to execute</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the disposal</param>
        /// <exception cref="ArgumentNullException"><paramref name="asyncAction"/> is null</exception>
        /// <exception cref="OperationCanceledException">disposal was interrupted by a cancellation request</exception>      
		public static Task UsingAsync(this IAsyncDisposable asyncDisposable, Func<Task> asyncAction, CancellationToken cancellationToken = default) => Disposable.UsingAsync(asyncDisposable, asyncAction, cancellationToken);
    }
}
