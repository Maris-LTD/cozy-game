using System.Collections.Generic;
using CozyGachapon.Contracts;
using CozyGachapon.Foundation;
using NUnit.Framework;

namespace CozyGachapon.Tests.Bootstrap
{
    public class BootstrapGraphEventTests : BootstrapGraphTestBase
    {
        private static readonly HashSet<string> AllowedEvents = new HashSet<string>
        {
            "BootCompleted",
            "Interactive",
            "BootFailed",
            "FirstInteraction",
            "Paused",
            "Resumed",
            "TabChanged",
            "FrameChanged",
            "WelcomeQueueCompleted",
            "Quitting"
        };

        [Test]
        public void AC35_FullSession_EmitsOnlyWhitelistedEvents_AndNoAdRequests()
        {
            GraphEventRecorder recorder = new GraphEventRecorder();
            BootstrapGraph graph = CreateGraph(recorder);

            TickFor(graph, recorder, 1f);
            Assert.AreEqual(AppLifecycleState.Running, graph.Lifecycle.State);

            graph.FirstInteraction.NotifyUserGesture();

            for (int i = 0; i < 3; i++)
            {
                graph.SetPageVisible(false, PauseReason.Background);
                _clock.Advance(5.0);
                graph.SetPageVisible(true, PauseReason.Background);
                TickOnce(graph, recorder);
            }

            for (int i = 0; i < 10; i++)
            {
                AppTab target = i % 2 == 0 ? AppTab.Shelf : AppTab.Board;
                Assert.AreEqual(RequestTabResult.Changed, graph.Tabs.RequestTab(target));
            }

            graph.SetScreenSize(1440, 2560);
            TickOnce(graph, recorder);
            graph.SetWindowFocus(false);
            graph.SetWindowFocus(true);
            graph.NotifyQuit();
            graph.NotifyQuit();

            Assert.AreEqual(1, recorder.Count("BootCompleted"));
            Assert.AreEqual(1, recorder.Count("Interactive"));
            Assert.AreEqual(1, recorder.Count("WelcomeQueueCompleted"));
            Assert.AreEqual(1, recorder.Count("FirstInteraction"));
            Assert.AreEqual(3, recorder.Count("Paused"));
            Assert.AreEqual(3, recorder.Count("Resumed"));
            Assert.AreEqual(10, recorder.Count("TabChanged"));
            Assert.AreEqual(2, recorder.Count("FrameChanged"));
            Assert.AreEqual(1, recorder.Count("Quitting"));
            Assert.AreEqual(0, recorder.Count("AdClosed"));
            AssertOnlyAllowed(recorder);
            Assert.AreEqual(0, _platform.UnexpectedCallCount);
            Assert.AreEqual(0, _platform.BackgroundRequestCount);
        }

        [Test]
        public void AC35_CriticalStepFails_EmitsSingleBootFailed_AndNoAdRequests()
        {
            GraphEventRecorder recorder = new GraphEventRecorder();
            BootstrapGraph graph = CreateGraph(recorder);
            graph.RegisterStep(new FakeBootStep("pet.catalog", BootPhase.DataLoad, true, true));

            TickFor(graph, recorder, 1f);

            Assert.AreEqual(1, recorder.Count("BootFailed"));
            Assert.AreEqual(0, recorder.Count("BootCompleted"));
            AssertOnlyAllowed(recorder);
            Assert.AreEqual(0, _platform.UnexpectedCallCount);
            Assert.AreEqual(0, _platform.BackgroundRequestCount);
        }

        [Test]
        public void Lifecycle_HiddenDuringReveal_FreezesCurtain_AndResumesOnVisible()
        {
            BootstrapGraph graph = CreateGraph(null);
            TickFor(graph, null, _bootConfig.CurtainMinDisplaySeconds + Dt);
            Assert.AreEqual(CurtainState.Revealing, graph.Curtain.State);

            graph.SetPageVisible(false, PauseReason.Hidden);
            Assert.AreEqual(CurtainState.RevealFrozen, graph.Curtain.State);
            Assert.AreEqual(AppLifecycleState.Paused, graph.Lifecycle.State);

            graph.SetPageVisible(true, PauseReason.Hidden);
            Assert.AreEqual(CurtainState.Revealing, graph.Curtain.State);

            TickFor(graph, null, 1f);
            Assert.AreEqual(CurtainState.Open, graph.Curtain.State);
            Assert.AreEqual(AppLifecycleState.Running, graph.Lifecycle.State);
        }

        private static void AssertOnlyAllowed(GraphEventRecorder recorder)
        {
            foreach (string name in recorder.ObservedNames)
            {
                Assert.IsTrue(AllowedEvents.Contains(name), name);
            }
        }
    }
}
