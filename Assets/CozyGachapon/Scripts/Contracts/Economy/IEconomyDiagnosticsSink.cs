namespace CozyGachapon.Contracts
{
    public interface IEconomyDiagnosticsSink
    {
        void ReportError(EconomyDiagnosticCode code, string currencyId, double value);
        void ReportWarning(EconomyDiagnosticCode code, string currencyId, double value);
    }
}
