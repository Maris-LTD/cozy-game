using System;
using System.Collections.Generic;
using CozyGachapon.Contracts;
using CozyGachapon.Foundation;
using NUnit.Framework;

namespace CozyGachapon.Tests.Bootstrap
{
    public class BootStepSequencerTests
    {
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
        public void AC1_ExecutionOrder_FollowsPhasesSequentially_AndInvokesCompletedOnce()
        {
            BootStepSequencer sequencer = new BootStepSequencer();
            List<string> executedIds = new List<string>();
            int completedCount = 0;

            MockBootStep stepPrewarm = new MockBootStep("prewarm_step", BootPhase.Prewarm, null, true, true, () => executedIds.Add("prewarm_step"));
            MockBootStep stepGameplay = new MockBootStep("gameplay_step", BootPhase.GameplayInit, null, true, true, () => executedIds.Add("gameplay_step"));
            MockBootStep stepService = new MockBootStep("service_step", BootPhase.ServiceInit, null, true, true, () => executedIds.Add("service_step"));
            MockBootStep stepData = new MockBootStep("data_step", BootPhase.DataLoad, null, true, true, () => executedIds.Add("data_step"));

            sequencer.RegisterStep(stepPrewarm);
            sequencer.RegisterStep(stepGameplay);
            sequencer.RegisterStep(stepService);
            sequencer.RegisterStep(stepData);

            sequencer.OnBootCompleted += _ => completedCount++;

            Assert.IsTrue(sequencer.Start());
            sequencer.Tick(0.016f);

            Assert.AreEqual(4, executedIds.Count);
            Assert.AreEqual("service_step", executedIds[0]);
            Assert.AreEqual("data_step", executedIds[1]);
            Assert.AreEqual("gameplay_step", executedIds[2]);
            Assert.AreEqual("prewarm_step", executedIds[3]);
            Assert.AreEqual(1, completedCount);
            Assert.IsTrue(sequencer.IsCompleted);
        }

        [Test]
        public void AC2_TopologicalOrder_ResolvesRunsAfter_AndAlphabeticalFallback()
        {
            BootStepSequencer sequencer = new BootStepSequencer();
            List<string> executedIds = new List<string>();

            MockBootStep stepZeta = new MockBootStep("zeta", BootPhase.GameplayInit, null, true, true, () => executedIds.Add("zeta"));
            MockBootStep stepBeta = new MockBootStep("beta", BootPhase.GameplayInit, new[] { "alpha" }, true, true, () => executedIds.Add("beta"));
            MockBootStep stepAlpha = new MockBootStep("alpha", BootPhase.GameplayInit, null, true, true, () => executedIds.Add("alpha"));

            sequencer.RegisterStep(stepZeta);
            sequencer.RegisterStep(stepBeta);
            sequencer.RegisterStep(stepAlpha);

            Assert.IsTrue(sequencer.Start());
            sequencer.Tick(0.016f);

            Assert.AreEqual(3, executedIds.Count);
            Assert.AreEqual("alpha", executedIds[0]);
            Assert.AreEqual("beta", executedIds[1]);
            Assert.AreEqual("zeta", executedIds[2]);
        }

        [Test]
        public void AC3_MultiFrameStep_BlocksDependentStepsUntilDone()
        {
            BootStepSequencer sequencer = new BootStepSequencer();
            List<string> executedIds = new List<string>();

            MockBootStep stepA = new MockBootStep("step_a", BootPhase.GameplayInit, null, false, true, () => executedIds.Add("step_a"));
            MockBootStep stepB = new MockBootStep("step_b", BootPhase.GameplayInit, new[] { "step_a" }, true, true, () => executedIds.Add("step_b"));

            sequencer.RegisterStep(stepA);
            sequencer.RegisterStep(stepB);

            Assert.IsTrue(sequencer.Start());

            sequencer.Tick(0.016f);
            Assert.AreEqual(1, stepA.RunCallCount);
            Assert.AreEqual(0, stepB.RunCallCount);
            Assert.IsFalse(sequencer.IsCompleted);

            sequencer.Tick(0.016f);
            Assert.AreEqual(1, stepA.RunCallCount);
            Assert.AreEqual(0, stepB.RunCallCount);

            stepA.IsDone = true;

            sequencer.Tick(0.016f);
            Assert.AreEqual(1, stepA.RunCallCount);
            Assert.AreEqual(1, stepB.RunCallCount);
            Assert.IsTrue(sequencer.IsCompleted);
        }

        [Test]
        public void AC4_EmptyPhase_PassesWithoutExtraFrameDelay()
        {
            BootStepSequencer sequencer = new BootStepSequencer();
            MockBootStep stepService = new MockBootStep("service_step", BootPhase.ServiceInit);
            MockBootStep stepPrewarm = new MockBootStep("prewarm_step", BootPhase.Prewarm);

            sequencer.RegisterStep(stepService);
            sequencer.RegisterStep(stepPrewarm);

            Assert.IsTrue(sequencer.Start());
            sequencer.Tick(0.016f);

            Assert.IsTrue(sequencer.IsCompleted);
        }

        [Test]
        public void AC6_DuplicateId_TriggersBootFailed_AndRejectsRegistration()
        {
            BootStepSequencer sequencer = new BootStepSequencer();
            BootFailedEvent? failedEvent = null;
            sequencer.OnBootFailed += evt => failedEvent = evt;

            MockBootStep step1 = new MockBootStep("dup_id", BootPhase.ServiceInit);
            MockBootStep step2 = new MockBootStep("dup_id", BootPhase.DataLoad);

            Assert.IsTrue(sequencer.RegisterStep(step1));
            Assert.IsFalse(sequencer.RegisterStep(step2));
            Assert.IsTrue(sequencer.HasFailed);
            Assert.IsNotNull(failedEvent);
            Assert.AreEqual("dup_id", failedEvent.Value.StepId);
        }

        [Test]
        public void AC6_RegisterAfterStart_IsRejected()
        {
            BootStepSequencer sequencer = new BootStepSequencer();
            MockBootStep step1 = new MockBootStep("step_1", BootPhase.ServiceInit);
            MockBootStep step2 = new MockBootStep("step_2", BootPhase.ServiceInit);

            Assert.IsTrue(sequencer.RegisterStep(step1));
            Assert.IsTrue(sequencer.Start());
            Assert.IsFalse(sequencer.RegisterStep(step2));
        }

        [Test]
        public void AC2_CyclicDependency_TriggersBootFailed()
        {
            BootStepSequencer sequencer = new BootStepSequencer();
            BootFailedEvent? failedEvent = null;
            sequencer.OnBootFailed += evt => failedEvent = evt;

            MockBootStep stepA = new MockBootStep("loop_a", BootPhase.GameplayInit, new[] { "loop_b" });
            MockBootStep stepB = new MockBootStep("loop_b", BootPhase.GameplayInit, new[] { "loop_a" });

            sequencer.RegisterStep(stepA);
            sequencer.RegisterStep(stepB);

            Assert.IsFalse(sequencer.Start());
            Assert.IsTrue(sequencer.HasFailed);
            Assert.IsNotNull(failedEvent);
        }

        [Test]
        public void AC6_NonCriticalStepFailure_IsSkipped_AndBootProceeds()
        {
            BootStepSequencer sequencer = new BootStepSequencer();
            MockBootStep failStep = new MockBootStep("non_crit", BootPhase.ServiceInit, null, false, false);
            failStep.IsFailed = true;
            MockBootStep okStep = new MockBootStep("crit_ok", BootPhase.ServiceInit);

            sequencer.RegisterStep(failStep);
            sequencer.RegisterStep(okStep);

            Assert.IsTrue(sequencer.Start());
            sequencer.Tick(0.016f);

            Assert.IsFalse(sequencer.HasFailed);
            Assert.IsTrue(sequencer.IsCompleted);
        }

        [Test]
        public void AC6_CriticalStepException_TriggersBootFailed()
        {
            BootStepSequencer sequencer = new BootStepSequencer();
            BootFailedEvent? failedEvent = null;
            sequencer.OnBootFailed += evt => failedEvent = evt;

            MockBootStep crashStep = new MockBootStep("crash", BootPhase.ServiceInit, null, false, true, () => throw new InvalidOperationException("Boom"));

            sequencer.RegisterStep(crashStep);

            Assert.IsTrue(sequencer.Start());
            sequencer.Tick(0.016f);

            Assert.IsTrue(sequencer.HasFailed);
            Assert.IsNotNull(failedEvent);
            Assert.AreEqual("crash", failedEvent.Value.StepId);
        }
    }
}
