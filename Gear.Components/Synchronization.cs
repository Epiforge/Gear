using Nito.AsyncEx;
using System;
using System.Threading;

namespace Gear.Components
{
    public static class Synchronization
    {
        static readonly Lazy<AsyncContext> asyncContext = new Lazy<AsyncContext>(CreateAsyncContext, LazyThreadSafetyMode.ExecutionAndPublication);

        static AsyncContext CreateAsyncContext() => new AsyncContext();

        public static SynchronizationContext DefaultSynchronizationContext => asyncContext.Value.SynchronizationContext;
    }
}
