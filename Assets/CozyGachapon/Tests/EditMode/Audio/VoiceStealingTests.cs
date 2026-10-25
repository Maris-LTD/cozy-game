using CozyGachapon.Audio;
using CozyGachapon.Contracts;
using NUnit.Framework;

namespace CozyGachapon.Tests.Audio
{
    public class VoiceStealingTests : AudioServiceFixture
    {
        private const string PegHitUnscoredId = StealScenarioBuilder.PegHitUnscoredId;
        private const string JackpotHitId = StealScenarioBuilder.JackpotHitId;
        private const string BucketX2Id = StealScenarioBuilder.BucketX2Id;
        private const string LowId = "Test.Low";
        private const string MidId = "Test.Mid";
        private const string HighId = "Test.High";
        private const string MusicId = "Test.OnlyMusic";
        private const int LowPriority = 10;
        private const int MidPriority = 20;
        private const int HighPriority = 90;

        private StealScenarioBuilder _builder;

        [Test]
        public void AC12_StateA_RateLimitedThenPriorityStealThenNoVoice()
        {
            BuildState(true);
            int victimVoice = _builder.OldestPegHitUnscoredVoice;

            AssertDropped(DropReason.RateLimited, PlayAt(PegHitUnscoredId, 2.25));
            Assert.AreEqual(0, Backend.StopCount);

            AssertPlayed(PlayAt(JackpotHitId, 2.30));
            Assert.AreEqual(victimVoice, Backend.LastStartedVoice);
            Assert.AreEqual(1, Backend.GetStopCount(victimVoice));
            Assert.AreEqual(1, Backend.StopCount);
            Assert.AreEqual(0, Service.Stats.SameCueSteals);
            Assert.AreEqual(1, Service.Stats.PrioritySteals);

            AssertDropped(DropReason.NoVoice, PlayAt(PegHitUnscoredId, 2.35));
            Assert.AreEqual(1, Backend.StopCount);
            Assert.AreEqual(16, Backend.PlayingCount);
            AssertMusicUntouched();
        }

        [Test]
        public void AC12_StateB_BucketX2_NoVoice_NoPeerSteal()
        {
            BuildState(false);

            AssertDropped(DropReason.NoVoice, PlayAt(BucketX2Id, 2.50));

            Assert.AreEqual(0, Backend.StopCount);
            Assert.AreEqual(16, Backend.PlayingCount);
            Assert.AreEqual(0, Service.Stats.SameCueSteals);
            Assert.AreEqual(0, Service.Stats.PrioritySteals);
            AssertMusicUntouched();
        }

        [Test]
        public void AC12_StateA_BucketX2AtCap_SelfStealsOldest()
        {
            BuildState(true);
            int oldestVoice = _builder.OldestBucketX2Voice;

            AssertPlayed(PlayAt(BucketX2Id, 2.30));

            Assert.AreEqual(oldestVoice, Backend.LastStartedVoice);
            Assert.AreEqual(1, Backend.GetStopCount(oldestVoice));
            Assert.AreEqual(1, Backend.StopCount);
            Assert.AreEqual(1, Service.Stats.SameCueSteals);
            Assert.AreEqual(0, Service.Stats.PrioritySteals);
            Assert.AreEqual(16, Backend.PlayingCount);
            AssertMusicUntouched();
        }

        [Test]
        public void PriorityTie_PicksEarliestStart()
        {
            int sfxVoices = FillSfxWithLowCue(LowPriority);
            int earliestVoice = Backend.LastStartedVoice - (sfxVoices - 1);

            AssertPlayed(PlayAt(HighId, 5.0));

            Assert.AreEqual(earliestVoice, Backend.LastStartedVoice);
            Assert.AreEqual(1, Backend.StopCount);
            Assert.AreEqual(1, Backend.GetStopCount(earliestVoice));
        }

        [Test]
        public void LowestPriorityBeatsOlderStart()
        {
            InitializeWithCues(CreateStealCue(LowId, LowPriority, 1), CreateStealCue(MidId, MidPriority, 14), CreateStealCue(HighId, HighPriority, 1));
            int midVoice = PlayVoiceAt(MidId, 1.0);
            for (int i = 1; i < 14; i++)
            {
                PlayVoiceAt(MidId, 1.0 + i * 0.1);
            }

            int lowVoice = PlayVoiceAt(LowId, 3.0);

            AssertPlayed(PlayAt(HighId, 5.0));

            Assert.AreEqual(lowVoice, Backend.LastStartedVoice);
            Assert.AreEqual(1, Backend.GetStopCount(lowVoice));
            Assert.IsTrue(Backend.IsPlaying(midVoice));
            Assert.AreEqual(0, Backend.GetStopCount(midVoice));
        }

        [Test]
        public void EqualPriorityRequest_DoesNotSteal()
        {
            FillSfxWithLowCue(HighPriority);

            AssertDropped(DropReason.NoVoice, PlayAt(HighId, 5.0));

            Assert.AreEqual(0, Backend.StopCount);
        }

        [Test]
        public void MusicVoiceNeverVictim_EvenWithLowestPriority()
        {
            InitializeWithCues(
                CreateMusicCue(MusicId),
                CreateStealCue(LowId, MidPriority, SfxVoiceCount()),
                CreateStealCue(HighId, HighPriority, 1));
            int musicVoice = PlayVoiceAt(MusicId, 0.5);
            for (int i = 0; i < SfxVoiceCount(); i++)
            {
                PlayVoiceAt(LowId, 1.0 + i * 0.1);
            }

            AssertPlayed(PlayAt(HighId, 5.0));

            Assert.AreNotEqual(musicVoice, Backend.LastStartedVoice);
            Assert.IsTrue(Backend.IsPlaying(musicVoice));
            Assert.AreEqual(0, Backend.GetStopCount(musicVoice));
            Assert.AreEqual(1, Backend.StopCount);
        }

        [Test]
        public void PriorityStealAcrossRepeatedRequests_NeverExceedsSfxVoices()
        {
            FillSfxWithLowCue(LowPriority);
            int sfxVoices = SfxVoiceCount();

            AssertPlayed(PlayAt(HighId, 5.0));
            AssertDropped(DropReason.InstanceCap, PlayAt(HighId, 5.2));

            Assert.AreEqual(sfxVoices, Backend.PlayingCount);
            Assert.AreEqual(1, Backend.StopCount);
        }

        private void BuildState(bool stateA)
        {
            InitializeWithDefaultAssets(CreateMusicCue(StealScenarioBuilder.MusicCueId));
            _builder = new StealScenarioBuilder(Service, Clock, Backend);
            if (stateA)
            {
                _builder.BuildStateA();
                return;
            }

            _builder.BuildStateB();
        }

        private void AssertMusicUntouched()
        {
            Assert.IsTrue(Backend.IsPlaying(_builder.MusicVoice));
            Assert.AreEqual(0, Backend.GetStopCount(_builder.MusicVoice));
            Assert.AreEqual(1, Backend.GetStartCount(_builder.MusicVoice));
        }

        private int FillSfxWithLowCue(int lowPriority)
        {
            int sfxVoices = SfxVoiceCount();
            InitializeWithCues(CreateStealCue(LowId, lowPriority, sfxVoices), CreateStealCue(HighId, HighPriority, 1));
            for (int i = 0; i < sfxVoices; i++)
            {
                PlayVoiceAt(LowId, 1.0 + i * 0.1);
            }

            return sfxVoices;
        }

        private int SfxVoiceCount()
        {
            return Config.TotalVoices - Config.MusicVoices - Config.AmbienceVoices;
        }

        private SoundCue CreateStealCue(string cueId, int priority, int maxInstances)
        {
            SoundCue cue = CreateLimitedCue(cueId, AudioBus.Plinko, maxInstances, 0f, InstanceCapPolicy.DropNew);
            cue.SetLimitsForTest(priority, maxInstances, 0f);
            return cue;
        }
    }
}
