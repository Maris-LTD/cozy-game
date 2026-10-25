using CozyGachapon.Contracts;
using NUnit.Framework;

namespace CozyGachapon.Tests.Bootstrap
{
    public class WelcomeQueueTests : WelcomeQueueTestBase
    {
        [Test]
        public void Begin_EmptyQueue_CompletesOnceInSameCall()
        {
            _queue.Begin();

            Assert.AreEqual(1, _completedCount);
            Assert.AreEqual(0, _gate.ActiveTokenCount);
            Assert.AreEqual(WelcomeQueueState.Done, _queue.State);
            Assert.AreEqual(0, _lastEvent.ShownStepCount);
        }

        [Test]
        public void Begin_CalledTwiceOnEmptyQueue_StillOneEvent()
        {
            _queue.Begin();
            _queue.Begin();

            Assert.AreEqual(1, _completedCount);
            Assert.AreEqual(0, _gate.ActiveTokenCount);
        }

        [Test]
        public void Begin_WithOrderBA_ShowsBThenCSkippingA()
        {
            SetOrder("b", "a");
            FakeWelcomeStep a = AddStep("a", false);
            FakeWelcomeStep b = AddStep("b");
            FakeWelcomeStep c = AddStep("c");

            _queue.Begin();
            b.PendingDone();
            c.PendingDone();

            CollectionAssert.AreEqual(new[] { "b", "c" }, _showLog);
            Assert.AreEqual(0, a.ShowCount);
        }

        [Test]
        public void StepShowing_HoldsExactlyOneToken_ReleasedOnDone()
        {
            SetOrder("b", "a");
            AddStep("a", false);
            FakeWelcomeStep b = AddStep("b");
            FakeWelcomeStep c = AddStep("c");

            _queue.Begin();
            Assert.AreEqual(1, _gate.ActiveTokenCount);
            b.PendingDone();
            Assert.AreEqual(1, _gate.ActiveTokenCount);
            c.PendingDone();
            Assert.AreEqual(0, _gate.ActiveTokenCount);
        }

        [Test]
        public void OnlyOneStepVisible_UntilOnDone()
        {
            SetOrder("b", "a");
            AddStep("a", false);
            FakeWelcomeStep b = AddStep("b");
            FakeWelcomeStep c = AddStep("c");

            _queue.Begin();

            Assert.AreEqual(1, b.ShowCount);
            Assert.AreEqual(0, c.ShowCount);
            b.PendingDone();
            Assert.AreEqual(1, c.ShowCount);
        }

        [Test]
        public void Completed_FiresOnlyAfterLastOnDone()
        {
            SetOrder("b", "a");
            AddStep("a", false);
            FakeWelcomeStep b = AddStep("b");
            FakeWelcomeStep c = AddStep("c");

            _queue.Begin();
            Assert.AreEqual(0, _completedCount);
            b.PendingDone();
            Assert.AreEqual(0, _completedCount);
            c.PendingDone();
            Assert.AreEqual(1, _completedCount);
            Assert.AreEqual(2, _lastEvent.ShownStepCount);
        }

        [Test]
        public void Register_AfterBegin_ReturnsFalse()
        {
            _queue.Begin();

            Assert.IsFalse(_queue.Register(new FakeWelcomeStep("late", _showLog)));
        }

        [Test]
        public void State_TransitionsWaitingRunningDone()
        {
            FakeWelcomeStep a = AddStep("a");

            Assert.AreEqual(WelcomeQueueState.Waiting, _queue.State);
            _queue.Begin();
            Assert.AreEqual(WelcomeQueueState.Running, _queue.State);
            a.PendingDone();
            Assert.AreEqual(WelcomeQueueState.Done, _queue.State);
        }
    }
}
