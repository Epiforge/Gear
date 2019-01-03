using System.Threading;
using System.Threading.Tasks;

namespace Gear.Components
{
    /// <summary>
    /// Provides an asynchronous mechanism for releasing unmanaged resources
    /// </summary>
    public interface IAsyncDisposable
    {
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources
        /// </summary>
        /// <param name="cancellationToken">The cancellation token used to cancel the disposal</param>
        Task DisposeAsync(CancellationToken cancellationToken = default);
    }
}
