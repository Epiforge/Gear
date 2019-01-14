namespace Gear.Components
{
    /// <summary>
    /// Provides the disposal status of an object
    /// </summary>
    public interface IDisposeStatus
    {
        /// <summary>
        /// Gets whether this object has been disposed
        /// </summary>
        bool IsDisposed { get; }
    }
}
