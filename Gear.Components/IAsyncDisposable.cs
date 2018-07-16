using System.Threading;
using System.Threading.Tasks;

namespace Gear.Components
{
    public interface IAsyncDisposable
    {
        Task DisposeAsync(CancellationToken cancellationToken = default);
    }
}
