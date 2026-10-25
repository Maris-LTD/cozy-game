using CozyGachapon.Audio;
using NUnit.Framework;

namespace CozyGachapon.Tests.Audio
{
    public class VoicePartitionTests : AudioServiceFixture
    {
        private const string MusicCueId = "Test.Music";
        private const double StormStart = 1d;

        [Test]
        public void AC11_StormKeepsVoiceCount_MusicVoiceNeverGivenToSfx()
        {
            InitializeWithDefaultAssets();
            AudioConfig config = DefaultAudioAssets.LoadConfig();
            Assert.AreEqual(config.TotalVoices, Service.VoiceCount);
            Assert.AreEqual(config.TotalVoices, Backend.VoiceCount);

            RunStorm();

            Assert.AreEqual(config.TotalVoices, Service.VoiceCount);
            Assert.AreEqual(config.TotalVoices, Backend.VoiceCount);
            Assert.Less(Backend.MaxStartedVoice, config.TotalVoices);
            Assert.Greater(Backend.StartCount, 0);
            for (int voice = 0; voice < config.MusicVoices; voice++)
            {
                Assert.AreEqual(0, Backend.GetStartCount(voice), "voice " + voice);
            }
        }

        [Test]
        public void AC11_StormNeverExceedsTotalVoices()
        {
            InitializeWithDefaultAssets();
            AudioStormDriver driver = RunStorm();

            Assert.LessOrEqual(driver.MaxPlayingVoices, Service.VoiceCount);
            Assert.Greater(driver.MaxPlayingVoices, DefaultAudioAssets.LoadConfig().MusicVoices);
            Assert.Greater(driver.Steps, 0);
        }

        [Test]
        public void AC11_MusicPlaying_SurvivesStorm_OnlyMusicVoicesReceiveMusic()
        {
            SoundCue music = CreateMusicCue(MusicCueId);
            InitializeWithDefaultAssets(music);
            AudioConfig config = DefaultAudioAssets.LoadConfig();
            AssertPlayed(PlayAt(MusicCueId, 0.5));
            int musicVoice = Backend.LastStartedVoice;
            Assert.Less(musicVoice, config.MusicVoices);
            UnityEngine.AudioClip musicClip = Backend.GetClip(musicVoice);

            RunStorm();

            Assert.AreEqual(config.TotalVoices, Service.VoiceCount);
            Assert.IsTrue(Backend.IsPlaying(musicVoice));
            Assert.AreEqual(0, Backend.GetStopCount(musicVoice));
            Assert.AreEqual(1, Backend.GetStartCount(musicVoice));
            Assert.AreSame(musicClip, Backend.GetClip(musicVoice));
            for (int voice = config.MusicVoices; voice < config.TotalVoices; voice++)
            {
                Assert.AreNotSame(musicClip, Backend.GetClip(voice), "voice " + voice);
            }
        }

        [Test]
        public void Pool_PartitionCounts_MatchConfig()
        {
            VoicePool pool = new VoicePool(16, 2, 3);
            int music = 0;
            int ambience = 0;
            int sfx = 0;
            for (int i = 0; i < pool.Count; i++)
            {
                switch (pool.GetSlot(i).Partition)
                {
                    case VoicePartition.Music:
                        music++;
                        break;
                    case VoicePartition.Ambience:
                        ambience++;
                        break;
                    default:
                        sfx++;
                        break;
                }
            }

            Assert.AreEqual(2, music);
            Assert.AreEqual(3, ambience);
            Assert.AreEqual(11, sfx);
        }

        private AudioStormDriver RunStorm()
        {
            AudioStormDriver driver = new AudioStormDriver(
                Service,
                Clock,
                Backend,
                DefaultAudioAssets.LoadTargets(),
                DefaultAudioAssets.TapCooldownSeconds);
            driver.Run(StormStart);
            return driver;
        }
    }
}
