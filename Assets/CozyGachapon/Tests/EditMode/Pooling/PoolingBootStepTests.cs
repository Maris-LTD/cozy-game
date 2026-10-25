using System;
using System.Collections.Generic;
using CozyGachapon.Contracts;
using CozyGachapon.Foundation;
using CozyGachapon.Pooling;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Pooling
{
    public class PoolingBootStepTests : PoolingTestFixture
    {
        private const int TargetFps = 60;
        private const float FrameSeconds = 0.016f;
        private const int TickGuard = 200;

        private PoolingBootModule CreateModule(Transform root)
        {
            return new PoolingBootModule(Config, root, Sink, TargetFps);
        }

        private BootStepSequencer CreateSequencer()
        {
            BootConfig bootConfig = Track(ScriptableObject.CreateInstance<BootConfig>());
            SceneFlowConfig sceneFlowConfig = Track(ScriptableObject.CreateInstance<SceneFlowConfig>());
            return new BootStepSequencer(bootConfig, sceneFlowConfig);
        }

        private void ConfigurePools(int count, int perFrame)
        {
            List<PoolDefinition> definitions = new List<PoolDefinition>();
            for (int i = 0; i < count; i++)
            {
                definitions.Add(new PoolDefinition(PoolId(i), CreateVisualPrefab(PoolId(i), 1), PoolExhaustionPolicy.Fixed, 0));
            }

            Config.SetPoolsForTest(definitions);
            Config.SetLimitsForTest(1000, 10000);
            Config.SetPrewarmForTest(perFrame, 0.25f);
        }

        private List<RegisteringBootStep> RegisterGameplaySteps(BootStepSequencer sequencer, PoolingBootModule module, int count, int declared)
        {
            List<RegisteringBootStep> steps = new List<RegisteringBootStep>();
            for (int i = 0; i < count; i++)
            {
                RegisteringBootStep step = new RegisteringBootStep("gameplay." + i, () => module.Service, PoolId(i), declared);
                steps.Add(step);
                Assert.IsTrue(sequencer.RegisterStep(step));
            }

            return steps;
        }

        [Test]
        public void AC30_RegisterSteps_RegistersExactlyTwoCriticalSteps()
        {
            ConfigurePools(0, 10);
            PoolingBootModule module = CreateModule(CreateRoot());
            FakeStepRegistry registry = new FakeStepRegistry();

            bool accepted = module.RegisterSteps(registry.Add);

            Assert.IsTrue(accepted);
            Assert.AreEqual(2, registry.Steps.Count);
            Assert.AreEqual("pooling.service", registry.Steps[0].Id);
            Assert.AreEqual(BootPhase.ServiceInit, registry.Steps[0].Phase);
            Assert.IsTrue(registry.Steps[0].IsCritical);
            Assert.AreEqual("pooling.prewarm", registry.Steps[1].Id);
            Assert.AreEqual(BootPhase.Prewarm, registry.Steps[1].Phase);
            Assert.IsTrue(registry.Steps[1].IsCritical);
            Assert.AreNotEqual(registry.Steps[0].Id, registry.Steps[1].Id);
            Assert.AreEqual(0, registry.Steps[0].RunsAfter.Count);
            Assert.AreEqual(0, registry.Steps[1].RunsAfter.Count);
        }

        [Test]
        public void AC30_RealBoot_AcceptsEveryRegistrationBeforePrewarmRun_AndDoneOnlyWhenReady()
        {
            ConfigurePools(3, 10);
            Transform root = CreateRoot();
            PoolingBootModule module = CreateModule(root);
            BootStepSequencer sequencer = CreateSequencer();
            Assert.IsTrue(module.RegisterSteps(sequencer.RegisterStep));
            List<RegisteringBootStep> gameplaySteps = RegisterGameplaySteps(sequencer, module, 3, 10);
            Assert.IsTrue(sequencer.Start());

            int ticks = 0;
            while (!sequencer.IsCompleted && !sequencer.HasFailed && ticks < TickGuard)
            {
                module.Tick();
                sequencer.Tick(FrameSeconds);
                ticks++;
                bool ready = module.Service.Phase == PoolServicePhase.Ready;
                Assert.AreEqual(ready, module.PrewarmStep.IsDone);
            }

            Assert.IsTrue(sequencer.IsCompleted);
            Assert.IsFalse(sequencer.HasFailed);
            for (int i = 0; i < gameplaySteps.Count; i++)
            {
                Assert.IsTrue(gameplaySteps[i].Result.Accepted);
                Assert.AreEqual(PoolServicePhase.Accepting, gameplaySteps[i].PhaseAtRun);
            }

            Assert.AreEqual(1 + PrewarmBudget.Frames(30, 10), ticks);
            Assert.AreEqual(0, Sink.ErrorCount);
            Assert.AreEqual(3, root.childCount);
        }

        [Test]
        public void AC30_PrewarmIsNotDoneBeforeReady_AndDoneRightAtReady()
        {
            ConfigurePools(1, 4);
            PoolingBootModule module = CreateModule(CreateRoot());
            BootStepSequencer sequencer = CreateSequencer();
            module.RegisterSteps(sequencer.RegisterStep);
            RegisterGameplaySteps(sequencer, module, 1, 10);
            sequencer.Start();

            sequencer.Tick(FrameSeconds);
            Assert.IsFalse(module.PrewarmStep.IsDone);
            Assert.AreEqual(PoolServicePhase.Prewarming, module.Service.Phase);

            module.Tick();
            module.Tick();
            Assert.IsFalse(module.PrewarmStep.IsDone);

            module.Tick();
            Assert.AreEqual(PoolServicePhase.Ready, module.Service.Phase);
            Assert.IsTrue(module.PrewarmStep.IsDone);
        }

        [Test]
        public void ServiceStep_IsDoneRightAfterRun_AndEachRunCreatesFreshEmptyService()
        {
            ConfigurePools(1, 10);
            PoolingServiceBootStep step = new PoolingServiceBootStep(Config, Sink);
            Assert.IsFalse(step.IsDone);

            step.Run();
            ObjectPoolService first = step.Service;

            Assert.IsTrue(step.IsDone);
            Assert.IsFalse(step.IsFailed);
            Assert.AreEqual(PoolServicePhase.Accepting, first.Phase);
            Assert.AreEqual(0, first.RegisteredCapacityTotal);

            first.Register(PoolId(0), 5, false);
            step.Run();

            Assert.AreNotSame(first, step.Service);
            Assert.AreEqual(0, step.Service.RegisteredCapacityTotal);
        }

        [Test]
        public void EmptyPools_BootCompletesInOneTick()
        {
            ConfigurePools(0, 10);
            PoolingBootModule module = CreateModule(CreateRoot());
            BootStepSequencer sequencer = CreateSequencer();
            module.RegisterSteps(sequencer.RegisterStep);
            sequencer.Start();

            sequencer.Tick(FrameSeconds);

            Assert.IsTrue(sequencer.IsCompleted);
            Assert.AreEqual(PoolServicePhase.Ready, module.Service.Phase);
            Assert.IsTrue(module.PrewarmStep.IsDone);
        }

        [Test]
        public void PrewarmStep_AfterServiceStepRunsAgain_DoesNotReportOldSessionAsDone()
        {
            ConfigurePools(0, 10);
            PoolingBootModule module = CreateModule(CreateRoot());
            module.ServiceStep.Run();
            module.PrewarmStep.Run();
            Assert.IsTrue(module.PrewarmStep.IsDone);

            module.ServiceStep.Run();

            Assert.IsFalse(module.PrewarmStep.IsDone);
            Assert.IsFalse(module.PrewarmStep.IsFailed);
            Assert.AreEqual(PoolServicePhase.Accepting, module.Service.Phase);
        }

        [Test]
        public void PrewarmRun_BeforeServiceStepRan_FailsWithoutException()
        {
            ConfigurePools(0, 10);
            PoolingBootModule module = CreateModule(CreateRoot());

            Assert.DoesNotThrow(() => module.PrewarmStep.Run());
            Assert.DoesNotThrow(() => module.Tick());

            Assert.IsTrue(module.PrewarmStep.IsFailed);
            Assert.IsFalse(module.PrewarmStep.IsDone);
        }

        [Test]
        public void PrewarmRun_PerFrameZero_ReportsValidatorErrorToSink_AndStillCompletes()
        {
            ConfigurePools(1, 0);
            PoolingBootModule module = CreateModule(CreateRoot());
            BootStepSequencer sequencer = CreateSequencer();
            module.RegisterSteps(sequencer.RegisterStep);
            RegisterGameplaySteps(sequencer, module, 1, 3);
            sequencer.Start();

            int ticks = 0;
            while (!sequencer.IsCompleted && !sequencer.HasFailed && ticks < TickGuard)
            {
                module.Tick();
                sequencer.Tick(FrameSeconds);
                ticks++;
            }

            Assert.IsTrue(sequencer.IsCompleted);
            Assert.AreEqual(1 + 3, ticks);
            Assert.AreEqual(PoolDiagnosticCode.PrewarmPerFrameInvalid, Sink.LastErrorCode);
        }

        [Test]
        public void PrewarmRun_OverBudget_ReportsWarningToSink()
        {
            ConfigurePools(1, 1);
            Config.SetPrewarmForTest(1, 0.05f);
            PoolingBootModule module = CreateModule(CreateRoot());
            BootStepSequencer sequencer = CreateSequencer();
            module.RegisterSteps(sequencer.RegisterStep);
            RegisterGameplaySteps(sequencer, module, 1, 10);
            sequencer.Start();

            sequencer.Tick(FrameSeconds);

            Assert.AreEqual(PoolDiagnosticCode.PrewarmBudgetExceeded, Sink.LastWarningCode);
        }
    }
}
