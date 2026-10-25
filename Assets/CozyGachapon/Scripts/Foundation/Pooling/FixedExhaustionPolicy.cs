using CozyGachapon.Contracts;

namespace CozyGachapon.Pooling
{
    public sealed class FixedExhaustionPolicy : IExhaustionStrategy
    {
        private readonly IPoolDiagnosticsSink _sink;

        public FixedExhaustionPolicy(IPoolDiagnosticsSink sink)
        {
            _sink = sink;
        }

        public bool TryReclaim(PoolRecord record, out int slot)
        {
            slot = -1;
            ExhaustionReporter.Report(record, _sink);

            return false;
        }
    }
}
