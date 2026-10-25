using CozyGachapon.Contracts;

namespace CozyGachapon.Audio
{
    public sealed class AudioDiagnosticsRecorder : IAudioDiagnosticsSink
    {
        public int ErrorCount { get; private set; }
        public int WarningCount { get; private set; }
        public AudioDiagnosticCode LastCode { get; private set; }
        public string LastCueId { get; private set; }

        public void ReportError(AudioDiagnosticCode code, string cueId)
        {
            ErrorCount++;
            LastCode = code;
            LastCueId = cueId;
        }

        public void ReportWarning(AudioDiagnosticCode code, string cueId)
        {
            WarningCount++;
            LastCode = code;
            LastCueId = cueId;
        }
    }
}
