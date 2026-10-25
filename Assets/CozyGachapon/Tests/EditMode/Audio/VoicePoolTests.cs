using CozyGachapon.Audio;
using CozyGachapon.Contracts;
using NUnit.Framework;

namespace CozyGachapon.Tests.Audio
{
    public class VoicePoolTests : AudioServiceFixture
    {
        private const string CueId = "Test.Plinko";
        private const string MusicCueId = "Test.Music";
        private const float Interval = 0.1f;

        [Test]
        public void Pool_AllocatesTotalVoices_MusicThenAmbienceThenSfx()
        {
            VoicePool pool = new VoicePool(6, 1, 2);

            Assert.AreEqual(6, pool.Count);
            Assert.AreEqual(VoicePartition.Music, pool.GetSlot(0).Partition);
            Assert.AreEqual(VoicePartition.Ambience, pool.GetSlot(1).Partition);
            Assert.AreEqual(VoicePartition.Ambience, pool.GetSlot(2).Partition);
            for (int i = 3; i < pool.Count; i++)
            {
                Assert.AreEqual(VoicePartition.Sfx, pool.GetSlot(i).Partition);
                Assert.IsFalse(pool.GetSlot(i).IsPlaying);
            }

            Assert.AreEqual(0, pool.ActiveCount);
        }

        [Test]
        public void SfxRequests_NeverUseMusicVoices_AndStayWithinTotalVoices()
        {
            int sfxVoices = Config.TotalVoices - Config.MusicVoices - Config.AmbienceVoices;
            InitializeWithCues(CreateLimitedCue(CueId, AudioBus.Plinko, sfxVoices * 2, 0f, InstanceCapPolicy.DropNew));

            for (int i = 0; i < sfxVoices * 2; i++)
            {
                PlayAt(CueId, 1d);
            }

            for (int voice = 0; voice < Config.MusicVoices; voice++)
            {
                Assert.AreEqual(0, Backend.GetStartCount(voice));
            }

            for (int voice = Config.MusicVoices; voice < Config.TotalVoices; voice++)
            {
                Assert.AreEqual(1, Backend.GetStartCount(voice));
            }

            Assert.AreEqual(sfxVoices, Backend.StartCount);
            Assert.AreEqual(sfxVoices, Service.Stats.ActiveVoices);
            Assert.AreEqual(sfxVoices, Service.Stats.DroppedNoVoice);
        }

        [Test]
        public void MusicCue_UsesOnlyMusicVoices()
        {
            SoundCue music = CreateLimitedCue(MusicCueId, AudioBus.Music, 4, 0f, InstanceCapPolicy.DropNew);
            InitializeWithCues(music);

            for (int i = 0; i < Config.MusicVoices; i++)
            {
                AssertPlayed(PlayAt(MusicCueId, 1d));
                Assert.Less(Backend.LastStartedVoice, Config.MusicVoices);
            }

            AssertDropped(DropReason.NoVoice, PlayAt(MusicCueId, 1d));
        }

        [Test]
        public void Tick_ReclaimsFinishedVoice_AndReleasesInstance()
        {
            InitializeWithCues(CreateLimitedCue(CueId, AudioBus.Plinko, 1, Interval, InstanceCapPolicy.DropNew));
            AssertPlayed(PlayAt(CueId, 1d));
            int voice = Backend.LastStartedVoice;

            Service.Tick();
            AssertDropped(DropReason.InstanceCap, PlayAt(CueId, 2d));

            Backend.FinishVoice(voice);
            Service.Tick();

            Assert.AreEqual(0, Service.Stats.ActiveVoices);
            AssertPlayed(PlayAt(CueId, 3d));
            Assert.AreEqual(1, Service.Stats.ActiveVoices);
        }

        [Test]
        public void Tick_DoesNotReclaimLoopingVoice()
        {
            SoundCue loop = CreateLimitedCue(CueId, AudioBus.Plinko, 1, Interval, InstanceCapPolicy.DropNew);
            SetLoop(loop, true);
            InitializeWithCues(loop);
            AssertPlayed(PlayAt(CueId, 1d));
            Assert.IsTrue(Backend.GetLoop(Backend.LastStartedVoice));

            Backend.FinishVoice(Backend.LastStartedVoice);
            Service.Tick();

            Assert.AreEqual(1, Service.Stats.ActiveVoices);
            AssertDropped(DropReason.InstanceCap, PlayAt(CueId, 2d));
        }

        [Test]
        public void PlayedVoice_StartsWithCueClip_ClampedVolume_NeutralPitchAndCenterPan()
        {
            SoundCue loud = CreateLimitedCue(CueId, AudioBus.Plinko, 1, 0f, InstanceCapPolicy.DropNew);
            loud.SetVolumeDbForTest(6f);
            InitializeWithCues(loud);

            AssertPlayed(PlayAt(CueId, 1d));

            int voice = Backend.LastStartedVoice;
            Assert.AreSame(loud.Clips[0], Backend.GetClip(voice));
            Assert.AreEqual(1f, Backend.GetVolume(voice), 1e-6f);
            Assert.AreEqual(1f, Backend.GetPitch(voice), 1e-6f);
            Assert.AreEqual(0f, Backend.GetPan(voice), 1e-6f);
            Assert.IsFalse(Backend.GetLoop(voice));
        }

        [Test]
        public void PlayedVoice_QuietCue_UsesDbToLinearVolume()
        {
            SoundCue quiet = CreateLimitedCue(CueId, AudioBus.Plinko, 1, 0f, InstanceCapPolicy.DropNew);
            quiet.SetVolumeDbForTest(-6f);
            InitializeWithCues(quiet);

            AssertPlayed(PlayAt(CueId, 1d));

            float expected = AudioMath.DbToLinear(-6f, Config.SilenceFloorDb);
            Assert.AreEqual(expected, Backend.GetVolume(Backend.LastStartedVoice), 1e-6f);
        }
    }
}
