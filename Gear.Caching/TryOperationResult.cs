using Gear.Components;
using System;

namespace Gear.Caching
{
    /// <summary>
    /// The result of attempting an operation on a cache
    /// </summary>
    public class TryOperationResult : IEquatable<TryOperationResult>
    {
        internal static TryOperationResult DuplicateKey = new TryOperationResult(TryOperationStatus.DuplicateKey);
        internal static TryOperationResult KeyNotFound = new TryOperationResult(TryOperationStatus.KeyNotFound);
        internal static TryOperationResult Succeeded = new TryOperationResult(TryOperationStatus.Succeeded);

        TryOperationResult(TryOperationStatus status) => Status = status;

        /// <summary>
        /// Creates a new instance of the <see cref="TryOperationResult"/> class
        /// </summary>
        /// <param name="ex"></param>
        public TryOperationResult(Exception ex)
        {
            Status = TryOperationStatus.ValueSourceThrew;
            Exception = ex;
            base.GetHashCode();
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object
        /// </summary>
        /// <param name="obj">An object to compare with this object</param>
        public override bool Equals(object obj) => Equals(obj as TryOperationResult);

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type
        /// </summary>
        /// <param name="other">An object to compare with this object</param>
        public bool Equals(TryOperationResult other) => other?.Status == Status && other?.Exception == Exception;

        /// <summary>
        /// Serves as the hash function
        /// </summary>
        public override int GetHashCode() => HashCodes.CombineObjects(typeof(TryOperationResult), Status, Exception);

        /// <summary>
        /// Gets the status of the attempted operation
        /// </summary>
        public TryOperationStatus Status { get; }

        /// <summary>
        /// Gets the exception encountered while attempting the operation
        /// </summary>
        public Exception Exception { get; }
    }
}
