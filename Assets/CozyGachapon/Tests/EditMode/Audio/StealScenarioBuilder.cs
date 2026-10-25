using CozyGachapon.Audio;
using CozyGachapon.Contracts;
using NUnit.Framework;

namespace CozyGachapon.Tests.Audio
{
    public sealed class StealScenarioBuilder
    {
        public const string MusicCueId = "Test.Music";
        public const string PegHitScoredId = "Plinko.PegHitScored";
        public const string PegHitUnscoredId = "Plinko.PegHitUnscored";
        public const string CoinSpawnId = "Plinko.CoinSpawn";
        public const string BucketX1Id = "Plinko.BucketX1";
        public const string BucketX2Id = "Plinko.BucketX2";
        public const string BucketX5Id = "Plinko.BucketX5";
        public const string JackpotBurstSpawnId = "Plinko.JackpotBurstSpawn";
        public const string JackpotHitId = "Plinko.JackpotHit";
        public const string CrankId = "Gacha.Crank";
        public const string EggPopId = "Gacha.EggPop";
        public const string RevealCommonId = "Gacha.RarityReveal.common";
        public const string RevealRareId = "Gacha.RarityReveal.rare";

        private const double MusicStart = 0.5d;
        private const double PegHitStart = 1.0d;
        private const double PegHitStep = 0.05d;
        private const int PegHitScoredCount = 6;
        private const int BusyVoiceCount = 15;

        private readonly AudioService _service;
        private readonly FakeAudioClock _clock;
        private readonly FakeVoiceBackend _backend;

        public int MusicVoice { get; private set; }
        public int OldestPegHitUnscoredVoice { get; private set; }
        public int OldestBucketX2Voice { get; private set; }

        public StealScenarioBuilder(AudioService service, FakeAudioClock clock, FakeVoiceBackend backend)
        {
            _service = service;
            _clock = clock;
            _backend = backend;
        }

        public void BuildStateA()
        {
            MusicVoice = PlayVoice(MusicCueId, MusicStart);
            for (int i = 0; i < PegHitScoredCount; i++)
            {
                PlayVoice(PegHitScoredId, PegHitStart + i * PegHitStep);
            }

            PlayVoice(CoinSpawnId, 1.30);
            OldestBucketX2Voice = PlayVoice(BucketX2Id, 1.40);
            PlayVoice(BucketX2Id, 1.45);
            PlayVoice(BucketX2Id, 1.50);
            PlayVoice(BucketX5Id, 1.60);
            PlayVoice(BucketX5Id, 1.65);
            PlayVoice(BucketX5Id, 1.70);
            OldestPegHitUnscoredVoice = PlayVoice(PegHitUnscoredId, 2.10);
            PlayVoice(PegHitUnscoredId, 2.20);
            AssertBuilt();
        }

        public void BuildStateB()
        {
            int earlyBucketVoice = PlayVoice(BucketX2Id, 0.30);
            _backend.FinishVoice(earlyBucketVoice);
            _service.Tick();
            MusicVoice = PlayVoice(MusicCueId, MusicStart);
            PlayVoice(BucketX5Id, 1.00);
            PlayVoice(BucketX5Id, 1.05);
            PlayVoice(BucketX5Id, 1.10);
            PlayVoice(BucketX1Id, 1.20);
            PlayVoice(BucketX1Id, 1.25);
            PlayVoice(BucketX1Id, 1.30);
            PlayVoice(JackpotBurstSpawnId, 1.40);
            PlayVoice(JackpotBurstSpawnId, 1.50);
            PlayVoice(JackpotBurstSpawnId, 1.60);
            PlayVoice(JackpotHitId, 1.70);
            PlayVoice(JackpotHitId, 1.85);
            PlayVoice(CrankId, 1.90);
            PlayVoice(EggPopId, 1.95);
            PlayVoice(RevealCommonId, 2.00);
            PlayVoice(RevealRareId, 2.05);
            AssertBuilt();
        }

        private int PlayVoice(string cueId, double now)
        {
            _clock.Set(now);
            PlayResult result = _service.Play(cueId, AudioPlayParams.None);
            Assert.IsTrue(result.Played, cueId + " " + result.Reason);
            return _backend.LastStartedVoice;
        }

        private void AssertBuilt()
        {
            Assert.AreEqual(BusyVoiceCount + 1, _backend.PlayingCount);
            Assert.IsTrue(_backend.IsPlaying(MusicVoice));
            Assert.AreEqual(0, _backend.StopCount);
        }
    }
}
