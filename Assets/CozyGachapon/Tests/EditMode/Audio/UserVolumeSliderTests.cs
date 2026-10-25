using CozyGachapon.Audio;
using CozyGachapon.Contracts;
using NUnit.Framework;

namespace CozyGachapon.Tests.Audio
{
    public class UserVolumeSliderTests : AudioGainFixture
    {
        private const string ShopLoopId = "Music.ShopLoop";
        private const string PegHitId = "Plinko.PegHitScored";
        private const string CrankId = "Gacha.Crank";
        private const float MusicDb = -12f;
        private const float HalfSliderDb = -20f;

        [Test]
        public void SfxSliderAtZero_DropsSfxCuesAsBusSilent()
        {
            InitializeWithDefaultAssets();
            Service.SetUserVolume(AudioBus.Sfx, 0f);

            string[] ids = { PegHitId, "Ui.Tap", CrankId, "Plinko.BucketX5" };
            for (int i = 0; i < ids.Length; i++)
            {
                Clock.Advance(1d);
                AssertDropped(DropReason.BusSilent, Service.Play(ids[i], AudioPlayParams.None));
            }
        }

        [Test]
        public void NegativeSlider_IsTreatedAsZero()
        {
            InitializeWithDefaultAssets();
            Service.SetUserVolume(AudioBus.Sfx, -0.5f);

            Clock.Advance(1d);
            AssertDropped(DropReason.BusSilent, Service.Play(PegHitId, AudioPlayParams.None));
        }

        [Test]
        public void SfxSliderAtZeroThenRaised_PlaysAgain()
        {
            InitializeWithDefaultAssets();
            Service.SetUserVolume(AudioBus.Sfx, 0f);
            Service.SetUserVolume(AudioBus.Sfx, 0.5f);

            int voice = PlayHeld(PegHitId);

            AssertVolume(voice, CueDb(PegHitId) + HalfSliderDb);
        }

        [TestCase(1f, 0f)]
        [TestCase(0.75f, -10f)]
        [TestCase(0.5f, -20f)]
        [TestCase(1.5f, 0f)]
        [TestCase(float.NaN, 0f)]
        public void SfxSlider_AppliesTheUserDbToSfxCues(float slider, float expectedUserDb)
        {
            InitializeWithDefaultAssets();
            Service.SetUserVolume(AudioBus.Sfx, slider);

            int voice = PlayHeld(CrankId);

            AssertVolume(voice, CueDb(CrankId) + expectedUserDb);
        }

        [Test]
        public void MusicSliderAtZero_KeepsThePlayingMusicMutedButRunning()
        {
            InitializeWithDefaultAssets(CreateMusicCue(ShopLoopId));
            int voice = PlayHeld(ShopLoopId);
            AssertVolume(voice, MusicDb);

            Service.SetUserVolume(AudioBus.Music, 0f);

            Assert.AreEqual(0f, Backend.GetVolume(voice));
            Assert.IsTrue(Backend.IsPlaying(voice));
            Assert.AreEqual(0, Backend.GetStopCount(voice));
            Clock.Advance(1d);
            AssertDropped(DropReason.BusSilent, Service.Play(ShopLoopId, AudioPlayParams.None));
            Assert.IsTrue(Backend.IsPlaying(voice));
        }

        [Test]
        public void MusicSliderRestored_BringsTheMusicBack()
        {
            InitializeWithDefaultAssets(CreateMusicCue(ShopLoopId));
            int voice = PlayHeld(ShopLoopId);
            Service.SetUserVolume(AudioBus.Music, 0f);

            Service.SetUserVolume(AudioBus.Music, 1f);

            AssertVolume(voice, MusicDb);
        }

        [Test]
        public void MusicSliderHalf_LowersMusicByTwentyDb()
        {
            InitializeWithDefaultAssets(CreateMusicCue(ShopLoopId));
            int voice = PlayHeld(ShopLoopId);

            Service.SetUserVolume(AudioBus.Music, 0.5f);

            Assert.AreEqual(0.025f, Backend.GetVolume(voice), 0.001f);
            AssertVolume(voice, MusicDb + HalfSliderDb);
        }

        [Test]
        public void MusicSlider_DoesNotAffectSfxCues()
        {
            InitializeWithDefaultAssets();
            Service.SetUserVolume(AudioBus.Music, 0f);

            int voice = PlayHeld(PegHitId);

            AssertVolume(voice, CueDb(PegHitId));
        }

        [Test]
        public void SfxSlider_LowersPlayingSfxVoicesImmediately()
        {
            InitializeWithDefaultAssets();
            int voice = PlayHeld(PegHitId);

            Service.SetUserVolume(AudioBus.Sfx, 0.5f);

            AssertVolume(voice, CueDb(PegHitId) + HalfSliderDb);
        }

        [TestCase(AudioBus.Plinko)]
        [TestCase(AudioBus.Master)]
        [TestCase(AudioBus.Ui)]
        public void SetUserVolume_ForABusOtherThanMusicOrSfx_IsIgnored(AudioBus bus)
        {
            InitializeWithDefaultAssets();
            Service.SetUserVolume(bus, 0f);

            int voice = PlayHeld(PegHitId);

            AssertVolume(voice, CueDb(PegHitId));
        }

        [Test]
        public void SetUserVolume_BeforeInitialize_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => Service.SetUserVolume(AudioBus.Sfx, 0f));
            InitializeWithDefaultAssets();

            int voice = PlayHeld(PegHitId);

            AssertVolume(voice, CueDb(PegHitId));
        }
    }
}
