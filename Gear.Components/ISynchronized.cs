using System.Threading;

namespace Gear.Components
{
    public interface ISynchronized
    {
        SynchronizationContext SynchronizationContext { get; }
    }
}
