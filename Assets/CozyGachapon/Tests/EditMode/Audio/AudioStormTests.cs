using System;
using System.Collections.Generic;
using CozyGachapon.Audio;
using CozyGachapon.Contracts;
using CozyGachapon.Tests.Pooling;
using NUnit.Framework;

namespace CozyGachapon.Tests.Audio
{
    public class AudioStormTests : AudioServiceFixture
    {
        private const double StormStart = 100d;
        private const double HalfInterval = 0.5d;
        private const double AllocationRunGap = 1d;

        private AudioBalanceTargets _targets;
        private AudioStormShape _shape;
        private AudioStormDriver _driver;
        private double _nextRunStart;

        [Test]
        public void AC13_Storm_PegHitScoredPlayed_WithinThroughputBound()
        {
            RunStorm();
            SoundCue cue = FindCue(AudioStormDriver.PegHitScoredId);

            double bound = _targets.StormTestSeconds / cue.MinInterval + 1d;
            int played = _driver.GetPlayed(StormCue.PegHitScored);

            Assert.LessOrEqual(played, bound);
            Assert.Greater(played, 0);
            Assert.Greater(_driver.GetRequests(StormCue.PegHitScored), played);
            Assert.Greater(Service.Stats.DroppedRateLimited, 0);
        }

        [Test]
        public void AC13_Storm_PlayingVoicesNeverExceedTotal()
        {
            RunStorm();

            Assert.AreEqual(0, _driver.PlayingVoiceOverflowSteps);
            Assert.LessOrEqual(_driver.MaxPlayingVoices, DefaultAudioAssets.LoadConfig().TotalVoices);
            Assert.AreEqual((int)(_shape.Seconds * _shape.PegHitRate), _driver.Steps);
        }

        [Test]
        public void AC13_Storm_EveryJackpotPlayed()
        {
            RunStorm();

            int requests = _driver.GetRequests(StormCue.JackpotHit);
            Assert.Greater(requests, 0);
            Assert.AreEqual(requests, _driver.GetPlayed(StormCue.JackpotHit));
        }

        [Test]
        public void AC13_Storm_JackpotCount_FollowsConfiguredInterval()
        {
            RunStorm();

            double lastStepOffset = (_driver.Steps - 1) / _shape.PegHitRate;
            int expected = (int)Math.Floor(lastStepOffset / _shape.JackpotIntervalSeconds);
            Assert.AreEqual(expected, _driver.GetRequests(StormCue.JackpotHit));
        }

        [Test]
        public void AC13_Storm_UnscoredShare_FollowsConfiguredRatio()
        {
            RunStorm();

            int pegRequests = _driver.GetRequests(StormCue.PegHitScored) + _driver.GetRequests(StormCue.PegHitUnscored);
            double share = (double)_driver.GetRequests(StormCue.PegHitUnscored) / pegRequests;

            Assert.AreEqual(_shape.UnscoredRatio, share, _targets.BoundaryTolerance + 1d / pegRequests);
        }

        [Test]
        public void AC13_Storm_SameCuePegHitSteals_VictimAgeAtLeastMinStealAge()
        {
            RunStorm();
            double minAge = _targets.MinStealAgeSeconds;

            Assert.Greater(_driver.GetSameCueSteals(StormCue.PegHitScored), 0);
            Assert.GreaterOrEqual(_driver.GetMinSameCueStealAge(StormCue.PegHitScored), minAge);
            Assert.GreaterOrEqual(Service.Stats.MinSameCueStealAgeSeconds, minAge);
            Assert.AreEqual(SumSameCueSteals(), Service.Stats.SameCueSteals);
        }

        [Test]
        public void AC30_Storm_RequestsEqualPlayedPlusDroppedByReason()
        {
            RunStorm();
            AudioStats stats = Service.Stats;

            Assert.AreEqual(SumRequests(), stats.Requests);
            Assert.AreEqual(SumPlayed(), stats.Played);
            Assert.AreEqual(stats.Requests, stats.Played + SumDroppedByReason(stats));
            Assert.AreEqual(stats.Requests, stats.Played + stats.DroppedTotal);
            Assert.AreEqual(SumDropped(), stats.DroppedTotal);
        }

        [Test]
        public void AC30_Storm_PeakVoicesWithinTotal()
        {
            RunStorm();
            AudioStats stats = Service.Stats;

            Assert.LessOrEqual(stats.PeakVoices, DefaultAudioAssets.LoadConfig().TotalVoices);
            Assert.AreEqual(_driver.MaxPlayingVoices, stats.PeakVoices);
            Assert.GreaterOrEqual(stats.PeakVoices, stats.ActiveVoices);
        }

        [Test]
        public void AC30_Storm_StatsReadTwice_AreIdentical()
        {
            RunStorm();
            AudioStats first = Service.Stats;
            AudioStats second = Service.Stats;

            Assert.AreEqual(first.Requests, second.Requests);
            Assert.AreEqual(first.Played, second.Played);
            Assert.AreEqual(first.DroppedTotal, second.DroppedTotal);
            Assert.AreEqual(first.SameCueSteals, second.SameCueSteals);
            Assert.AreEqual(first.PrioritySteals, second.PrioritySteals);
            Assert.AreEqual(first.PeakVoices, second.PeakVoices);
            Assert.AreEqual(first.MinSameCueStealAgeSeconds, second.MinSameCueStealAgeSeconds);
        }

        [Test]
        public void Edge_TwoJackpotsWithinMinInterval_OnlyOnePlays()
        {
            InitializeWithDefaultAssets();
            SoundCue cue = FindCue(AudioStormDriver.JackpotHitId);

            AssertPlayed(PlayAt(AudioStormDriver.JackpotHitId, StormStart));
            AssertDropped(
                DropReason.RateLimited,
                PlayAt(AudioStormDriver.JackpotHitId, StormStart + cue.MinInterval * HalfInterval));
        }

        [Test]
        public void Storm_Run_DoesNotAllocate()
        {
            Prepare();
            _nextRunStart = StormStart;

            int allocations = GcAllocProbe.CountAllocations(RunNextWindow);

            Assert.AreEqual(0, allocations);
            Assert.Greater(Service.Stats.Played, 0);
        }

        private void Prepare()
        {
            InitializeWithDefaultAssets();
            _targets = DefaultAudioAssets.LoadTargets();
            _shape = AudioStormShape.FromTargets(_targets, DefaultAudioAssets.TapCooldownSeconds);
            _driver = new AudioStormDriver(Service, Clock, Backend, _shape);
        }

        private void RunStorm()
        {
            Prepare();
            _driver.Run(StormStart);
        }

        private void RunNextWindow()
        {
            _driver.Run(_nextRunStart, _shape.Seconds);
            _nextRunStart += _shape.Seconds + AllocationRunGap;
        }

        private static SoundCue FindCue(string cueId)
        {
            List<SoundCue> cues = DefaultAudioAssets.LoadCues();
            for (int i = 0; i < cues.Count; i++)
            {
                if (cues[i].CueId == cueId)
                {
                    return cues[i];
                }
            }

            Assert.Fail(cueId);
            return null;
        }

        private int SumRequests()
        {
            int sum = 0;
            foreach (StormCue cue in Enum.GetValues(typeof(StormCue)))
            {
                sum += _driver.GetRequests(cue);
            }

            return sum;
        }

        private int SumPlayed()
        {
            int sum = 0;
            foreach (StormCue cue in Enum.GetValues(typeof(StormCue)))
            {
                sum += _driver.GetPlayed(cue);
            }

            return sum;
        }

        private int SumDropped()
        {
            int sum = 0;
            foreach (StormCue cue in Enum.GetValues(typeof(StormCue)))
            {
                sum += _driver.GetDropped(cue);
            }

            return sum;
        }

        private int SumSameCueSteals()
        {
            int sum = 0;
            foreach (StormCue cue in Enum.GetValues(typeof(StormCue)))
            {
                sum += _driver.GetSameCueSteals(cue);
            }

            return sum;
        }

        private static int SumDroppedByReason(AudioStats stats)
        {
            int sum = 0;
            foreach (DropReason reason in Enum.GetValues(typeof(DropReason)))
            {
                if (reason != DropReason.None)
                {
                    sum += stats.GetDropped(reason);
                }
            }

            return sum;
        }
    }
}
