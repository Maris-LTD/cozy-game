namespace CozyGachapon.Contracts
{
    public interface IPoolService
    {
        PoolServicePhase Phase { get; }
        PoolRegistrationResult Register(string id, int clientDeclaredMax, bool forbidRecycle);
        PoolRegistrationResult Register(string id, int clientDeclaredMax, bool forbidRecycle, IPoolHooks hooks);
        PoolLease Get(string poolId);
        void Release(in PoolLease lease);
        void Release(string poolId, in PoolLease lease);
        bool TryGetStats(string id, out PoolStats stats);
    }
}
