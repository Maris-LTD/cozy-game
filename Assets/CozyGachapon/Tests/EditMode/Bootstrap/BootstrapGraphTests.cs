using CozyGachapon.Contracts;
using CozyGachapon.Foundation;
using NUnit.Framework;

namespace CozyGachapon.Tests.Bootstrap
{
    public class BootstrapGraphTests : BootstrapGraphTestBase
    {
        private const float TimeTolerance = 0.0001f;
        private const string PetCatalogStepId = "pet.catalog";

        [Test]
        public void Wire_DefaultConfigsNoSteps_BootCompletedOnFirstTick()
        {
            GraphEventRecorder recorder = new GraphEventRecorder();
            BootstrapGraph graph = CreateGraph(recorder);

            TickOnce(graph, recorder);

            Assert.AreEqual(1, recorder.Count("BootCompleted"));
            Assert.AreEqual(1, recorder.FirstTick("BootCompleted"));
            Assert.AreEqual(1, recorder.Count("FrameChanged"));
            Assert.AreEqual(AppLifecycleState.BootingCurtain, graph.Lifecycle.State);
            Assert.AreEqual(CurtainState.Covering, graph.Curtain.State);
            Assert.AreEqual(0, _presenter.CallCount);
        }

        [Test]
        public void Wire_CurtainRevealsAtMinDisplay_OpensAfterRevealDuration()
        {
            GraphEventRecorder recorder = new GraphEventRecorder();
            BootstrapGraph graph = CreateGraph(recorder);
            int revealingTick = -1;
            int openTick = -1;

            int totalTicks = UnityEngine.Mathf.CeilToInt(1f / Dt);
            for (int i = 0; i < totalTicks; i++)
            {
                TickOnce(graph, recorder);
                if (revealingTick < 0 && graph.Curtain.State == CurtainState.Revealing)
                {
                    revealingTick = _tick;
                }

                if (openTick < 0 && graph.Curtain.State == CurtainState.Open)
                {
                    openTick = _tick;
                }
            }

            float minDisplay = _bootConfig.CurtainMinDisplaySeconds;
            float openAt = minDisplay + _bootConfig.CurtainRevealDuration;
            float revealingTime = revealingTick * Dt;
            float openTime = openTick * Dt;

            Assert.GreaterOrEqual(revealingTime, minDisplay - TimeTolerance);
            Assert.LessOrEqual(revealingTime, minDisplay + Dt + TimeTolerance);
            Assert.GreaterOrEqual(openTime, openAt - TimeTolerance);
            Assert.LessOrEqual(openTime, openAt + 2f * Dt + TimeTolerance);
            Assert.AreEqual(1, recorder.Count("Interactive"));
            Assert.AreEqual(openTick, recorder.FirstTick("Interactive"));
            Assert.AreEqual(1, recorder.Count("WelcomeQueueCompleted"));
            Assert.AreEqual(recorder.FirstTick("Interactive"), recorder.FirstTick("WelcomeQueueCompleted"));
            Assert.AreEqual(0, graph.Gate.ActiveTokenCount);
            Assert.AreEqual(_sceneFlowConfig.DefaultTab, graph.Tabs.ActiveTab);
            Assert.AreEqual(AppLifecycleState.Running, graph.Lifecycle.State);
            Assert.IsFalse(graph.Tabs.IsBooting);
            Assert.AreEqual(WelcomeQueueState.Done, graph.Welcome.State);
        }

        [Test]
        public void Wire_TabsLockedWhileBooting()
        {
            BootstrapGraph graph = CreateGraph(null);

            TickOnce(graph, null);

            Assert.AreEqual(RequestTabResult.Locked, graph.Tabs.RequestTab(AppTab.Shelf));
            Assert.AreEqual(1, graph.Gate.ActiveTokenCount);
        }

        [Test]
        public void Wire_CriticalPetCatalogFails_ErrorShownAndPresenterGetsEventStepId()
        {
            GraphEventRecorder recorder = new GraphEventRecorder();
            BootstrapGraph graph = CreateGraph(recorder);
            FakeBootStep step = new FakeBootStep(PetCatalogStepId, BootPhase.DataLoad, true, true);
            Assert.IsTrue(graph.RegisterStep(step));

            TickOnce(graph, recorder);

            Assert.AreEqual(CurtainState.ErrorShown, graph.Curtain.State);
            Assert.AreEqual(1, _presenter.CallCount);
            Assert.AreEqual(PetCatalogStepId, _presenter.LastFailure.StepId);
            Assert.AreEqual(BootPhase.DataLoad, _presenter.LastFailure.Phase);

            TickFor(graph, recorder, 2f);

            Assert.AreEqual(1, step.RunCount);
            Assert.AreEqual(1, _presenter.CallCount);
            Assert.AreEqual(1, recorder.Count("BootFailed"));
            Assert.AreEqual(0, recorder.Count("Interactive"));
            Assert.AreEqual(0, recorder.Count("BootCompleted"));
            Assert.AreEqual(1, graph.Gate.ActiveTokenCount);
            Assert.IsTrue(graph.Status.IsBootFailed);
            Assert.AreEqual(CurtainState.ErrorShown, graph.Curtain.State);
        }

        [Test]
        public void Wire_StepRegisteredBeforeFirstTick_RunsBeforeBootCompleted()
        {
            GraphEventRecorder recorder = new GraphEventRecorder();
            BootstrapGraph graph = CreateGraph(recorder);
            FakeBootStep step = new FakeBootStep("save.load", BootPhase.DataLoad, true, false);
            graph.RegisterStep(step);

            TickOnce(graph, recorder);

            Assert.AreEqual(1, step.RunCount);
            Assert.AreEqual(1, recorder.Count("BootCompleted"));
            Assert.AreEqual(0, _presenter.CallCount);
        }
    }
}
