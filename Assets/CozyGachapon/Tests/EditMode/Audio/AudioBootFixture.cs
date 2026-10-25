using CozyGachapon.Audio;
using CozyGachapon.Contracts;
using NUnit.Framework;

namespace CozyGachapon.Tests.Audio
{
    public abstract class AudioBootFixture : AudioTestFixture
    {
        private const int RandomSeed = 20261025;

        protected FakeAudioClock Clock { get; private set; }
        protected CountingAudioDiagnosticsSink Sink { get; private set; }
        protected FakeAudioSettingsStore Store { get; private set; }

        [SetUp]
        public void SetUpBootFixture()
        {
            Clock = new FakeAudioClock();
            Sink = new CountingAudioDiagnosticsSink();
            Store = new FakeAudioSettingsStore();
        }

        protected AudioConfig LoadDefaultConfig()
        {
            AudioConfig config = DefaultAudioAssets.LoadConfig();
            Assert.IsNotNull(config);
            return config;
        }

        protected SoundCueRepository CreateDefaultRepository()
        {
            return new SoundCueRepository(DefaultAudioAssets.LoadCues());
        }

        protected AudioBootModule CreateValidModule()
        {
            AudioConfig config = LoadDefaultConfig();
            return CreateModule(config, new FakeVoiceBackend(config.TotalVoices), Sink);
        }

        protected AudioBootModule CreateMissingConfigModule()
        {
            return CreateModule(null, new FakeVoiceBackend(1), Sink);
        }

        protected AudioBootModule CreateModule(AudioConfig config, IVoiceBackend backend, IAudioDiagnosticsSink sink)
        {
            return new AudioBootModule(
                config, CreateDefaultRepository(), backend, Clock, sink, new SystemAudioRandom(RandomSeed), Store);
        }

        protected static void AssertNotReady(PlayResult result)
        {
            Assert.IsFalse(result.Played);
            Assert.AreEqual(DropReason.NotReady, result.Reason);
        }
    }
}
