using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Gear.Components
{
    /// <summary>
    /// Provides a synchronization context for the Task Parallel Library
    /// </summary>
    public class AsyncSynchronizationContext : SynchronizationContext, IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncSynchronizationContext"/> class
        /// </summary>
        public AsyncSynchronizationContext()
        {
            queuedCallbacksCancellationTokenSource = new CancellationTokenSource();
            queuedCallbacks = new BufferBlock<(SendOrPostCallback callback, object state, ManualResetEventSlim signal, Exception exception)>(new DataflowBlockOptions { CancellationToken = queuedCallbacksCancellationTokenSource.Token });
            Task.Run(ProcessCallbacks);
        }

        internal AsyncSynchronizationContext(bool allowDisposal) : this() =>
            this.allowDisposal = allowDisposal;

        /// <summary>
        /// Finalizes this object
        /// </summary>
        ~AsyncSynchronizationContext() => Dispose(false);

        readonly bool allowDisposal;
        readonly BufferBlock<(SendOrPostCallback callback, object state, ManualResetEventSlim signal, Exception exception)> queuedCallbacks;
        readonly CancellationTokenSource queuedCallbacksCancellationTokenSource;

        async Task ProcessCallbacks()
        {
            while (true)
            {
                var csse = await queuedCallbacks.ReceiveAsync().ConfigureAwait(false);
                var currentContext = Current;
                SetSynchronizationContext(this);
                var (callback, state, signal, _) = csse;
                try
                {
                    callback(state);
                }
                catch (Exception ex)
                {
                    csse.exception = ex;
                }
                if (signal != null)
                    signal.Set();
                SetSynchronizationContext(currentContext);
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources
        /// </summary>
        public void Dispose()
        {
            if (!allowDisposal)
                throw new InvalidOperationException();
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Frees, releases, or resets unmanaged resources
        /// </summary>
        /// <param name="disposing"><c>false</c> if invoked by the finalizer because the object is being garbage collected; otherwise, <c>true</c></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                queuedCallbacksCancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Dispatches an asynchronous message to the synchronization context
        /// </summary>
        /// <param name="d">The <see cref="SendOrPostCallback"/> delegate to call</param>
        /// <param name="state">The object passed to the delegate</param>
        public override void Post(SendOrPostCallback d, object state) =>
            queuedCallbacks.Post((d ?? throw new ArgumentNullException(nameof(d)), state, null, null));

        /// <summary>
        /// Dispatches a synchronous message to the synchronization context
        /// </summary>
        /// <param name="d">The <see cref="SendOrPostCallback"/> delegate to call</param>
        /// <param name="state">The object passed to the delegate</param>
        public override void Send(SendOrPostCallback d, object state)
        {
            using (var signal = new ManualResetEventSlim(false))
            {
                var csse = (callback: d ?? throw new ArgumentNullException(nameof(d)), state, signal, exception: (Exception)null);
                queuedCallbacks.Post(csse);
                signal.Wait();
                if (csse.exception != null)
                    ExceptionDispatchInfo.Capture(csse.exception).Throw();
            }
        }
    }
}
