using CozyGachapon.Contracts;

namespace CozyGachapon.Pooling
{
    public class ObjectPoolService : IPoolService
    {
        private readonly PoolingConfig _config;
        private readonly IPoolDiagnosticsSink _sink;
        private readonly PoolRegistry _registry = new PoolRegistry();

        public PoolServicePhase Phase { get; private set; }
        public int RegisteredCapacityTotal => _registry.TotalCapacity;

        public ObjectPoolService(PoolingConfig config, IPoolDiagnosticsSink sink)
        {
            _config = config;
            _sink = sink;
            Phase = PoolServicePhase.Accepting;
        }

        public PoolRegistrationResult Register(string id, int clientDeclaredMax, bool forbidRecycle)
        {
            PoolRegistrationError error = PoolRegistrationValidator.Validate(
                Phase,
                id,
                clientDeclaredMax,
                forbidRecycle,
                _config,
                _registry,
                out PoolDefinition definition,
                out int capacity);

            if (error != PoolRegistrationError.None)
            {
                _sink.ReportError(PoolRegistrationValidator.ToDiagnosticCode(error), id);
                return PoolRegistrationResult.Reject(error);
            }

            _registry.Add(new PoolRecord(id, definition, capacity));
            return PoolRegistrationResult.Accept(capacity);
        }

        public bool TryGetStats(string id, out PoolStats stats)
        {
            if (_registry.TryGet(id, out PoolRecord record))
            {
                stats = record.ToStats();
                return true;
            }

            stats = default;
            return false;
        }

        public void SetPhaseForTest(PoolServicePhase phase)
        {
            Phase = phase;
        }
    }
}
