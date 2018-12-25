using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Gear.Components
{
    public static class SynchronizedExtensions
    {
        public static void Execute(this SynchronizationContext synchronizationContext, Action action)
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

        public static void Execute(this ISynchronized synchronizable, Action action) => Execute(synchronizable.SynchronizationContext, action);

        public static TReturn Execute<TReturn>(this SynchronizationContext synchronizationContext, Func<TReturn> func)
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

        public static TReturn Execute<TReturn>(this ISynchronized synchronizable, Func<TReturn> func) => Execute(synchronizable.SynchronizationContext, func);

        public static Task ExecuteAsync(this SynchronizationContext synchronizationContext, Action action)
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

        public static Task ExecuteAsync(this ISynchronized synchronizable, Action action) => ExecuteAsync(synchronizable.SynchronizationContext, action);

        public static Task<TResult> ExecuteAsync<TResult>(this SynchronizationContext synchronizationContext, Func<TResult> func)
        {
            if (synchronizationContext == null || SynchronizationContext.Current == synchronizationContext)
                return Task.FromResult(func());
            var completion = new TaskCompletionSource<TResult>();
            synchronizationContext.Post(state => completion.AttemptSetResult(func), null);
            return completion.Task;
        }

        public static Task<TResult> ExecuteAsync<TResult>(this ISynchronized synchronizable, Func<TResult> func) => ExecuteAsync(synchronizable.SynchronizationContext, func);

        public static async Task ExecuteAsync(this SynchronizationContext synchronizationContext, Func<Task> asyncAction)
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

        public static Task ExecuteAsync(this ISynchronized synchronizable, Func<Task> asyncAction) => ExecuteAsync(synchronizable.SynchronizationContext, asyncAction);

        public static async Task<TResult> ExecuteAsync<TResult>(this SynchronizationContext synchronizationContext, Func<Task<TResult>> asyncFunc)
        {
            if (synchronizationContext == null || SynchronizationContext.Current == synchronizationContext)
                return await asyncFunc().ConfigureAwait(false);
            var completion = new TaskCompletionSource<TResult>();
            synchronizationContext.Post(async state => await completion.AttemptSetResultAsync(asyncFunc).ConfigureAwait(false), null);
            return await completion.Task.ConfigureAwait(false);
        }

        public static Task<TResult> ExecuteAsync<TResult>(this ISynchronized synchronizable, Func<Task<TResult>> asyncFunc) => ExecuteAsync(synchronizable.SynchronizationContext, asyncFunc);

        public static void SequentialExecute(this ISynchronized synchronizable, Action action) => Execute(synchronizable?.SynchronizationContext ?? Synchronization.DefaultSynchronizationContext, action);

        public static TReturn SequentialExecute<TReturn>(this ISynchronized synchronizable, Func<TReturn> func) => Execute(synchronizable?.SynchronizationContext ?? Synchronization.DefaultSynchronizationContext, func);

        public static Task SequentialExecuteAsync(this ISynchronized synchronizable, Action action) => ExecuteAsync(synchronizable?.SynchronizationContext ?? Synchronization.DefaultSynchronizationContext, action);

        public static Task<TResult> SequentialExecuteAsync<TResult>(this ISynchronized synchronizable, Func<TResult> func) => ExecuteAsync(synchronizable?.SynchronizationContext ?? Synchronization.DefaultSynchronizationContext, func);

        public static Task SequentialExecuteAsync(this ISynchronized synchronizable, Func<Task> asyncAction) => ExecuteAsync(synchronizable?.SynchronizationContext ?? Synchronization.DefaultSynchronizationContext, asyncAction);

        public static Task<TResult> SequentialExecuteAsync<TResult>(this ISynchronized synchronizable, Func<Task<TResult>> asyncFunc) => ExecuteAsync(synchronizable?.SynchronizationContext ?? Synchronization.DefaultSynchronizationContext, asyncFunc);
    }
}
