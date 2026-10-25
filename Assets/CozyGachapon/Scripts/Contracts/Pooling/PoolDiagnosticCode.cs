namespace CozyGachapon.Contracts
{
    public enum PoolDiagnosticCode
    {
        RegistrationWrongPhase = 0,
        RegistrationUnknownPoolId = 1,
        RegistrationMissingPrefab = 2,
        RegistrationDuplicatePoolId = 3,
        RegistrationInvalidDeclaredMax = 4,
        RegistrationCapacityExceedsPerPoolLimit = 5,
        RegistrationTotalCapacityExceeded = 6,
        RegistrationRecycleForbidden = 7,
        PrefabHasAudioSource = 8
    }
}
