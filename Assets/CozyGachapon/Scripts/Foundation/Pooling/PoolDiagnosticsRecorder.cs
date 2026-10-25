using CozyGachapon.Contracts;

namespace CozyGachapon.Pooling
{
    public sealed class PoolDiagnosticsRecorder : IPoolDiagnosticsSink
    {
        public int ErrorCount { get; private set; }
        public int WarningCount { get; private set; }
        public PoolDiagnosticCode LastCode { get; private set; }

        public void ReportError(PoolDiagnosticCode code, string poolId)
        {
            ErrorCount++;
            LastCode = code;
        }

        public void ReportWarning(PoolDiagnosticCode code, string poolId)
        {
            WarningCount++;
            LastCode = code;
        }
    }
}
