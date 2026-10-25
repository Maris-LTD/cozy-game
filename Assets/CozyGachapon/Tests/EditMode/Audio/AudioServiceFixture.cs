using System.Collections.Generic;
using CozyGachapon.Audio;
using CozyGachapon.Contracts;
using NUnit.Framework;

namespace CozyGachapon.Tests.Audio
{
    public abstract class AudioServiceFixture : AudioTestFixture
    {
        private const int DefaultRandomSeed = 20261025;
        private const double PlayStepSeconds = 1d;

        protected AudioService Service { get; private set; }
        protected FakeVoiceBackend Backend { get; private set; }
        protected FakeAudioClock Clock { get; private set; }
        protected CountingAudioDiagnosticsSink Sink { get; private set; }
        protected IAudioRandom Random { get; private set; }
        protected FakeAudioSettingsStore SettingsStore { get; private set; }

        [SetUp]
        public void SetUpAudioService()
        {
            Service = new AudioService();
            Clock = new FakeAudioClock();
            Sink = new CountingAudioDiagnosticsSink();
            Backend = null;
            Random = new SystemAudioRandom(DefaultRandomSeed);
            SettingsStore = new FakeAudioSettingsStore();
        }

        protected void UseSettingsStore(FakeAudioSettingsStore store)
        {
            SettingsStore = store;
        }

        protected void UseRandom(IAudioRandom random)
        {
            Random = random;
        }

        protected void InitializeWithDefaultAssets()
        {
            AudioConfig config = DefaultAudioAssets.LoadConfig();
            Assert.IsNotNull(config);
            Backend = new FakeVoiceBackend(config.TotalVoices);
            SoundCueRepository repository = new SoundCueRepository(DefaultAudioAssets.LoadCues());
            Assert.IsTrue(Service.Initialize(config, repository, Backend, Clock, Sink, Random, SettingsStore));
        }

        protected void InitializeWithDefaultAssets(params SoundCue[] extraCues)
        {
            AudioConfig config = DefaultAudioAssets.LoadConfig();
            Assert.IsNotNull(config);
            Backend = new FakeVoiceBackend(config.TotalVoices);
            List<SoundCue> cues = DefaultAudioAssets.LoadCues();
            cues.AddRange(extraCues);
            Assert.IsTrue(Service.Initialize(config, new SoundCueRepository(cues), Backend, Clock, Sink, Random, SettingsStore));
        }

        protected void InitializeWithCues(params SoundCue[] cues)
        {
            Backend = new FakeVoiceBackend(Config.TotalVoices);
            SoundCueRepository repository = new SoundCueRepository(new List<SoundCue>(cues));
            Assert.IsTrue(Service.Initialize(Config, repository, Backend, Clock, Sink, Random, SettingsStore));
        }

        protected SoundCue CreateLimitedCue(
            string cueId,
            AudioBus bus,
            int maxInstances,
            float minInterval,
            InstanceCapPolicy policy)
        {
            SoundCue cue = CreateCue(cueId);
            cue.SetIdentityForTest(cueId, bus);
            cue.SetLimitsForTest(50, maxInstances, minInterval);
            cue.SetInstanceCapPolicyForTest(policy);
            return cue;
        }

        protected SoundCue CreateMusicCue(string cueId)
        {
            SoundCue cue = CreateLimitedCue(cueId, AudioBus.Music, 1, 0f, InstanceCapPolicy.StealOldest);
            cue.SetLimitsForTest(0, 1, 0f);
            SetLoop(cue, true);
            return cue;
        }

        protected static void SetKeepWhenBusBackground(SoundCue cue, bool keep)
        {
            SetCueBool(cue, "_keepWhenBusBackground", keep);
        }

        protected static void SetAvoidRepeat(SoundCue cue, bool avoidRepeat)
        {
            SetCueBool(cue, "_avoidRepeat", avoidRepeat);
        }

        protected static void SetPanFromPosition(SoundCue cue, bool panFromPosition)
        {
            SetCueBool(cue, "_panFromPosition", panFromPosition);
        }

        protected int ResolveCue(string cueId)
        {
            Assert.IsTrue(Service.TryResolveCue(cueId, out int cueIndex), cueId);
            return cueIndex;
        }

        protected int PlayVoice(int cueIndex, in AudioPlayParams playParams)
        {
            Clock.Advance(PlayStepSeconds);
            AssertPlayed(Service.Play(cueIndex, playParams));
            int voice = Backend.LastStartedVoice;
            Backend.FinishVoice(voice);
            Service.Tick();
            return voice;
        }

        protected PlayResult PlayAt(string cueId, double now)
        {
            Clock.Set(now);
            return Service.Play(cueId, AudioPlayParams.None);
        }

        protected int PlayVoiceAt(string cueId, double now)
        {
            AssertPlayed(PlayAt(cueId, now));
            return Backend.LastStartedVoice;
        }

        protected static void AssertDropped(DropReason expected, PlayResult result)
        {
            Assert.IsFalse(result.Played);
            Assert.AreEqual(expected, result.Reason);
        }

        protected static void AssertPlayed(PlayResult result)
        {
            Assert.IsTrue(result.Played, result.Reason.ToString());
            Assert.AreEqual(DropReason.None, result.Reason);
        }
    }
}
