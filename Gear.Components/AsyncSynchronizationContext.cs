using Nito.AsyncEx;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Gear.Components
{
    /// <summary>
    /// Provides a synchronization context for the Task Parallel Library
    /// </summary>
    public class AsyncSynchronizationContext : SynchronizationContext
    {
        readonly AsyncLock queuedCallbacksExecutionAccess = new AsyncLock();
        readonly ConcurrentQueue<(SendOrPostCallback callback, object state, ManualResetEventSlim signal, Exception exception)> queuedCallbacks = new ConcurrentQueue<(SendOrPostCallback callback, object state, ManualResetEventSlim signal, Exception exception)>();

        void ExecuteQueuedCallbacks() => Task.Run(async () =>
        {
            using (await queuedCallbacksExecutionAccess.LockAsync().ConfigureAwait(false))
            {
                if (!queuedCallbacks.IsEmpty)
                {
                    var postedCallbackExceptions = new List<Exception>();
                    var currentContext = Current;
                    SetSynchronizationContext(this);
                    while (queuedCallbacks.TryDequeue(out var csse))
                    {
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
                        else if (csse.exception != null)
                            postedCallbackExceptions.Add(csse.exception);
                    }
                    SetSynchronizationContext(currentContext);
                    if (postedCallbackExceptions.Count > 0)
                        throw new AggregateException("Unhandled exceptions encountered by posted callbacks", postedCallbackExceptions);
                }
            }
        });

        /// <summary>
        /// Dispatches an asynchronous message to the synchronization context
        /// </summary>
        /// <param name="d">The <see cref="SendOrPostCallback"/> delegate to call</param>
        /// <param name="state">The object passed to the delegate</param>
        public override void Post(SendOrPostCallback d, object state)
        {
            queuedCallbacks.Enqueue((d ?? throw new ArgumentNullException(nameof(d)), state, null, null));
            ExecuteQueuedCallbacks();
        }

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
                queuedCallbacks.Enqueue(csse);
                ExecuteQueuedCallbacks();
                signal.Wait();
                if (csse.exception != null)
                    ExceptionDispatchInfo.Capture(csse.exception).Throw();
            }
        }
    }
}
