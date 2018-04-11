using System.Threading;

namespace Gear.Components
{
    public interface IsSynchronizable
    {
        SynchronizationContext SynchronizationContext { get; }
    }
}
