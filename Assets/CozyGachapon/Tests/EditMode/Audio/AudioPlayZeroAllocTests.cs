using CozyGachapon.Audio;
using CozyGachapon.Contracts;
using CozyGachapon.Tests.Pooling;
using NUnit.Framework;

namespace CozyGachapon.Tests.Audio
{
    public class AudioPlayZeroAllocTests : AudioServiceFixture
    {
        private const string PegHitScoredId = "Plinko.PegHitScored";
        private const string MissingId = "Khong.Ton.Tai";
        private const string LowId = "Test.Low";
        private const string OtherLowId = "Test.OtherLow";
        private const string HighId = "Test.High";
        private const int Iterations = 200;
        private const double Step = 1d;

        [Test]
        public void Play_PlayedRateLimitedAndUnknown_DoesNotAllocate()
        {
            InitializeWithDefaultAssets();
            Assert.IsTrue(Service.TryResolveCue(PegHitScoredId, out int pegIndex));
            AudioPlayParams playParams = AudioPlayParams.None;
            FakeVoiceBackend backend = Backend;
            FakeAudioClock clock = Clock;

            RunRequests(pegIndex, playParams, backend, clock);
            int allocations = GcAllocProbe.CountAllocations(() => RunRequests(pegIndex, playParams, backend, clock));

            Assert.AreEqual(0, allocations);
            Assert.Greater(Service.Stats.Played, 0);
            Assert.Greater(Service.Stats.DroppedRateLimited, 0);
            Assert.Greater(Service.Stats.DroppedUnknownCue, 0);
        }

        [Test]
        public void Play_SameCueStealPriorityStealAndNoVoice_DoesNotAllocate()
        {
            int sfxVoices = Config.TotalVoices - Config.MusicVoices - Config.AmbienceVoices;
            InitializeWithCues(
                CreateStealCue(LowId, 10, sfxVoices, InstanceCapPolicy.StealOldest),
                CreateStealCue(OtherLowId, 10, 1, InstanceCapPolicy.DropNew),
                CreateStealCue(HighId, 90, 1, InstanceCapPolicy.DropNew));
            Assert.IsTrue(Service.TryResolveCue(LowId, out int lowIndex));
            Assert.IsTrue(Service.TryResolveCue(OtherLowId, out int otherLowIndex));
            Assert.IsTrue(Service.TryResolveCue(HighId, out int highIndex));
            AudioPlayParams playParams = AudioPlayParams.None;
            for (int i = 0; i < sfxVoices; i++)
            {
                Assert.IsTrue(Service.Play(lowIndex, playParams).Played);
            }

            RunStealRequests(lowIndex, otherLowIndex, highIndex, playParams);
            int allocations = GcAllocProbe.CountAllocations(() => RunStealRequests(lowIndex, otherLowIndex, highIndex, playParams));

            Assert.AreEqual(0, allocations);
            Assert.Greater(Service.Stats.SameCueSteals, 0);
            Assert.Greater(Service.Stats.PrioritySteals, 0);
            Assert.Greater(Service.Stats.PeakVoices, 0);
            Assert.Greater(Service.Stats.MinSameCueStealAgeSeconds, 0d);
            Assert.Greater(Service.Stats.DroppedNoVoice, 0);
            Assert.Greater(Backend.StopCount, 0);
        }

        [Test]
        public void Play_BeforeInitialize_DoesNotAllocate()
        {
            AudioPlayParams playParams = AudioPlayParams.None;
            Service.Play(PegHitScoredId, playParams);
            Service.Play(0, playParams);
            AudioService service = Service;

            int allocations = GcAllocProbe.CountAllocations(() =>
            {
                for (int i = 0; i < Iterations; i++)
                {
                    service.Play(PegHitScoredId, playParams);
                    service.Play(i, playParams);
                }
            });

            Assert.AreEqual(0, allocations);
            Assert.AreEqual(Iterations * 4 + 2, Service.Stats.DroppedNotReady);
        }

        private SoundCue CreateStealCue(string cueId, int priority, int maxInstances, InstanceCapPolicy policy)
        {
            SoundCue cue = CreateLimitedCue(cueId, AudioBus.Plinko, maxInstances, 0f, policy);
            cue.SetLimitsForTest(priority, maxInstances, 0f);
            return cue;
        }

        private void RunStealRequests(int lowIndex, int otherLowIndex, int highIndex, AudioPlayParams playParams)
        {
            for (int i = 0; i < Iterations; i++)
            {
                Clock.Advance(Step);
                Service.Play(highIndex, playParams);
                Backend.FinishVoice(Backend.LastStartedVoice);
                Service.Tick();
                Service.Play(lowIndex, playParams);
                Service.Play(lowIndex, playParams);
                Service.Play(otherLowIndex, playParams);
            }
        }

        private void RunRequests(int pegIndex, AudioPlayParams playParams, FakeVoiceBackend backend, FakeAudioClock clock)
        {
            for (int i = 0; i < Iterations; i++)
            {
                clock.Advance(Step);
                Service.Play(PegHitScoredId, playParams);
                Service.Play(pegIndex, playParams);
                Service.Play(MissingId, playParams);
                Service.Play(-1, playParams);
                backend.FinishVoice(backend.LastStartedVoice);
                Service.Tick();
                Service.Stats.GetDropped(DropReason.RateLimited);
            }
        }
    }
}
