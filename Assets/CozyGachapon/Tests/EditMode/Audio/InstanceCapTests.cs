using CozyGachapon.Contracts;
using NUnit.Framework;

namespace CozyGachapon.Tests.Audio
{
    public class InstanceCapTests : AudioServiceFixture
    {
        private const string PegHitScoredId = StealScenarioBuilder.PegHitScoredId;
        private const string CoinSpawnAutoId = "Plinko.CoinSpawnAuto";
        private const double FirstStart = 1.00;
        private const double Step = 0.05;
        private const int PegHitMaxInstances = 6;

        [SetUp]
        public void SetUpDefaultAssets()
        {
            InitializeWithDefaultAssets();
        }

        [Test]
        public void AC9_PegHitScoredAtCap_StealsOldestOwnInstance()
        {
            int oldestVoice = FillPegHits();

            AssertPlayed(PlayAt(PegHitScoredId, FirstStart + PegHitMaxInstances * Step));

            Assert.AreEqual(oldestVoice, Backend.LastStartedVoice);
            Assert.AreEqual(1, Backend.GetStopCount(oldestVoice));
            Assert.AreEqual(1, Backend.StopCount);
            Assert.AreEqual(PegHitMaxInstances, Backend.PlayingCount);
            Assert.AreEqual(PegHitMaxInstances, Service.Stats.ActiveVoices);
            Assert.AreEqual(1, Service.Stats.SameCueSteals);
        }

        [Test]
        public void AC9_StealOldest_PicksEarliestStartNotLowestVoiceIndex()
        {
            FillPegHits();
            int secondVoice = Backend.LastStartedVoice - (PegHitMaxInstances - 2);
            Backend.FinishVoice(Backend.LastStartedVoice - (PegHitMaxInstances - 1));
            Service.Tick();
            AssertPlayed(PlayAt(PegHitScoredId, 1.30));
            Assert.AreEqual(0, Backend.StopCount);

            AssertPlayed(PlayAt(PegHitScoredId, 1.35));

            Assert.AreEqual(secondVoice, Backend.LastStartedVoice);
            Assert.AreEqual(1, Backend.GetStopCount(secondVoice));
            Assert.AreEqual(PegHitMaxInstances, Backend.PlayingCount);
            Assert.AreEqual(1, Service.Stats.SameCueSteals);
        }

        [Test]
        public void AC9_StealOldest_RepeatedSteals_KeepInstanceCountAtCap()
        {
            FillPegHits();
            for (int i = 0; i < PegHitMaxInstances * 3; i++)
            {
                AssertPlayed(PlayAt(PegHitScoredId, 2.00 + i * Step));
            }

            Assert.AreEqual(PegHitMaxInstances, Backend.PlayingCount);
            Assert.AreEqual(PegHitMaxInstances * 3, Service.Stats.SameCueSteals);
            Assert.AreEqual(PegHitMaxInstances * 3, Backend.StopCount);
        }

        [Test]
        public void AC9_CoinSpawnAutoAtCap_DropsWithInstanceCap_OldKeepsPlaying()
        {
            int oldVoice = PlayVoiceAt(CoinSpawnAutoId, FirstStart);

            AssertDropped(DropReason.InstanceCap, PlayAt(CoinSpawnAutoId, FirstStart + 0.20));

            Assert.AreEqual(0, Backend.StopCount);
            Assert.IsTrue(Backend.IsPlaying(oldVoice));
            Assert.AreEqual(1, Backend.PlayingCount);
            Assert.AreEqual(0, Service.Stats.SameCueSteals);
            Assert.AreEqual(1, Service.Stats.DroppedInstanceCap);
        }

        [Test]
        public void RateLimitedRequestAtCap_StealsNothing()
        {
            FillPegHits();

            AssertDropped(DropReason.RateLimited, PlayAt(PegHitScoredId, FirstStart + (PegHitMaxInstances - 1) * Step + 0.02));

            Assert.AreEqual(0, Backend.StopCount);
            Assert.AreEqual(0, Service.Stats.SameCueSteals);
            Assert.AreEqual(PegHitMaxInstances, Backend.PlayingCount);
        }

        private int FillPegHits()
        {
            int firstVoice = -1;
            for (int i = 0; i < PegHitMaxInstances; i++)
            {
                int voice = PlayVoiceAt(PegHitScoredId, FirstStart + i * Step);
                firstVoice = i == 0 ? voice : firstVoice;
            }

            return firstVoice;
        }
    }
}
