using CozyGachapon.Contracts;

namespace CozyGachapon.Pooling
{
    public sealed class RecycleOldestPolicy : IExhaustionStrategy
    {
        private readonly IPoolDiagnosticsSink _sink;

        public RecycleOldestPolicy(IPoolDiagnosticsSink sink)
        {
            _sink = sink;
        }

        public bool TryReclaim(PoolRecord record, out int slot)
        {
            if (!record.TryFindOldestInUse(out slot))
            {
                ExhaustionReporter.Report(record, _sink);
                return false;
            }

            if (record.Hooks != null)
            {
                record.Hooks.OnReturn(record.GetInstance(slot), PoolReturnReason.Recycled);
            }

            return true;
        }
    }
}
