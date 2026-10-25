using System.Collections.Generic;
using CozyGachapon.Audio;
using CozyGachapon.Contracts;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Audio
{
    public class AudioSpawnClipValidatorTests : AudioMisconfigFixture
    {
        private const float Epsilon = 0.0001f;

        [Test]
        public void ManualSpawnClipFiftyMillisecondsOverTheLimitNamesCueClipAndTimes()
        {
            float length = Targets.ManualSpawnClipMaxSeconds + 0.05f;
            Cue("Plinko.CoinSpawn").SetClipsForTest(new[] { CreateClipOfLength("tok_long", length) });

            AudioConfigIssue issue = AssertSingleIssue(AudioConfigIssueCode.SpawnClipTooLong);

            Assert.AreEqual("Plinko.CoinSpawn", issue.CueId);
            Assert.AreEqual("tok_long", issue.Subject);
            Assert.AreEqual(length, issue.Value, Epsilon);
            Assert.AreEqual(length / 0.97f, issue.SecondaryValue, Epsilon);
            Assert.AreEqual(Targets.ManualSpawnClipMaxSeconds, issue.Limit);
        }

        [Test]
        public void AutoSpawnClipAtExactlyTheLimitIsCaughtByItsRealPlayTime()
        {
            Cue("Plinko.CoinSpawnAuto").SetClipsForTest(new[] { CreateClipOfLength("tok_edge", Targets.ManualSpawnClipMaxSeconds) });

            AudioConfigIssue issue = AssertSingleIssue(AudioConfigIssueCode.SpawnClipTooLong);

            Assert.AreEqual("Plinko.CoinSpawnAuto", issue.CueId);
            Assert.AreEqual(0.12f, issue.Value, Epsilon);
            Assert.AreEqual(0.124f, issue.SecondaryValue, 0.001f);
            Assert.Greater(issue.SecondaryValue, issue.Limit);
        }

        [Test]
        public void SpawnClipShortEnoughForItsPitchJitterIsValid()
        {
            float length = Targets.ManualSpawnClipMaxSeconds * (1f - Cue("Plinko.CoinSpawnAuto").PitchJitterRatio) - 0.001f;
            Cue("Plinko.CoinSpawnAuto").SetClipsForTest(new[] { CreateClipOfLength("tok_fit", length) });

            Assert.AreEqual(0, RunValidator().Count);
        }

        [Test]
        public void ManualLimitEqualToTheCooldownBreaksTheMarginBound()
        {
            Targets.SetSpawnClipLimitsForTest(DefaultAudioAssets.TapCooldownSeconds, Targets.SpawnClipMarginSeconds);

            AudioConfigIssue issue = AssertSingleIssue(AudioConfigIssueCode.ManualSpawnClipExceedsCooldownMargin);

            Assert.AreEqual("manualSpawnClipMaxSeconds", issue.Subject);
            Assert.AreEqual(DefaultAudioAssets.TapCooldownSeconds, issue.Value, Epsilon);
            Assert.AreEqual(DefaultAudioAssets.TapCooldownSeconds - Targets.SpawnClipMarginSeconds, issue.Limit, Epsilon);
        }

        [Test]
        public void MarginBelowTheSafeRangeIsRejected()
        {
            Targets.SetSpawnClipLimitsForTest(Targets.ManualSpawnClipMaxSeconds, 0.01f);

            AudioConfigIssue issue = AssertSingleIssue(AudioConfigIssueCode.SpawnClipMarginOutOfRange);

            Assert.AreEqual("spawnClipMarginSeconds", issue.Subject);
            Assert.AreEqual(0.01f, issue.Value, Epsilon);
            Assert.AreEqual(Targets.SpawnClipMarginMinSeconds, issue.Limit);
        }

        [Test]
        public void MarginAboveTheSafeRangeIsRejected()
        {
            Targets.SetSpawnClipLimitsForTest(Targets.ManualSpawnClipMaxSeconds, 0.06f);

            List<AudioConfigIssue> issues = RunValidator();

            AudioConfigIssue found = default;
            for (int i = 0; i < issues.Count; i++)
            {
                if (issues[i].Code == AudioConfigIssueCode.SpawnClipMarginOutOfRange)
                {
                    found = issues[i];
                }
            }

            Assert.AreEqual(AudioConfigIssueCode.SpawnClipMarginOutOfRange, found.Code);
            Assert.AreEqual("spawnClipMarginSeconds", found.Subject);
            Assert.AreEqual(Targets.SpawnClipMarginMaxSeconds, found.Limit);
        }

        [Test]
        public void SpawnCueThatStealsIsRejected()
        {
            Cue("Plinko.CoinSpawn").SetInstanceCapPolicyForTest(InstanceCapPolicy.StealOldest);

            List<AudioConfigIssue> issues = RunValidator();

            AudioConfigIssue notDropNew = issues.Find(issue => issue.Code == AudioConfigIssueCode.SpawnCueNotDropNew);
            Assert.AreEqual("Plinko.CoinSpawn", notDropNew.CueId);
        }

        [Test]
        public void MissingSpawnCueIsReported()
        {
            Cues.Remove(Cue("Plinko.CoinSpawnAuto"));

            AudioConfigIssue issue = AssertSingleIssue(AudioConfigIssueCode.SpawnCueMissing);

            Assert.AreEqual("Plinko.CoinSpawnAuto", issue.CueId);
        }
    }
}
