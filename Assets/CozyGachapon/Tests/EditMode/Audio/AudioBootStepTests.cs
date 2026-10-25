using CozyGachapon.Audio;
using CozyGachapon.Contracts;
using NUnit.Framework;

namespace CozyGachapon.Tests.Audio
{
    public class AudioBootStepTests : AudioBootFixture
    {
        [Test]
        public void AC41_RegisterSteps_RegistersExactlyOneNonCriticalServiceInitStep()
        {
            AudioBootModule module = CreateValidModule();
            FakeBootStepRegistry registry = new FakeBootStepRegistry();

            Assert.IsTrue(module.RegisterSteps(registry.Add));

            Assert.AreEqual(1, registry.Steps.Count);
            IBootStep step = registry.Steps[0];
            Assert.AreEqual("audio.service", step.Id);
            Assert.AreEqual(BootPhase.ServiceInit, step.Phase);
            Assert.AreEqual(0, step.RunsAfter.Count);
            Assert.IsFalse(step.IsCritical);
            Assert.AreSame(module.ServiceStep, step);
        }

        [Test]
        public void AC41_PlayBeforeRun_DropsNotReady()
        {
            AudioBootModule module = CreateValidModule();

            AssertNotReady(module.Service.Play("Ui.Tap", AudioPlayParams.None));
            AssertNotReady(module.Service.Play(0, AudioPlayParams.None));
            Assert.AreEqual(AudioLifecycle.Uninitialized, module.Service.RuntimeState.Lifecycle);
            Assert.IsFalse(module.ServiceStep.IsDone);
            Assert.IsFalse(module.ServiceStep.IsFailed);
        }

        [Test]
        public void AC41_RunWithValidConfig_IsDone_VoiceCountEqualsTotalVoices()
        {
            AudioConfig config = LoadDefaultConfig();
            AudioBootModule module = CreateValidModule();

            module.ServiceStep.Run();

            Assert.IsTrue(module.ServiceStep.IsDone);
            Assert.IsFalse(module.ServiceStep.IsFailed);
            Assert.IsTrue(module.Service.IsReady);
            Assert.AreEqual(AudioLifecycle.Active, module.Service.RuntimeState.Lifecycle);
            Assert.AreEqual(config.TotalVoices, module.Service.VoiceCount);
        }

        [Test]
        public void AC41_RunTwice_DoesNotCreateMoreVoices()
        {
            AudioConfig config = LoadDefaultConfig();
            FakeVoiceBackend backend = new FakeVoiceBackend(config.TotalVoices);
            AudioBootModule module = CreateModule(config, backend, Sink);

            module.ServiceStep.Run();
            module.ServiceStep.Run();

            Assert.IsTrue(module.ServiceStep.IsDone);
            Assert.IsFalse(module.ServiceStep.IsFailed);
            Assert.AreEqual(config.TotalVoices, module.Service.VoiceCount);
            Assert.AreEqual(0, backend.StartCount);
            Assert.AreEqual(0, Sink.ErrorCount);
        }

        [Test]
        public void AC41_RunWithMissingConfig_IsFailed_NoException()
        {
            AudioBootModule module = CreateMissingConfigModule();

            Assert.DoesNotThrow(module.ServiceStep.Run);

            Assert.IsTrue(module.ServiceStep.IsFailed);
            Assert.IsFalse(module.ServiceStep.IsDone);
            Assert.IsFalse(module.Service.IsReady);
            Assert.AreEqual(AudioLifecycle.Failed, module.Service.RuntimeState.Lifecycle);
            Assert.AreEqual(1, Sink.CountErrors(AudioDiagnosticCode.InitializeInvalidInput));
        }

        [Test]
        public void AC41_RunWithThrowingBackend_IsFailed_ReportsException()
        {
            AudioBootModule module = CreateModule(LoadDefaultConfig(), new ThrowingVoiceBackend(), Sink);

            Assert.DoesNotThrow(module.ServiceStep.Run);

            Assert.IsTrue(module.ServiceStep.IsFailed);
            Assert.IsFalse(module.ServiceStep.IsDone);
            Assert.AreEqual(AudioLifecycle.Failed, module.Service.RuntimeState.Lifecycle);
            Assert.AreEqual(1, Sink.CountErrors(AudioDiagnosticCode.InitializeException));
        }

        [Test]
        public void AC41_RunWithThrowingBackendAndNullSink_DoesNotThrow()
        {
            AudioBootModule module = CreateModule(LoadDefaultConfig(), new ThrowingVoiceBackend(), null);

            Assert.DoesNotThrow(module.ServiceStep.Run);

            Assert.IsTrue(module.ServiceStep.IsFailed);
            Assert.AreEqual(AudioLifecycle.Failed, module.Service.RuntimeState.Lifecycle);
        }

        [Test]
        public void AC41_RunWithTooFewBackendVoices_IsFailed()
        {
            AudioConfig config = LoadDefaultConfig();
            AudioBootModule module = CreateModule(config, new FakeVoiceBackend(config.TotalVoices - 1), Sink);

            module.ServiceStep.Run();

            Assert.IsTrue(module.ServiceStep.IsFailed);
            Assert.AreEqual(0, module.Service.VoiceCount);
            Assert.AreEqual(AudioLifecycle.Failed, module.Service.RuntimeState.Lifecycle);
        }

        [Test]
        public void AC41_FailedService_InitializeAgain_IsRejected()
        {
            AudioConfig config = LoadDefaultConfig();
            AudioBootModule module = CreateModule(config, new FakeVoiceBackend(config.TotalVoices - 1), Sink);
            module.ServiceStep.Run();

            bool initialized = module.Service.Initialize(
                config,
                CreateDefaultRepository(),
                new FakeVoiceBackend(config.TotalVoices),
                Clock,
                Sink,
                new SystemAudioRandom(1),
                Store);

            Assert.IsFalse(initialized);
            Assert.IsFalse(module.Service.IsReady);
            Assert.AreEqual(AudioLifecycle.Failed, module.Service.RuntimeState.Lifecycle);
        }

        [Test]
        public void AC41_ModuleHandlers_ForwardPausedResumed_AndRememberFirstInteraction()
        {
            AudioBootModule module = CreateValidModule();

            module.HandleFirstInteraction(new FirstInteractionEvent());
            Assert.IsTrue(module.Service.RuntimeState.HasFirstInteraction);

            module.ServiceStep.Run();
            module.HandlePaused(new PausedEvent(PauseReason.Hidden));
            Assert.IsTrue(module.Service.RuntimeState.IsSuspended);

            module.HandleResumed(new ResumedEvent(PauseReason.Hidden, 1d));
            Assert.IsFalse(module.Service.RuntimeState.IsSuspended);
        }
    }
}
