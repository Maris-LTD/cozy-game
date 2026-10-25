using CozyGachapon.Contracts;
using CozyGachapon.Tests.Pooling;
using NUnit.Framework;

namespace CozyGachapon.Tests.Audio
{
    public class AdMuteZeroAllocTests : AudioMuteFixture
    {
        private const int Iterations = 200;
        private const int Cycle = 6;
        private const double Step = 0.04d;
        private const double PlayGap = 1d;

        [Test]
        public void AdCycleWithDroppedPlayAndTick_DoesNotAllocate()
        {
            InitializeWithMusic();
            PlayHeld(ShopLoopId);

            RunAdCycles();
            int stops = Backend.StopCount;
            int pauses = Backend.PauseCount;
            int resumes = Backend.ResumeCount;
            int allocations = GcAllocProbe.CountAllocations(RunAdCycles);

            Assert.AreEqual(0, allocations);
            Assert.Greater(Backend.StopCount, stops);
            Assert.Greater(Backend.PauseCount, pauses);
            Assert.Greater(Backend.ResumeCount, resumes);
        }

        private void RunAdCycles()
        {
            for (int i = 0; i < Iterations; i++)
            {
                Clock.Advance(Step);
                int phase = i % Cycle;
                if (phase == 0)
                {
                    Clock.Advance(PlayGap);
                    Service.Play(PegHitId, AudioPlayParams.None);
                    Service.SetMuteReason(MuteReason.Ad, true);
                }
                else if (phase == 2)
                {
                    Service.Play(PegHitId, AudioPlayParams.None);
                }
                else if (phase == 4)
                {
                    Service.SetMuteReason(MuteReason.Ad, false);
                }

                Service.Tick();
            }
        }
    }
}
