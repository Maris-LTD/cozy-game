using CozyGachapon.Contracts;
using CozyGachapon.Tests.Pooling;
using NUnit.Framework;

namespace CozyGachapon.Tests.Audio
{
    public class MuteZeroAllocTests : AudioMuteFixture
    {
        private const int Iterations = 200;
        private const int Cycle = 8;
        private const double Step = 0.04d;

        [Test]
        public void SuspendResumeMuteAndTick_DoNotAllocate()
        {
            InitializeWithMusic();
            PlayHeld(ShopLoopId);
            PlayHeld(PegHitId);

            RunMuteCycles();
            int changes = MuteChangedCount;
            int volumeWrites = Backend.SetVolumeCount;
            int allocations = GcAllocProbe.CountAllocations(RunMuteCycles);

            Assert.AreEqual(0, allocations);
            Assert.Greater(MuteChangedCount, changes);
            Assert.Greater(Backend.SetVolumeCount, volumeWrites);
            Assert.Greater(Backend.PauseCount, 0);
            Assert.Greater(Backend.ResumeCount, 0);
        }

        private void RunMuteCycles()
        {
            for (int i = 0; i < Iterations; i++)
            {
                Clock.Advance(Step);
                int phase = i % Cycle;
                if (phase == 0)
                {
                    Service.HandlePaused(new PausedEvent(PauseReason.Hidden));
                }
                else if (phase == 1)
                {
                    Service.HandleResumed(new ResumedEvent(PauseReason.Hidden, Step));
                }
                else if (phase == 3)
                {
                    Service.SetUserMuted(true);
                }
                else if (phase == 5)
                {
                    Service.SetUserMuted(false);
                }

                Service.Tick();
            }
        }
    }
}
