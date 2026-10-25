using CozyGachapon.Contracts;

namespace CozyGachapon.Pooling
{
    public static class ExhaustionReporter
    {
        public static void Report(PoolRecord record, IPoolDiagnosticsSink sink)
        {
            if (record.Counters.AddExhausted())
            {
                sink.ReportWarning(PoolDiagnosticCode.PoolExhausted, record.Id);
            }
        }
    }
}
