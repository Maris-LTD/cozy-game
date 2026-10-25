using System;
using CozyGachapon.Contracts;

namespace CozyGachapon.Tests.Audio
{
    public sealed class CountingAudioDiagnosticsSink : IAudioDiagnosticsSink
    {
        private readonly int[] _warningsByCode = new int[Enum.GetValues(typeof(AudioDiagnosticCode)).Length];
        private readonly int[] _errorsByCode = new int[Enum.GetValues(typeof(AudioDiagnosticCode)).Length];

        public int ErrorCount { get; private set; }
        public int WarningCount { get; private set; }
        public AudioDiagnosticCode LastErrorCode { get; private set; }
        public string LastErrorCueId { get; private set; }
        public AudioDiagnosticCode LastWarningCode { get; private set; }
        public string LastWarningCueId { get; private set; }

        public void ReportError(AudioDiagnosticCode code, string cueId)
        {
            ErrorCount++;
            _errorsByCode[(int)code]++;
            LastErrorCode = code;
            LastErrorCueId = cueId;
        }

        public void ReportWarning(AudioDiagnosticCode code, string cueId)
        {
            WarningCount++;
            _warningsByCode[(int)code]++;
            LastWarningCode = code;
            LastWarningCueId = cueId;
        }

        public int CountWarnings(AudioDiagnosticCode code)
        {
            return _warningsByCode[(int)code];
        }

        public int CountErrors(AudioDiagnosticCode code)
        {
            return _errorsByCode[(int)code];
        }
    }
}
