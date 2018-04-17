using Gear.Components;
using System;

namespace Gear.Caching
{
    public class TryOperationResult : IEquatable<TryOperationResult>
    {
        public static TryOperationResult DuplicateKey = new TryOperationResult(TryOperationStatus.DuplicateKey);
        public static TryOperationResult KeyNotFound = new TryOperationResult(TryOperationStatus.KeyNotFound);
        public static TryOperationResult Succeeded = new TryOperationResult(TryOperationStatus.Succeeded);

        TryOperationResult(TryOperationStatus status) => Status = status;

        public TryOperationResult(Exception ex)
        {
            Status = TryOperationStatus.ValueSourceThrew;
            Exception = ex;
        }

        public override bool Equals(object obj) => Equals(obj as TryOperationResult);

        public bool Equals(TryOperationResult other) => other?.Status == Status && other?.Exception == Exception;

        public override int GetHashCode() => HashCodes.CombineObjects(typeof(TryOperationResult), Status, Exception);

        public TryOperationStatus Status { get; }
        public Exception Exception { get; }
    }
}
