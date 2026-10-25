namespace CozyGachapon.Contracts
{
    public interface IPoolDiagnosticsSink
    {
        void ReportError(PoolDiagnosticCode code, string poolId);
        void ReportWarning(PoolDiagnosticCode code, string poolId);
    }
}
