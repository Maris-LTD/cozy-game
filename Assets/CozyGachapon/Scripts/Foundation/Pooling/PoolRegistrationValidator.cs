using CozyGachapon.Contracts;

namespace CozyGachapon.Pooling
{
    public static class PoolRegistrationValidator
    {
        public static PoolRegistrationError Validate(
            PoolServicePhase phase,
            string id,
            int clientDeclaredMax,
            bool forbidRecycle,
            PoolingConfig config,
            PoolRegistry registry,
            out PoolDefinition definition,
            out int capacity)
        {
            definition = null;
            capacity = 0;

            if (phase != PoolServicePhase.Accepting)
            {
                return PoolRegistrationError.WrongPhase;
            }

            if (id == null || !config.TryGetDefinition(id, out definition))
            {
                return PoolRegistrationError.UnknownPoolId;
            }

            if (definition.Prefab == null)
            {
                return PoolRegistrationError.MissingPrefab;
            }

            if (registry.Contains(id))
            {
                return PoolRegistrationError.DuplicatePoolId;
            }

            if (clientDeclaredMax < 1)
            {
                return PoolRegistrationError.InvalidDeclaredMax;
            }

            long wideCapacity = PoolCapacity.ComputeWide(clientDeclaredMax, definition.Headroom);

            if (wideCapacity < 1)
            {
                return PoolRegistrationError.InvalidDeclaredMax;
            }

            if (wideCapacity > config.MaxCapacityPerPool)
            {
                return PoolRegistrationError.CapacityExceedsPerPoolLimit;
            }

            if (registry.TotalCapacity + wideCapacity > config.MaxTotalPooledObjects)
            {
                return PoolRegistrationError.TotalCapacityExceeded;
            }

            capacity = (int)wideCapacity;

            if (definition.Policy == PoolExhaustionPolicy.RecycleOldest && forbidRecycle)
            {
                return PoolRegistrationError.RecycleForbidden;
            }

            return PoolRegistrationError.None;
        }

        public static PoolDiagnosticCode ToDiagnosticCode(PoolRegistrationError error)
        {
            switch (error)
            {
                case PoolRegistrationError.WrongPhase:
                    return PoolDiagnosticCode.RegistrationWrongPhase;
                case PoolRegistrationError.UnknownPoolId:
                    return PoolDiagnosticCode.RegistrationUnknownPoolId;
                case PoolRegistrationError.MissingPrefab:
                    return PoolDiagnosticCode.RegistrationMissingPrefab;
                case PoolRegistrationError.DuplicatePoolId:
                    return PoolDiagnosticCode.RegistrationDuplicatePoolId;
                case PoolRegistrationError.InvalidDeclaredMax:
                    return PoolDiagnosticCode.RegistrationInvalidDeclaredMax;
                case PoolRegistrationError.CapacityExceedsPerPoolLimit:
                    return PoolDiagnosticCode.RegistrationCapacityExceedsPerPoolLimit;
                case PoolRegistrationError.TotalCapacityExceeded:
                    return PoolDiagnosticCode.RegistrationTotalCapacityExceeded;
                default:
                    return PoolDiagnosticCode.RegistrationRecycleForbidden;
            }
        }
    }
}
