using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Gear.Components
{
    public static class SynchronizedExtensions
    {
        public static void Execute(this ISynchronized synchronizable, Action action)
        {
            if (synchronizable.SynchronizationContext == null || SynchronizationContext.Current == synchronizable.SynchronizationContext)
            {
                action();
                return;
            }
            ExceptionDispatchInfo edi = default;
            synchronizable.SynchronizationContext.Send(state =>
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

        public static TReturn Execute<TReturn>(this ISynchronized synchronizable, Func<TReturn> func)
        {
            if (synchronizable.SynchronizationContext == null || SynchronizationContext.Current == synchronizable.SynchronizationContext)
                return func();
            TReturn result = default;
            ExceptionDispatchInfo edi = default;
            synchronizable.SynchronizationContext.Send(state =>
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

        public static Task ExecuteAsync(this ISynchronized synchronizable, Action action)
        {
            if (synchronizable.SynchronizationContext == null || SynchronizationContext.Current == synchronizable.SynchronizationContext)
            {
                action();
                return Task.CompletedTask;
            }
            var completion = new TaskCompletionSource<object>();
            synchronizable.SynchronizationContext.Post(state => completion.AttemptSetResult(action), null);
            return completion.Task;
        }

        public static Task<TResult> ExecuteAsync<TResult>(this ISynchronized synchronizable, Func<TResult> func)
        {
            if (synchronizable.SynchronizationContext == null || SynchronizationContext.Current == synchronizable.SynchronizationContext)
                return Task.FromResult(func());
            var completion = new TaskCompletionSource<TResult>();
            synchronizable.SynchronizationContext.Post(state => completion.AttemptSetResult(func), null);
            return completion.Task;
        }

        public static async Task ExecuteAsync(this ISynchronized synchronizable, Func<Task> asyncAction)
        {
            if (synchronizable.SynchronizationContext == null || SynchronizationContext.Current == synchronizable.SynchronizationContext)
            {
                await asyncAction().ConfigureAwait(false);
                return;
            }
            var completion = new TaskCompletionSource<object>();
            synchronizable.SynchronizationContext.Post(async state => await completion.AttemptSetResultAsync(asyncAction).ConfigureAwait(false), null);
            await completion.Task.ConfigureAwait(false);
        }

        public static async Task<TResult> ExecuteAsync<TResult>(this ISynchronized synchronizable, Func<Task<TResult>> asyncFunc)
        {
            if (synchronizable.SynchronizationContext == null || SynchronizationContext.Current == synchronizable.SynchronizationContext)
                return await asyncFunc().ConfigureAwait(false);
            var completion = new TaskCompletionSource<TResult>();
            synchronizable.SynchronizationContext.Post(async state => await completion.AttemptSetResultAsync(asyncFunc).ConfigureAwait(false), null);
            return await completion.Task.ConfigureAwait(false);
        }
    }
}
