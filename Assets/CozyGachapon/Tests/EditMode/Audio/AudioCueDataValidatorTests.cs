using System.Collections.Generic;
using CozyGachapon.Audio;
using CozyGachapon.Contracts;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Audio
{
    public class AudioCueDataValidatorTests : AudioMisconfigFixture
    {
        [Test]
        public void ShippedCueTableIsClean()
        {
            Assert.AreEqual(0, RunValidator().Count);
        }

        [Test]
        public void CueWithZeroClipsIsRejected()
        {
            Cue("Ui.Tap").SetClipsForTest(new AudioClip[0]);

            AudioConfigIssue issue = AssertSingleIssue(AudioConfigIssueCode.NoClip);

            Assert.AreEqual("Ui.Tap", issue.CueId);
            Assert.IsTrue(issue.IsError);
        }

        [Test]
        public void MaxInstancesBelowOneIsRejected()
        {
            SoundCue cue = Cue("Ui.Confirm");
            cue.SetLimitsForTest(cue.Priority, 0, cue.MinInterval);

            AudioConfigIssue issue = AssertSingleIssue(AudioConfigIssueCode.MaxInstancesInvalid);

            Assert.AreEqual("Ui.Confirm", issue.CueId);
            Assert.AreEqual(0f, issue.Value);
        }

        [Test]
        public void NegativeMinIntervalIsRejected()
        {
            SoundCue cue = Cue("Ui.Confirm");
            cue.SetLimitsForTest(cue.Priority, cue.MaxInstances, -0.01f);

            AudioConfigIssue issue = AssertSingleIssue(AudioConfigIssueCode.MinIntervalNegative);

            Assert.AreEqual("Ui.Confirm", issue.CueId);
            Assert.AreEqual(-0.01f, issue.Value, 0.00001f);
        }

        [Test]
        public void PriorityAboveTheMaximumIsRejected()
        {
            SoundCue cue = Cue("Ui.Confirm");
            cue.SetLimitsForTest(101, cue.MaxInstances, cue.MinInterval);

            AudioConfigIssue issue = AssertSingleIssue(AudioConfigIssueCode.PriorityOutOfRange);

            Assert.AreEqual("Ui.Confirm", issue.CueId);
            Assert.AreEqual(101f, issue.Value);
            Assert.AreEqual(Targets.CuePriorityMax, issue.Limit);
        }

        [Test]
        public void PriorityAtTheMaximumIsAccepted()
        {
            SoundCue cue = Cue("Ui.Confirm");
            cue.SetLimitsForTest(Targets.CuePriorityMax, cue.MaxInstances, cue.MinInterval);

            Assert.AreEqual(0, RunValidator().Count);
        }

        [Test]
        public void TwoCuesWithTheSameIdAreReportedOnce()
        {
            Cues.Add(Track(Object.Instantiate(Cue("Ui.Confirm"))));

            AudioConfigIssue issue = AssertSingleIssue(AudioConfigIssueCode.DuplicateCueId);

            Assert.AreEqual("Ui.Confirm", issue.CueId);
        }

        [Test]
        public void ScaleCueWithOnlyTheMiddleSampleNamesTheCueAndNote()
        {
            SoundCue cue = Cue("Plinko.PegHitScored");
            PitchedSample middle = new PitchedSample(CreateClipOfLength("middle", 0.4f), 12);
            cue.SetPitchForTest(PitchMode.Scale, cue.PitchJitterRatio, new[] { middle });

            AudioConfigIssue issue = AssertSingleIssue(AudioConfigIssueCode.PitchShiftExceeded);

            Assert.AreEqual("Plinko.PegHitScored", issue.CueId);
            Assert.AreEqual(0, issue.NoteIndex);
            Assert.AreEqual(-12f, issue.Value);
            Assert.AreEqual(DefaultConfig.MaxSampleShiftSemitones, issue.Limit);
        }

        [Test]
        public void ScaleCueWithoutSamplesNamesTheCue()
        {
            SoundCue cue = Cue("Plinko.JackpotBurstSpawn");
            cue.SetPitchForTest(PitchMode.Scale, cue.PitchJitterRatio, new PitchedSample[0]);

            AudioConfigIssue issue = AssertSingleIssue(AudioConfigIssueCode.ScaleSamplesMissing);

            Assert.AreEqual("Plinko.JackpotBurstSpawn", issue.CueId);
        }

        [Test]
        public void CueWithAnUnknownBusIsAnError()
        {
            Cue("Ui.Tap").SetIdentityForTest("Ui.Tap", (AudioBus)99);

            AudioConfigIssue issue = AssertSingleIssue(AudioConfigIssueCode.InvalidBus);

            Assert.AreEqual("Ui.Tap", issue.CueId);
            Assert.IsTrue(issue.IsError);
        }

        [Test]
        public void VolumeAboveZeroIsOnlyAWarning()
        {
            Cue("Plinko.JackpotHit").SetVolumeDbForTest(3f);

            List<AudioConfigIssue> issues = RunValidator();

            Assert.AreEqual(1, issues.Count);
            Assert.AreEqual(AudioConfigIssueCode.VolumeAboveZero, issues[0].Code);
            Assert.IsFalse(issues[0].IsError);
            Assert.AreEqual(0, AudioConfigValidator.CountErrors(issues));
        }
    }
}
