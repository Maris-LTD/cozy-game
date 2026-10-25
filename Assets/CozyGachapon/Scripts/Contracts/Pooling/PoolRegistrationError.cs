namespace CozyGachapon.Contracts
{
    public enum PoolRegistrationError
    {
        None = 0,
        WrongPhase = 1,
        UnknownPoolId = 2,
        MissingPrefab = 3,
        DuplicatePoolId = 4,
        InvalidDeclaredMax = 5,
        CapacityExceedsPerPoolLimit = 6,
        TotalCapacityExceeded = 7,
        RecycleForbidden = 8
    }
}
