using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace Gear.Components
{
    /// <summary>
    /// Provides consumers of methods that might be asynchronous with means to resolve their return values without concern for their their disposition towards synchrony
    /// </summary>
    public static class TaskResolver
    {
        static readonly ConcurrentDictionary<Type, FastMethodInfo> taskValueGetters = new ConcurrentDictionary<Type, FastMethodInfo>();

        static FastMethodInfo CreateTaskValueGetter(Type type) => new FastMethodInfo(type.GetRuntimeProperty(nameof(Task<object>.Result)).GetMethod);

        /// <summary>
        /// Ensures that if a specified object is a <see cref="Task"/>, it is properly awaited and if it is a <see cref="Task{TResult}"/>, the result is retrieved
        /// </summary>
        /// <param name="potentialTask">The object that may be a <see cref="Task"/> or <see cref="Task{TResult}"/></param>
        /// <returns>The result of the task once it is complete if <paramref name="potentialTask"/> is a <see cref="Task{TResult}"/>; or, <paramref name="potentialTask"/> once it is complete if <paramref name="potentialTask"/> is a <see cref="Task"/>; otherwise, <paramref name="potentialTask"/> (immediately)</returns>
        public static async Task<object> ResolveAsync(object potentialTask)
        {
            if (potentialTask is Task task)
            {
                ExceptionDispatchInfo edi = default;
                await task.ContinueWith(t =>
                {
                    if (t.IsFaulted)
                        edi = ExceptionDispatchInfo.Capture(t.Exception);
                    else
                    {
                        var type = t.GetType();
                        if (type.IsConstructedGenericType)
                            potentialTask = taskValueGetters.GetOrAdd(type, CreateTaskValueGetter).Invoke(t);
                    }
                }).ConfigureAwait(false);
                edi?.Throw();
            }
            return potentialTask;
        }
    }
}
