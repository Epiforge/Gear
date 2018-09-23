using System.ComponentModel;
using System.Threading;

namespace Gear.Components
{
    public interface ISynchronizable : INotifyPropertyChanged
    {
        bool IsSynchronized { get; set; }
        SynchronizationContext SynchronizationContext { get; }
    }
}
