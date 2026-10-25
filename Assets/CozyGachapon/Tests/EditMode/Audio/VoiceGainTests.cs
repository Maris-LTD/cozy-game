using System.Collections.Generic;
using CozyGachapon.Audio;
using CozyGachapon.Contracts;
using NUnit.Framework;

namespace CozyGachapon.Tests.Audio
{
    public class VoiceGainTests : AudioGainFixture
    {
        private const string BucketX5Id = "Plinko.BucketX5";
        private const string ShopLoopId = "Music.ShopLoop";
        private const string HotId = "Test.Hot";
        private const string QuietId = "Test.Quiet";
        private const float HotDb = 6f;
        private const float QuietDb = -8f;
        private const float SilenceFloorDb = -6f;

        [Test]
        public void BucketX5_WhenPlinkoIsNormal_UsesTheCueVolume()
        {
            InitializeWithDefaultAssets();

            int voice = PlayHeld(BucketX5Id);

            Assert.AreEqual(0.398f, Backend.GetVolume(voice), 0.001f);
            AssertVolume(voice, CueDb(BucketX5Id));
        }

        [Test]
        public void BucketX5_AfterBackgroundTransition_IsTwelveDbLower()
        {
            InitializeWithDefaultAssets();
            Service.SetBusBackground(AudioBus.Plinko, true);
            SettleBackground();

            int voice = PlayHeld(BucketX5Id);

            Assert.AreEqual(0.100f, Backend.GetVolume(voice), 0.001f);
            AssertVolume(voice, CueDb(BucketX5Id) + DefaultAudioAssets.LoadConfig().PlinkoBackgroundOffsetDb);
        }

        [Test]
        public void MusicCue_UsesTheMusicBusOffset()
        {
            InitializeWithDefaultAssets(CreateMusicCue(ShopLoopId));

            int voice = PlayHeld(ShopLoopId);

            Assert.AreEqual(0.251f, Backend.GetVolume(voice), 0.001f);
            AssertVolume(voice, DefaultAudioAssets.LoadConfig().MusicOffsetFromSfxDb);
        }

        [Test]
        public void CueAboveZeroDb_ClampsToFullVolume()
        {
            SoundCue hot = CreateLimitedCue(HotId, AudioBus.Plinko, 1, 0f, InstanceCapPolicy.DropNew);
            hot.SetVolumeDbForTest(HotDb);
            InitializeWithCues(hot);

            int voice = PlayHeld(HotId);

            Assert.AreEqual(1f, Backend.GetVolume(voice));
        }

        [Test]
        public void CueAboveZeroDb_ProducesAValidatorWarning()
        {
            SoundCue hot = CreateLimitedCue(HotId, AudioBus.Plinko, 1, 0f, InstanceCapPolicy.DropNew);
            hot.SetVolumeDbForTest(HotDb);
            List<AudioConfigIssue> issues = new List<AudioConfigIssue>();
            AudioBalanceTargets targets = Track(UnityEngine.Object.Instantiate(DefaultAudioAssets.LoadTargets()));

            AudioCueDataValidator.Validate(Config, targets, new List<SoundCue> { hot }, issues);

            Assert.AreEqual(1, issues.Count);
            Assert.AreEqual(AudioConfigIssueCode.VolumeAboveZero, issues[0].Code);
            Assert.IsFalse(issues[0].IsError);
        }

        [Test]
        public void CueBelowSilenceFloor_PlaysAtZeroVolume()
        {
            Config.SetSilenceFloorDbForTest(SilenceFloorDb);
            SoundCue quiet = CreateLimitedCue(QuietId, AudioBus.Plinko, 1, 0f, InstanceCapPolicy.DropNew);
            quiet.SetVolumeDbForTest(QuietDb);
            InitializeWithCues(quiet);

            int voice = PlayHeld(QuietId);

            Assert.AreEqual(0f, Backend.GetVolume(voice));
        }
    }
}
