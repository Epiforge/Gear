namespace Gear.Caching
{
    /// <summary>
    /// The status of attempting an operation on a cache
    /// </summary>
    public enum TryOperationStatus
    {
        /// <summary>
        /// The operation succeeded
        /// </summary>
        Succeeded,

        /// <summary>
        /// The specified key could not be found
        /// </summary>
        KeyNotFound,

        /// <summary>
        /// The specified key was already present
        /// </summary>
        DuplicateKey,

        /// <summary>
        /// The factory method for the cache value threw an exception
        /// </summary>
        ValueSourceThrew
    }
}
