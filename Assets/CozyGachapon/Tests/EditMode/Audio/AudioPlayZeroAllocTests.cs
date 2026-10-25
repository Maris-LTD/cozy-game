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
