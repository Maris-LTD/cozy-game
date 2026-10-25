namespace CozyGachapon.Contracts
{
    public interface IPoolService
    {
        PoolServicePhase Phase { get; }
        PoolRegistrationResult Register(string id, int clientDeclaredMax, bool forbidRecycle);
        bool TryGetStats(string id, out PoolStats stats);
    }
}
