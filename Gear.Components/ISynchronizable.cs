using System.Threading;

namespace Gear.Components
{
    public interface ISynchronizable
    {
        bool IsSynchronized { get; set; }
        SynchronizationContext SynchronizationContext { get; }
    }
}
