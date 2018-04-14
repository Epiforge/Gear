using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace Gear.Components
{
    public static class TaskResolver
    {
        static readonly ConcurrentDictionary<Type, FastMethodInfo> taskValueGetters = new ConcurrentDictionary<Type, FastMethodInfo>();

        static FastMethodInfo CreateTaskValueGetter(Type type) => new FastMethodInfo(type.GetRuntimeProperty(nameof(Task<object>.Result)).GetMethod);

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
