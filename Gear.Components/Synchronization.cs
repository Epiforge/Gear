using System.Threading;

namespace Gear.Components
{
    public static class Synchronization
    {
        static readonly AsyncSynchronizationContext asyncSynchronizationContext = new AsyncSynchronizationContext();

        public static SynchronizationContext DefaultSynchronizationContext => asyncSynchronizationContext;
    }
}
