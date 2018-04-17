namespace Gear.Caching
{
    public enum TryOperationStatus
    {
        Succeeded,
        KeyNotFound,
        DuplicateKey,
        ValueSourceThrew
    }
}
