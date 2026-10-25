using CozyGachapon.Contracts;
using UnityEngine;

namespace CozyGachapon.Pooling
{
    public sealed class PoolReleaseHandler
    {
        private readonly PoolRegistry _registry;
        private readonly IPoolDiagnosticsSink _sink;

        public PoolReleaseHandler(PoolRegistry registry, IPoolDiagnosticsSink sink)
        {
            _registry = registry;
            _sink = sink;
        }

        public void Release(PoolInstanceTable table, string poolId, in PoolLease lease)
        {
            if (!_registry.TryGet(poolId, out PoolRecord record))
            {
                _sink.ReportError(lease.IsValid ? PoolDiagnosticCode.ReleaseUnknownPool : PoolDiagnosticCode.ReleaseForeign, poolId);
                return;
            }

            PoolReleaseVerdict verdict = PoolLeaseValidator.Classify(record, table, lease, out int slot);
            if (verdict != PoolReleaseVerdict.Valid)
            {
                RejectRelease(record, verdict);
                return;
            }

            GameObject instance = record.GetInstance(slot);
            instance.SetActive(false);
            record.MarkReleased(slot);
            record.PushFree(slot);
            record.Counters.OnReleased();
            if (record.Hooks != null)
            {
                record.Hooks.OnReturn(instance, PoolReturnReason.Released);
            }
        }

        private void RejectRelease(PoolRecord record, PoolReleaseVerdict verdict)
        {
            switch (verdict)
            {
                case PoolReleaseVerdict.Foreign:
                    record.Counters.AddForeignRelease();
                    _sink.ReportError(PoolDiagnosticCode.ReleaseForeign, record.Id);
                    break;
                case PoolReleaseVerdict.Double:
                    record.Counters.AddDoubleRelease();
                    _sink.ReportError(PoolDiagnosticCode.ReleaseDouble, record.Id);
                    break;
                default:
                    record.Counters.AddStaleRelease();
                    _sink.ReportError(PoolDiagnosticCode.ReleaseStale, record.Id);
                    break;
            }
        }
    }
}
