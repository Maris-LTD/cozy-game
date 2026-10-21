using System;
using CozyGachapon.Contracts;
using CozyGachapon.Foundation;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Bootstrap
{
    public class StartupCurtainControllerTests
    {
        private static BootConfig CreateBootConfig(float minDisplay = 0.4f, float revealDuration = 0.35f, float maxBudget = 2.0f)
        {
            BootConfig config = ScriptableObject.CreateInstance<BootConfig>();
            config.SetCurtainTimingForTest(minDisplay, revealDuration, maxBudget);
            return config;
        }

        [Test]
        public void AC13_Curtain_WaitsForMinDisplay_BeforeRevealing_AndFlagsBudgetExceeded()
        {
            BootConfig config = CreateBootConfig(0.4f, 0.35f, 0.7f);
            InputBlockGate blockGate = new InputBlockGate();
            StartupCurtainController controller = new StartupCurtainController(config, blockGate);

            controller.NotifyBootCompleted(new BootCompletedEvent(0.1f));

            controller.Tick(0.2f);
            Assert.AreEqual(CurtainState.Covering, controller.State);

            controller.Tick(0.2f);
            Assert.AreEqual(CurtainState.Revealing, controller.State);

            controller.Tick(0.35f);
            Assert.AreEqual(CurtainState.Open, controller.State);
            Assert.IsTrue(controller.IsBudgetExceeded);
        }

        [Test]
        public void AC14_Curtain_HoldsInputBlockInCoveringAndRevealing_AndReleasesOnOpen()
        {
            BootConfig config = CreateBootConfig(0.4f, 0.35f, 2.0f);
            InputBlockGate blockGate = new InputBlockGate();
            StartupCurtainController controller = new StartupCurtainController(config, blockGate);

            Assert.AreEqual(CurtainState.Covering, controller.State);
            Assert.IsTrue(blockGate.IsInputBlocked);

            controller.NotifyBootCompleted(new BootCompletedEvent(0.1f));
            controller.Tick(0.4f);
            Assert.AreEqual(CurtainState.Revealing, controller.State);
            Assert.IsTrue(blockGate.IsInputBlocked);

            controller.Tick(0.35f);
            Assert.AreEqual(CurtainState.Open, controller.State);
            Assert.IsFalse(blockGate.IsInputBlocked);
            Assert.IsTrue(controller.IsSimulationRunning);
        }

        [Test]
        public void AC38_Curtain_FreezesProgressAndHoldsToken_WhenApplicationHidden_AndResumesOnVisible()
        {
            BootConfig config = CreateBootConfig(0.2f, 0.4f, 2.0f);
            InputBlockGate blockGate = new InputBlockGate();
            StartupCurtainController controller = new StartupCurtainController(config, blockGate);

            controller.NotifyBootCompleted(new BootCompletedEvent(0.1f));
            controller.Tick(0.2f);
            Assert.AreEqual(CurtainState.Revealing, controller.State);

            controller.Tick(0.2f);
            float frozenProgress = controller.RevealProgress;
            Assert.AreEqual(0.5f, frozenProgress, 0.01f);

            controller.SetVisibility(false);
            Assert.AreEqual(CurtainState.RevealFrozen, controller.State);

            controller.Tick(0.5f);
            Assert.AreEqual(CurtainState.RevealFrozen, controller.State);
            Assert.AreEqual(frozenProgress, controller.RevealProgress, 0.01f);
            Assert.IsTrue(blockGate.IsInputBlocked);

            controller.SetVisibility(true);
            Assert.AreEqual(CurtainState.Revealing, controller.State);

            controller.Tick(0.2f);
            Assert.AreEqual(CurtainState.Open, controller.State);
            Assert.IsFalse(blockGate.IsInputBlocked);
        }

        [Test]
        public void AC40_SimulationRunning_IsFalseUntilOpen_AndInteractiveFiredOnce()
        {
            BootConfig config = CreateBootConfig(0.2f, 0.2f, 2.0f);
            InputBlockGate blockGate = new InputBlockGate();
            StartupCurtainController controller = new StartupCurtainController(config, blockGate);

            int interactiveFiredCount = 0;
            controller.OnInteractive += _ => interactiveFiredCount++;

            Assert.IsFalse(controller.IsSimulationRunning);

            controller.NotifyBootCompleted(new BootCompletedEvent(0.1f));
            controller.Tick(0.2f);
            Assert.IsFalse(controller.IsSimulationRunning);
            Assert.AreEqual(0, interactiveFiredCount);

            controller.Tick(0.2f);
            Assert.AreEqual(CurtainState.Open, controller.State);
            Assert.IsTrue(controller.IsSimulationRunning);
            Assert.IsTrue(controller.IsInteractive);
            Assert.AreEqual(1, interactiveFiredCount);

            controller.Tick(0.2f);
            Assert.AreEqual(1, interactiveFiredCount);
        }

        [Test]
        public void AC40_BootFailed_NeverFiresInteractive_AndKeepsSimulationStopped()
        {
            BootConfig config = CreateBootConfig(0.2f, 0.2f, 2.0f);
            InputBlockGate blockGate = new InputBlockGate();
            StartupCurtainController controller = new StartupCurtainController(config, blockGate);

            int interactiveFiredCount = 0;
            controller.OnInteractive += _ => interactiveFiredCount++;

            controller.NotifyBootFailed(new BootFailedEvent("step_x", BootPhase.DataLoad, "Crash"));
            Assert.AreEqual(CurtainState.ErrorShown, controller.State);

            controller.Tick(1.0f);
            Assert.AreEqual(0, interactiveFiredCount);
            Assert.IsFalse(controller.IsSimulationRunning);
            Assert.IsFalse(controller.IsInteractive);
            Assert.IsTrue(blockGate.IsInputBlocked);
        }
    }
}
