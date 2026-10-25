using CozyGachapon.Contracts;
using UnityEngine;

namespace CozyGachapon.Pooling
{
    public sealed class PoolGetHandler
    {
        private readonly PoolRegistry _registry;
        private readonly IPoolDiagnosticsSink _sink;

        public PoolGetHandler(PoolRegistry registry, IPoolDiagnosticsSink sink)
        {
            _registry = registry;
            _sink = sink;
        }

        public PoolLease Get(PoolServicePhase phase, string poolId)
        {
            if (phase != PoolServicePhase.Ready)
            {
                _sink.ReportError(PoolDiagnosticCode.GetWrongPhase, poolId);
                return default;
            }

            if (!_registry.TryGet(poolId, out PoolRecord record))
            {
                _sink.ReportError(PoolDiagnosticCode.GetUnknownPool, poolId);
                return default;
            }

            if (!record.TryPopFree(out int slot))
            {
                return default;
            }

            GameObject instance = record.GetInstance(slot);
            instance.SetActive(true);
            long sequence = record.MarkAcquired(slot);
            record.Counters.OnAcquired();
            if (record.Hooks != null)
            {
                record.Hooks.OnAcquire(instance);
            }

            return new PoolLease(record.GetInstanceId(slot), record.Id, sequence, instance);
        }
    }
}
