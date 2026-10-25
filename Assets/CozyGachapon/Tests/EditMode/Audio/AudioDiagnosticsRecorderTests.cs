using CozyGachapon.Audio;
using CozyGachapon.Contracts;
using NUnit.Framework;

namespace CozyGachapon.Tests.Audio
{
    public class AudioDiagnosticsRecorderTests
    {
        [Test]
        public void New_HasNoReports()
        {
            AudioDiagnosticsRecorder recorder = new AudioDiagnosticsRecorder();

            Assert.AreEqual(0, recorder.ErrorCount);
            Assert.AreEqual(0, recorder.WarningCount);
            Assert.IsNull(recorder.LastCueId);
        }

        [Test]
        public void ReportError_CountsAndRecordsLast()
        {
            AudioDiagnosticsRecorder recorder = new AudioDiagnosticsRecorder();

            recorder.ReportError(AudioDiagnosticCode.UnknownCue, "Missing.Cue");

            Assert.AreEqual(1, recorder.ErrorCount);
            Assert.AreEqual(0, recorder.WarningCount);
            Assert.AreEqual(AudioDiagnosticCode.UnknownCue, recorder.LastCode);
            Assert.AreEqual("Missing.Cue", recorder.LastCueId);
        }

        [Test]
        public void ReportWarning_CountsIndependently()
        {
            AudioDiagnosticsRecorder recorder = new AudioDiagnosticsRecorder();

            recorder.ReportError(AudioDiagnosticCode.InitializeInvalidInput, null);
            recorder.ReportWarning(AudioDiagnosticCode.CueExcluded, "Excluded.Cue");
            recorder.ReportWarning(AudioDiagnosticCode.CueExcluded, "Other.Cue");

            Assert.AreEqual(1, recorder.ErrorCount);
            Assert.AreEqual(2, recorder.WarningCount);
            Assert.AreEqual(AudioDiagnosticCode.CueExcluded, recorder.LastCode);
            Assert.AreEqual("Other.Cue", recorder.LastCueId);
        }
    }
}
