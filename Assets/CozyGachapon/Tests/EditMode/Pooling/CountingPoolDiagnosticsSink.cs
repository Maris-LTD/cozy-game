using CozyGachapon.Contracts;

namespace CozyGachapon.Tests.Pooling
{
    public class CountingPoolDiagnosticsSink : IPoolDiagnosticsSink
    {
        public int ErrorCount { get; private set; }
        public int WarningCount { get; private set; }
        public PoolDiagnosticCode LastErrorCode { get; private set; }
        public string LastErrorPoolId { get; private set; }
        public PoolDiagnosticCode LastWarningCode { get; private set; }
        public string LastWarningPoolId { get; private set; }

        public void ReportError(PoolDiagnosticCode code, string poolId)
        {
            ErrorCount++;
            LastErrorCode = code;
            LastErrorPoolId = poolId;
        }

        public void ReportWarning(PoolDiagnosticCode code, string poolId)
        {
            WarningCount++;
            LastWarningCode = code;
            LastWarningPoolId = poolId;
        }
    }
}
