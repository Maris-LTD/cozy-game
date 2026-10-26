using CozyGachapon.Contracts;

namespace CozyGachapon.Economy
{
    public sealed class EconomyDiagnosticsRecorder : IEconomyDiagnosticsSink
    {
        public int ErrorCount { get; private set; }
        public int WarningCount { get; private set; }
        public EconomyDiagnosticCode LastCode { get; private set; }
        public string LastCurrencyId { get; private set; }
        public double LastValue { get; private set; }

        public void ReportError(EconomyDiagnosticCode code, string currencyId, double value)
        {
            ErrorCount++;
            LastCode = code;
            LastCurrencyId = currencyId;
            LastValue = value;
        }

        public void ReportWarning(EconomyDiagnosticCode code, string currencyId, double value)
        {
            WarningCount++;
            LastCode = code;
            LastCurrencyId = currencyId;
            LastValue = value;
        }
    }
}
