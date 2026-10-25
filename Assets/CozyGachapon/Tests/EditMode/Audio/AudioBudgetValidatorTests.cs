using CozyGachapon.Audio;
using NUnit.Framework;

namespace CozyGachapon.Tests.Audio
{
    public class AudioBudgetValidatorTests : AudioMisconfigFixture
    {
        [Test]
        public void OneMoreStormInstanceBreaksTheVoiceBudget()
        {
            SoundCue cue = Cue("Plinko.PegHitUnscored");
            cue.SetLimitsForTest(cue.Priority, cue.MaxInstances + 1, cue.MinInterval);

            AudioConfigIssue issue = AssertSingleIssue(AudioConfigIssueCode.VoiceBudgetExceeded);

            Assert.AreEqual("storm_instances", issue.Subject);
            Assert.AreEqual(13f, issue.Value);
            Assert.AreEqual(12f, issue.Limit);
        }

        [Test]
        public void ZeroDbPegHitBreaksTheStormPeakGain()
        {
            Cue("Plinko.PegHitScored").SetVolumeDbForTest(0f);

            AudioConfigIssue issue = AssertSingleIssue(AudioConfigIssueCode.StormPeakGainExceeded);

            Assert.AreEqual("storm_peak_gain", issue.Subject);
            Assert.Greater(issue.Value, Targets.StormPeakGainMax);
            Assert.AreEqual(Targets.StormPeakGainMax, issue.Limit);
        }

        [Test]
        public void ShortMinIntervalBreaksTheStealAge()
        {
            SoundCue cue = Cue("Plinko.PegHitUnscored");
            cue.SetLimitsForTest(cue.Priority, cue.MaxInstances, 0.08f);

            AudioConfigIssue issue = AssertSingleIssue(AudioConfigIssueCode.StealAgeTooShort);

            Assert.AreEqual("Plinko.PegHitUnscored", issue.CueId);
            Assert.AreEqual(0.16f, issue.Value, 0.0001f);
            Assert.AreEqual(Targets.MinStealAgeSeconds, issue.Limit);
        }

        [Test]
        public void StealAgeEqualToTheMinimumIsValid()
        {
            SoundCue cue = Cue("Plinko.PegHitUnscored");
            cue.SetLimitsForTest(cue.Priority, 2, 0.10f);

            Assert.AreEqual(0, RunValidator().Count);
        }

        [Test]
        public void StealAgeOnTheMinimumWithFloatRoundingIsValid()
        {
            SoundCue cue = Cue("Plinko.PegHitScored");
            cue.SetLimitsForTest(cue.Priority, 5, 0.04f);

            Assert.AreEqual(0, RunValidator().Count);
        }

        [Test]
        public void StormInstancesEqualToTheCapacityAreValid()
        {
            Assert.AreEqual(AudioBudgetMath.StormVoiceCapacity(DefaultConfig), AudioBudgetMath.StormInstances(DefaultConfig, Cues));
            Assert.AreEqual(0, RunValidator().Count);
        }
    }
}
