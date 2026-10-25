using System.Collections.Generic;
using CozyGachapon.Audio;
using CozyGachapon.Contracts;
using NUnit.Framework;
using UnityEditor;

namespace CozyGachapon.Tests.Audio
{
    public class AudioCueExclusionTests : AudioServiceFixture
    {
        private const string ValidId = "Test.Valid";
        private const string NoClipId = "Test.NoClip";
        private const string ZeroInstancesId = "Test.ZeroInstances";
        private const string NegativeIntervalId = "Test.NegativeInterval";
        private const string NaNIntervalId = "Test.NaNInterval";
        private const string DuplicateId = "Test.Duplicate";
        private const string InvalidBusId = "Test.InvalidBus";
        private const string BadPriorityId = "Test.BadPriority";
        private const AudioBus UndefinedBus = (AudioBus)99;

        [Test]
        public void AC7_BrokenCues_ResolveAsUnknown_AndSinkWarns()
        {
            SoundCue zeroInstances = CreateLimitedCue(ZeroInstancesId, AudioBus.Plinko, 0, 0f, InstanceCapPolicy.DropNew);
            SoundCue negativeInterval = CreateLimitedCue(NegativeIntervalId, AudioBus.Plinko, 1, -0.5f, InstanceCapPolicy.DropNew);
            SoundCue nanInterval = CreateLimitedCue(NaNIntervalId, AudioBus.Plinko, 1, float.NaN, InstanceCapPolicy.DropNew);
            SoundCue invalidBus = CreateLimitedCue(InvalidBusId, UndefinedBus, 1, 0f, InstanceCapPolicy.DropNew);
            SoundCue badPriority = CreateLimitedCue(BadPriorityId, AudioBus.Plinko, 1, 0f, InstanceCapPolicy.DropNew);
            badPriority.SetLimitsForTest(SoundCue.PriorityMax + 1, 1, 0f);
            InitializeWithCues(
                CreateLimitedCue(ValidId, AudioBus.Plinko, 1, 0f, InstanceCapPolicy.DropNew),
                CreateCue(NoClipId, false),
                zeroInstances,
                negativeInterval,
                nanInterval,
                CreateCue(DuplicateId),
                CreateCue(DuplicateId),
                invalidBus,
                badPriority);

            string[] brokenIds = { NoClipId, ZeroInstancesId, NegativeIntervalId, NaNIntervalId, DuplicateId, InvalidBusId, BadPriorityId };
            for (int i = 0; i < brokenIds.Length; i++)
            {
                Assert.IsFalse(Service.TryResolveCue(brokenIds[i], out _), brokenIds[i]);
                AssertDropped(DropReason.UnknownCue, PlayAt(brokenIds[i], 1d));
            }

            Assert.AreEqual(0, Backend.StartCount);
            Assert.AreEqual(brokenIds.Length, Sink.CountWarnings(AudioDiagnosticCode.UnknownCue));
            Assert.AreEqual(6, Sink.CountWarnings(AudioDiagnosticCode.CueExcluded));
            Assert.IsTrue(Service.TryResolveCue(ValidId, out _));
            AssertPlayed(PlayAt(ValidId, 1d));
        }

        [Test]
        public void Initialize_NullInputs_StaysNotReady_AndReportsError()
        {
            FakeVoiceBackend backend = new FakeVoiceBackend(Config.TotalVoices);
            SoundCueRepository repository = new SoundCueRepository(new List<SoundCue>());

            Assert.IsFalse(Service.Initialize(null, repository, backend, Clock, Sink));
            Assert.IsFalse(Service.Initialize(Config, null, backend, Clock, Sink));
            Assert.IsFalse(Service.Initialize(Config, repository, null, Clock, Sink));
            Assert.IsFalse(Service.Initialize(Config, repository, backend, null, Sink));
            Assert.IsFalse(Service.Initialize(Config, repository, backend, Clock, null));

            Assert.IsFalse(Service.IsReady);
            Assert.AreEqual(4, Sink.CountErrors(AudioDiagnosticCode.InitializeInvalidInput));
            AssertDropped(DropReason.NotReady, Service.Play(ValidId, AudioPlayParams.None));
        }

        [Test]
        public void Initialize_BackendWithTooFewVoices_StaysNotReady()
        {
            FakeVoiceBackend backend = new FakeVoiceBackend(Config.TotalVoices - 1);
            SoundCueRepository repository = new SoundCueRepository(new List<SoundCue>());

            Assert.IsFalse(Service.Initialize(Config, repository, backend, Clock, Sink));
            Assert.IsFalse(Service.IsReady);
            Assert.AreEqual(1, Sink.CountErrors(AudioDiagnosticCode.InitializeInvalidInput));
        }

        [Test]
        public void Initialize_ReservedVoicesExceedTotal_StaysNotReady()
        {
            SerializedObject serialized = new SerializedObject(Config);
            serialized.FindProperty("_musicVoices").intValue = Config.TotalVoices + 1;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            FakeVoiceBackend backend = new FakeVoiceBackend(Config.TotalVoices);
            SoundCueRepository repository = new SoundCueRepository(new List<SoundCue>());

            Assert.IsFalse(Service.Initialize(Config, repository, backend, Clock, Sink));
            Assert.IsFalse(Service.IsReady);
        }

        [Test]
        public void Initialize_SecondCall_IsRejected_AndKeepsFirstSetup()
        {
            InitializeWithCues(CreateLimitedCue(ValidId, AudioBus.Plinko, 1, 0f, InstanceCapPolicy.DropNew));
            FakeVoiceBackend other = new FakeVoiceBackend(Config.TotalVoices);
            SoundCueRepository empty = new SoundCueRepository(new List<SoundCue>());

            Assert.IsFalse(Service.Initialize(Config, empty, other, Clock, Sink));
            Assert.IsTrue(Service.IsReady);
            AssertPlayed(PlayAt(ValidId, 1d));
            Assert.AreEqual(0, other.StartCount);
        }
    }
}
