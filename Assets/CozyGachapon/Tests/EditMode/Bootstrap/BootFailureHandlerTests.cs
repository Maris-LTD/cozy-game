using System;
using System.Collections.Generic;
using CozyGachapon.Contracts;
using CozyGachapon.Foundation;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Bootstrap
{
    public class BootFailureHandlerTests
    {
        private static BootStepSequencer CreateSequencer(float stepTimeout = 1.0f)
        {
            BootConfig bootConfig = ScriptableObject.CreateInstance<BootConfig>();
            bootConfig.SetStepTimeoutForTest(stepTimeout);
            SceneFlowConfig sceneFlowConfig = ScriptableObject.CreateInstance<SceneFlowConfig>();
            return new BootStepSequencer(bootConfig, sceneFlowConfig);
        }

        private sealed class MockBootStep : IBootStep
        {
            private readonly Action _onRun;
            public string Id { get; }
            public BootPhase Phase { get; }
            public IReadOnlyList<string> RunsAfter { get; }
            public bool IsCritical { get; set; }
            public bool IsDone { get; set; }
            public bool IsFailed { get; set; }
            public int RunCallCount { get; private set; }

            public MockBootStep(string id, BootPhase phase, IReadOnlyList<string> runsAfter = null, bool isDone = true, bool isCritical = true, Action onRun = null)
            {
                Id = id;
                Phase = phase;
                RunsAfter = runsAfter ?? Array.Empty<string>();
                IsCritical = isCritical;
                IsDone = isDone;
                _onRun = onRun;
            }

            public void Run()
            {
                RunCallCount++;
                _onRun?.Invoke();
            }
        }

        [Test]
        public void AC5_InvalidDependencyConfig_FailsWithinOneFrame()
        {
            BootStepSequencer sequencer = CreateSequencer();
            BootFailedEvent? failedEvent = null;
            sequencer.OnBootFailed += evt => failedEvent = evt;

            MockBootStep stepWithUnknownDep = new MockBootStep("step_x", BootPhase.GameplayInit, new[] { "non_existent_id" });
            sequencer.RegisterStep(stepWithUnknownDep);

            Assert.IsFalse(sequencer.Start());
            Assert.IsTrue(sequencer.HasFailed);
            Assert.IsNotNull(failedEvent);
        }

        [Test]
        public void AC8_NonCriticalStepFailure_RecordsInLog_AndCompletesBoot()
        {
            BootStepSequencer sequencer = CreateSequencer();
            bool completedFired = false;
            sequencer.OnBootCompleted += _ => completedFired = true;

            MockBootStep audioStep = new MockBootStep("audio_step", BootPhase.ServiceInit, null, false, false, () => throw new InvalidOperationException("Audio init error"));
            MockBootStep nextStep = new MockBootStep("next_step", BootPhase.ServiceInit, null, true, true);

            sequencer.RegisterStep(audioStep);
            sequencer.RegisterStep(nextStep);

            Assert.IsTrue(sequencer.Start());
            sequencer.Tick(0.016f);

            Assert.IsFalse(sequencer.HasFailed);
            Assert.IsTrue(sequencer.IsCompleted);
            Assert.IsTrue(completedFired);
            Assert.Contains("audio_step", (System.Collections.ICollection)sequencer.SkippedNonCriticalStepIds);
        }

        [Test]
        public void AC9_CriticalStepFailure_HaltsSequencing_AndFiresBootFailedOnce()
        {
            BootStepSequencer sequencer = CreateSequencer();
            int failedEventCount = 0;
            BootFailedEvent? recordedEvent = null;

            sequencer.OnBootFailed += evt =>
            {
                failedEventCount++;
                recordedEvent = evt;
            };

            MockBootStep catalogStep = new MockBootStep("pet_catalog", BootPhase.DataLoad, null, false, true);
            catalogStep.IsFailed = true;
            MockBootStep afterStep = new MockBootStep("z_after_step", BootPhase.DataLoad, new[] { "pet_catalog" }, true, true);

            sequencer.RegisterStep(catalogStep);
            sequencer.RegisterStep(afterStep);

            Assert.IsTrue(sequencer.Start());
            sequencer.Tick(0.016f);

            Assert.IsTrue(sequencer.HasFailed);
            Assert.IsFalse(sequencer.IsCompleted);
            Assert.AreEqual(1, failedEventCount);
            Assert.AreEqual("pet_catalog", recordedEvent.Value.StepId);
            Assert.AreEqual(BootPhase.DataLoad, recordedEvent.Value.Phase);
            Assert.AreEqual(0, afterStep.RunCallCount);
        }

        [Test]
        public void AC11_CriticalStepTimeout_TriggersBootFailed_OnlyCountingVisibleTime()
        {
            BootStepSequencer sequencer = CreateSequencer(1.0f);
            BootFailedEvent? failedEvent = null;
            sequencer.OnBootFailed += evt => failedEvent = evt;

            MockBootStep slowStep = new MockBootStep("slow_critical", BootPhase.GameplayInit, null, false, true);
            sequencer.RegisterStep(slowStep);

            Assert.IsTrue(sequencer.Start());

            sequencer.Tick(0.5f, false);
            sequencer.Tick(0.6f, false);
            Assert.IsFalse(sequencer.HasFailed);

            sequencer.Tick(0.5f, true);
            Assert.IsFalse(sequencer.HasFailed);

            sequencer.Tick(0.6f, true);
            Assert.IsTrue(sequencer.HasFailed);
            Assert.IsNotNull(failedEvent);
            Assert.AreEqual("slow_critical", failedEvent.Value.StepId);
        }

        [Test]
        public void AC11_NonCriticalStepTimeout_IsSkipped_AndCompletesBoot()
        {
            BootStepSequencer sequencer = CreateSequencer(0.5f);
            bool completedFired = false;
            sequencer.OnBootCompleted += _ => completedFired = true;

            MockBootStep slowNonCrit = new MockBootStep("slow_audio", BootPhase.ServiceInit, null, false, false);
            MockBootStep fastStep = new MockBootStep("fast_step", BootPhase.ServiceInit, null, true, true);

            sequencer.RegisterStep(slowNonCrit);
            sequencer.RegisterStep(fastStep);

            Assert.IsTrue(sequencer.Start());

            sequencer.Tick(0.3f, true);
            Assert.IsFalse(sequencer.HasFailed);
            Assert.IsFalse(sequencer.IsCompleted);

            sequencer.Tick(0.3f, true);
            Assert.IsFalse(sequencer.HasFailed);
            Assert.IsTrue(sequencer.IsCompleted);
            Assert.IsTrue(completedFired);
            Assert.Contains("slow_audio", (System.Collections.ICollection)sequencer.SkippedNonCriticalStepIds);
        }

        [Test]
        public void AC12_MissingMandatoryConfigs_FailsImmediatelyBeforeAnyRun()
        {
            BootStepSequencer sequencer = new BootStepSequencer(null, null);
            BootFailedEvent? failedEvent = null;
            sequencer.OnBootFailed += evt => failedEvent = evt;

            MockBootStep step = new MockBootStep("test_step", BootPhase.ServiceInit, null, true, true);
            sequencer.RegisterStep(step);

            Assert.IsFalse(sequencer.Start());
            Assert.IsTrue(sequencer.HasFailed);
            Assert.IsNotNull(failedEvent);
            Assert.AreEqual("boot.config", failedEvent.Value.StepId);
            Assert.AreEqual(0, step.RunCallCount);
        }
    }
}
