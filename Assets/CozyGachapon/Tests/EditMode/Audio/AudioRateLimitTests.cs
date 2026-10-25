using CozyGachapon.Audio;
using CozyGachapon.Contracts;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Audio
{
    public class AudioRateLimitTests : AudioServiceFixture
    {
        private const string PegHitScoredId = "Plinko.PegHitScored";
        private const double Epsilon = 1e-4d;
        private const double T0 = 0d;

        private float _savedTimeScale;

        [SetUp]
        public void SaveTimeScale()
        {
            _savedTimeScale = Time.timeScale;
        }

        [TearDown]
        public void RestoreTimeScale()
        {
            Time.timeScale = _savedTimeScale;
        }

        [Test]
        public void AC8_PegHitScored_PlayedRateLimitedPlayed_DroppedRequestDoesNotMoveStamp()
        {
            InitializeWithDefaultAssets();
            double minInterval = ReadPegHitMinInterval();
            Assert.Greater(minInterval, Epsilon);

            PlayResult[] results = RunRateLimitScenario(minInterval);

            AssertPlayed(results[0]);
            AssertDropped(DropReason.RateLimited, results[1]);
            AssertPlayed(results[2]);
            Assert.AreEqual(2, Service.Stats.Played);
            Assert.AreEqual(1, Service.Stats.DroppedRateLimited);
        }

        [Test]
        public void AC8_TwoRequestsAtSameInstant_FirstPlayedSecondRateLimited()
        {
            InitializeWithDefaultAssets();
            Assert.Greater(ReadPegHitMinInterval(), 0d);

            AssertPlayed(PlayAt(PegHitScoredId, T0));
            AssertDropped(DropReason.RateLimited, PlayAt(PegHitScoredId, T0));
            Assert.AreEqual(1, Backend.PlayingCount);
        }

        [Test]
        public void AC8_RequestExactlyOneTenthIntervalLater_IsPlayed()
        {
            SoundCue cue = CreateLimitedCue("Test.TenthInterval", AudioBus.Plinko, 4, 0.1f, InstanceCapPolicy.DropNew);
            InitializeWithCues(cue);

            AssertPlayed(PlayAt("Test.TenthInterval", 1.0d));
            AssertPlayed(PlayAt("Test.TenthInterval", 1.1d));
        }

        [Test]
        public void AC29_TimeScaleZeroAndTwo_RateLimitResultsIdenticalToRealTime()
        {
            PlayResult[] baseline = RunScenarioWithTimeScale(1f);
            PlayResult[] frozen = RunScenarioWithTimeScale(0f);
            PlayResult[] fast = RunScenarioWithTimeScale(2f);

            Assert.AreEqual(Time.timeScale, _savedTimeScale);
            AssertSameResults(baseline, frozen);
            AssertSameResults(baseline, fast);
            AssertPlayed(frozen[0]);
            AssertDropped(DropReason.RateLimited, frozen[1]);
            AssertPlayed(frozen[2]);
        }

        private PlayResult[] RunScenarioWithTimeScale(float timeScale)
        {
            float previous = Time.timeScale;
            try
            {
                Time.timeScale = timeScale;
                SetUpAudioService();
                InitializeWithDefaultAssets();
                return RunRateLimitScenario(ReadPegHitMinInterval());
            }
            finally
            {
                Time.timeScale = previous;
            }
        }

        private PlayResult[] RunRateLimitScenario(double minInterval)
        {
            return new[]
            {
                PlayAt(PegHitScoredId, T0),
                PlayAt(PegHitScoredId, T0 + minInterval - Epsilon),
                PlayAt(PegHitScoredId, T0 + minInterval)
            };
        }

        private double ReadPegHitMinInterval()
        {
            SoundCue cue = FindDefaultCue(PegHitScoredId);
            Assert.IsNotNull(cue, PegHitScoredId);
            return cue.MinInterval;
        }

        private static SoundCue FindDefaultCue(string cueId)
        {
            foreach (SoundCue cue in DefaultAudioAssets.LoadCues())
            {
                if (cue != null && cue.CueId == cueId)
                {
                    return cue;
                }
            }

            return null;
        }

        private static void AssertSameResults(PlayResult[] expected, PlayResult[] actual)
        {
            Assert.AreEqual(expected.Length, actual.Length);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i].Played, actual[i].Played, "index " + i);
                Assert.AreEqual(expected[i].Reason, actual[i].Reason, "index " + i);
            }
        }
    }
}
