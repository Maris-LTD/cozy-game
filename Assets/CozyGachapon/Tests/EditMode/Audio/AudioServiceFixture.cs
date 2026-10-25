using System.Collections.Generic;
using CozyGachapon.Audio;
using CozyGachapon.Contracts;
using NUnit.Framework;
using UnityEditor;

namespace CozyGachapon.Tests.Audio
{
    public abstract class AudioServiceFixture : AudioTestFixture
    {
        protected AudioService Service { get; private set; }
        protected FakeVoiceBackend Backend { get; private set; }
        protected FakeAudioClock Clock { get; private set; }
        protected CountingAudioDiagnosticsSink Sink { get; private set; }

        [SetUp]
        public void SetUpAudioService()
        {
            Service = new AudioService();
            Clock = new FakeAudioClock();
            Sink = new CountingAudioDiagnosticsSink();
            Backend = null;
        }

        protected void InitializeWithDefaultAssets()
        {
            AudioConfig config = DefaultAudioAssets.LoadConfig();
            Assert.IsNotNull(config);
            Backend = new FakeVoiceBackend(config.TotalVoices);
            SoundCueRepository repository = new SoundCueRepository(DefaultAudioAssets.LoadCues());
            Assert.IsTrue(Service.Initialize(config, repository, Backend, Clock, Sink));
        }

        protected void InitializeWithCues(params SoundCue[] cues)
        {
            Backend = new FakeVoiceBackend(Config.TotalVoices);
            SoundCueRepository repository = new SoundCueRepository(new List<SoundCue>(cues));
            Assert.IsTrue(Service.Initialize(Config, repository, Backend, Clock, Sink));
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

        protected static void SetKeepWhenBusBackground(SoundCue cue, bool keep)
        {
            SetCueBool(cue, "_keepWhenBusBackground", keep);
        }

        protected static void SetLoop(SoundCue cue, bool loop)
        {
            SetCueBool(cue, "_loop", loop);
        }

        protected PlayResult PlayAt(string cueId, double now)
        {
            Clock.Set(now);
            return Service.Play(cueId, AudioPlayParams.None);
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

        private static void SetCueBool(SoundCue cue, string propertyName, bool value)
        {
            SerializedObject serialized = new SerializedObject(cue);
            SerializedProperty property = serialized.FindProperty(propertyName);
            Assert.IsNotNull(property, propertyName);
            property.boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
