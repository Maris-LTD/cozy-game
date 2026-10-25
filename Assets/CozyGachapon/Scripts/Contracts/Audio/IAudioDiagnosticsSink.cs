namespace CozyGachapon.Contracts
{
    public interface IAudioDiagnosticsSink
    {
        void ReportError(AudioDiagnosticCode code, string cueId);
        void ReportWarning(AudioDiagnosticCode code, string cueId);
    }
}
