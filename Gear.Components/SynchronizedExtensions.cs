using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Gear.Components
{
    public static class SynchronizedExtensions
    {
        public static void Execute(this ISynchronized synchronizable, Action action) => ExecuteOn(action, synchronizable.SynchronizationContext);

        public static TReturn Execute<TReturn>(this ISynchronized synchronizable, Func<TReturn> func) => ExecuteOn(func, synchronizable.SynchronizationContext);

        public static Task ExecuteAsync(this ISynchronized synchronizable, Action action) => ExecuteOnAsync(action, synchronizable.SynchronizationContext);

        public static Task<TResult> ExecuteAsync<TResult>(this ISynchronized synchronizable, Func<TResult> func) => ExecuteOnAsync(func, synchronizable.SynchronizationContext);

        public static Task ExecuteAsync(this ISynchronized synchronizable, Func<Task> asyncAction) => ExecuteOnAsync(asyncAction, synchronizable.SynchronizationContext);

        public static Task<TResult> ExecuteAsync<TResult>(this ISynchronized synchronizable, Func<Task<TResult>> asyncFunc) => ExecuteOnAsync(asyncFunc, synchronizable.SynchronizationContext);

        static void ExecuteOn(Action action, SynchronizationContext synchronizationContext)
        {
            if (synchronizationContext == null || SynchronizationContext.Current == synchronizationContext)
            {
                action();
                return;
            }
            ExceptionDispatchInfo edi = default;
            synchronizationContext.Send(state =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    edi = ExceptionDispatchInfo.Capture(ex);
                }
            }, null);
            edi?.Throw();
        }

        static TReturn ExecuteOn<TReturn>(Func<TReturn> func, SynchronizationContext synchronizationContext)
        {
            if (synchronizationContext == null || SynchronizationContext.Current == synchronizationContext)
                return func();
            TReturn result = default;
            ExceptionDispatchInfo edi = default;
            synchronizationContext.Send(state =>
            {
                try
                {
                    result = func();
                }
                catch (Exception ex)
                {
                    edi = ExceptionDispatchInfo.Capture(ex);
                }
            }, null);
            edi?.Throw();
            return result;
        }

        static Task ExecuteOnAsync(Action action, SynchronizationContext synchronizationContext)
        {
            if (synchronizationContext == null || SynchronizationContext.Current == synchronizationContext)
            {
                action();
                return Task.CompletedTask;
            }
            var completion = new TaskCompletionSource<object>();
            synchronizationContext.Post(state => completion.AttemptSetResult(action), null);
            return completion.Task;
        }

        static Task<TResult> ExecuteOnAsync<TResult>(Func<TResult> func, SynchronizationContext synchronizationContext)
        {
            if (synchronizationContext == null || SynchronizationContext.Current == synchronizationContext)
                return Task.FromResult(func());
            var completion = new TaskCompletionSource<TResult>();
            synchronizationContext.Post(state => completion.AttemptSetResult(func), null);
            return completion.Task;
        }

        static async Task ExecuteOnAsync(Func<Task> asyncAction, SynchronizationContext synchronizationContext)
        {
            if (synchronizationContext == null || SynchronizationContext.Current == synchronizationContext)
            {
                await asyncAction().ConfigureAwait(false);
                return;
            }
            var completion = new TaskCompletionSource<object>();
            synchronizationContext.Post(async state => await completion.AttemptSetResultAsync(asyncAction).ConfigureAwait(false), null);
            await completion.Task.ConfigureAwait(false);
        }

        static async Task<TResult> ExecuteOnAsync<TResult>(Func<Task<TResult>> asyncFunc, SynchronizationContext synchronizationContext)
        {
            if (synchronizationContext == null || SynchronizationContext.Current == synchronizationContext)
                return await asyncFunc().ConfigureAwait(false);
            var completion = new TaskCompletionSource<TResult>();
            synchronizationContext.Post(async state => await completion.AttemptSetResultAsync(asyncFunc).ConfigureAwait(false), null);
            return await completion.Task.ConfigureAwait(false);
        }

        public static void SequentialExecute(this ISynchronized synchronizable, Action action) => ExecuteOn(action, synchronizable?.SynchronizationContext ?? Synchronization.DefaultSynchronizationContext);

        public static TReturn SequentialExecute<TReturn>(this ISynchronized synchronizable, Func<TReturn> func) => ExecuteOn(func, synchronizable?.SynchronizationContext ?? Synchronization.DefaultSynchronizationContext);

        public static Task SequentialExecuteAsync(this ISynchronized synchronizable, Action action) => ExecuteOnAsync(action, synchronizable?.SynchronizationContext ?? Synchronization.DefaultSynchronizationContext);

        public static Task<TResult> SequentialExecuteAsync<TResult>(this ISynchronized synchronizable, Func<TResult> func) => ExecuteOnAsync(func, synchronizable?.SynchronizationContext ?? Synchronization.DefaultSynchronizationContext);

        public static Task SequentialExecuteAsync(this ISynchronized synchronizable, Func<Task> asyncAction) => ExecuteOnAsync(asyncAction, synchronizable?.SynchronizationContext ?? Synchronization.DefaultSynchronizationContext);

        public static Task<TResult> SequentialExecuteAsync<TResult>(this ISynchronized synchronizable, Func<Task<TResult>> asyncFunc) => ExecuteOnAsync(asyncFunc, synchronizable?.SynchronizationContext ?? Synchronization.DefaultSynchronizationContext);
    }
}
