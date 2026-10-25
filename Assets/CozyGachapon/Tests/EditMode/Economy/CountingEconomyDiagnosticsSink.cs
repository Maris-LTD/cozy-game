using System;
using CozyGachapon.Contracts;

namespace CozyGachapon.Tests.Economy
{
    public sealed class CountingEconomyDiagnosticsSink : IEconomyDiagnosticsSink
    {
        private readonly int[] _warningsByCode = new int[Enum.GetValues(typeof(EconomyDiagnosticCode)).Length];
        private readonly int[] _errorsByCode = new int[Enum.GetValues(typeof(EconomyDiagnosticCode)).Length];

        public int ErrorCount { get; private set; }
        public int WarningCount { get; private set; }
        public string LastWarningCurrencyId { get; private set; }
        public double LastWarningValue { get; private set; }

        public void ReportError(EconomyDiagnosticCode code, string currencyId, double value)
        {
            ErrorCount++;
            _errorsByCode[(int)code]++;
        }

        public void ReportWarning(EconomyDiagnosticCode code, string currencyId, double value)
        {
            WarningCount++;
            _warningsByCode[(int)code]++;
            LastWarningCurrencyId = currencyId;
            LastWarningValue = value;
        }

        public int CountWarnings(EconomyDiagnosticCode code)
        {
            return _warningsByCode[(int)code];
        }

        public int CountErrors(EconomyDiagnosticCode code)
        {
            return _errorsByCode[(int)code];
        }
    }
}
