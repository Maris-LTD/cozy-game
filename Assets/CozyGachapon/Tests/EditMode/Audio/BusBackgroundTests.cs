using CozyGachapon.Audio;
using CozyGachapon.Contracts;
using NUnit.Framework;

namespace CozyGachapon.Tests.Audio
{
    public class BusBackgroundTests : AudioGainFixture
    {
        private const string PegHitId = "Plinko.PegHitScored";
        private const string CrankId = "Gacha.Crank";
        private static readonly string[] FilteredIds =
        {
            "Plinko.PegHitScored",
            "Plinko.PegHitUnscored",
            "Plinko.CoinSpawn",
            "Plinko.CoinSpawnAuto",
            "Plinko.JackpotBurstSpawn",
            "Plinko.CoinFizzle",
            "Plinko.BoardBusy"
        };
        private static readonly string[] KeptIds =
        {
            "Plinko.BucketX1",
            "Plinko.BucketX2",
            "Plinko.BucketX5",
            "Plinko.JackpotHit"
        };
        private static readonly string[] UnaffectedIds = { CrankId, "Ui.Tap" };

        private static float BackgroundDb => DefaultAudioAssets.LoadConfig().PlinkoBackgroundOffsetDb;

        [Test]
        public void BackgroundPlinko_DropsEveryCueThatDoesNotKeepPlaying()
        {
            InitializeWithDefaultAssets();
            Service.SetBusBackground(AudioBus.Plinko, true);
            SettleBackground();

            for (int i = 0; i < FilteredIds.Length; i++)
            {
                Clock.Advance(1d);
                AssertDropped(DropReason.BackgroundFiltered, Service.Play(FilteredIds[i], AudioPlayParams.None));
            }
        }

        [Test]
        public void BackgroundPlinko_PlaysKeptCuesTwelveDbLower()
        {
            InitializeWithDefaultAssets();
            Service.SetBusBackground(AudioBus.Plinko, true);
            SettleBackground();

            for (int i = 0; i < KeptIds.Length; i++)
            {
                int voice = PlayHeld(KeptIds[i]);
                AssertVolume(voice, CueDb(KeptIds[i]) + BackgroundDb);
            }
        }

        [Test]
        public void BackgroundPlinko_LeavesGachaAndUiAtTheirNormalLevel()
        {
            InitializeWithDefaultAssets();
            Service.SetBusBackground(AudioBus.Plinko, true);
            SettleBackground();

            for (int i = 0; i < UnaffectedIds.Length; i++)
            {
                int voice = PlayHeld(UnaffectedIds[i]);
                AssertVolume(voice, CueDb(UnaffectedIds[i]));
            }
        }

        [TestCase(AudioBus.Gacha)]
        [TestCase(AudioBus.Ui)]
        [TestCase(AudioBus.Sfx)]
        [TestCase(AudioBus.Gameplay)]
        [TestCase(AudioBus.Music)]
        public void SetBusBackground_ForABusOtherThanPlinko_HasNoEffect(AudioBus bus)
        {
            InitializeWithDefaultAssets();
            Service.SetBusBackground(bus, true);
            SettleBackground();

            int peg = PlayHeld(PegHitId);
            int crank = PlayHeld(CrankId);

            AssertVolume(peg, CueDb(PegHitId));
            AssertVolume(crank, CueDb(CrankId));
            Assert.IsFalse(Service.RuntimeState.IsBusBackgroundOnPath(AudioBus.Plinko));
            Assert.IsFalse(Service.RuntimeState.IsBusBackgroundOnPath(bus));
        }

        [Test]
        public void PlayingVoice_FadesWithoutStoppingAndReturnsToTheOldLevel()
        {
            InitializeWithDefaultAssets();
            float baseDb = CueDb(PegHitId);
            double transition = TransitionSeconds;
            int voice = PlayHeld(PegHitId);

            Service.SetBusBackground(AudioBus.Plinko, true);
            AdvanceAndTick(transition / 4d);
            AssertVolume(voice, baseDb + BackgroundDb * 0.25f);
            AdvanceAndTick(transition * 0.75d + SettleMargin);
            AssertVolume(voice, baseDb + BackgroundDb);

            Service.SetBusBackground(AudioBus.Plinko, false);
            AdvanceAndTick(transition / 2d);
            AssertVolume(voice, baseDb + BackgroundDb * 0.5f);
            AdvanceAndTick(transition / 2d + SettleMargin);
            AssertVolume(voice, baseDb);

            Assert.IsTrue(Backend.IsPlaying(voice));
            Assert.AreEqual(0, Backend.GetStopCount(voice));
            Assert.AreEqual(1, Backend.GetStartCount(voice));
        }

        [Test]
        public void RepeatedSameState_DoesNotRestartTheFade()
        {
            InitializeWithDefaultAssets();
            float baseDb = CueDb(PegHitId);
            double transition = TransitionSeconds;
            int voice = PlayHeld(PegHitId);

            Service.SetBusBackground(AudioBus.Plinko, true);
            AdvanceAndTick(transition / 2d);
            Service.SetBusBackground(AudioBus.Plinko, true);
            AdvanceAndTick(transition / 4d);

            AssertVolume(voice, baseDb + BackgroundDb * 0.75f);
        }

        [Test]
        public void ReversingMidway_DoesNotJumpTheVolume()
        {
            InitializeWithDefaultAssets();
            float baseDb = CueDb(PegHitId);
            double transition = TransitionSeconds;
            int voice = PlayHeld(PegHitId);

            Service.SetBusBackground(AudioBus.Plinko, true);
            AdvanceAndTick(transition / 2d);
            AssertVolume(voice, baseDb + BackgroundDb * 0.5f);

            Service.SetBusBackground(AudioBus.Plinko, false);
            Service.Tick();
            AssertVolume(voice, baseDb + BackgroundDb * 0.5f);

            AdvanceAndTick(transition / 4d);
            AssertVolume(voice, baseDb + BackgroundDb * 0.375f);
        }

        [Test]
        public void Tick_WithoutATransition_DoesNotWriteVolumes()
        {
            InitializeWithDefaultAssets();
            PlayHeld(PegHitId);
            Service.SetBusBackground(AudioBus.Plinko, true);
            SettleBackground();
            Service.Tick();
            int before = Backend.SetVolumeCount;

            for (int i = 0; i < 5; i++)
            {
                AdvanceAndTick(1d);
            }

            Assert.AreEqual(before, Backend.SetVolumeCount);
        }

        [Test]
        public void TurningBackgroundOff_AcceptsFilteredCuesAgain()
        {
            InitializeWithDefaultAssets();
            Service.SetBusBackground(AudioBus.Plinko, true);
            SettleBackground();
            Clock.Advance(1d);
            AssertDropped(DropReason.BackgroundFiltered, Service.Play(PegHitId, AudioPlayParams.None));

            Service.SetBusBackground(AudioBus.Plinko, false);
            SettleBackground();

            int voice = PlayHeld(PegHitId);
            AssertVolume(voice, CueDb(PegHitId));
        }

        [Test]
        public void SetBusBackground_BeforeInitialize_IsIgnored()
        {
            Assert.DoesNotThrow(() => Service.SetBusBackground(AudioBus.Plinko, true));
            InitializeWithDefaultAssets();

            int voice = PlayHeld(PegHitId);

            AssertVolume(voice, CueDb(PegHitId));
        }
    }
}
