using System;
using System.Collections.Generic;
using CozyGachapon.Contracts;
using NUnit.Framework;

namespace CozyGachapon.Tests.Bootstrap
{
    public class WelcomeQueueEdgeTests : WelcomeQueueTestBase
    {
        [Test]
        public void Validate_UnnamedSteps_ReturnsCAndZ()
        {
            SetOrder("b");
            AddStep("z");
            AddStep("c");
            AddStep("b");

            IReadOnlyList<string> warnings = _queue.Validate();

            CollectionAssert.AreEqual(new[] { "c", "z" }, warnings);
        }

        [Test]
        public void Validate_AllNamed_ReturnsEmpty()
        {
            SetOrder("a");
            AddStep("a");

            Assert.AreEqual(0, _queue.Validate().Count);
        }

        [Test]
        public void Begin_UnnamedSteps_RunAfterNamedInOrdinalOrder()
        {
            SetOrder("b");
            FakeWelcomeStep z = AddStep("z");
            FakeWelcomeStep c = AddStep("c");
            FakeWelcomeStep b = AddStep("b");

            _queue.Begin();
            b.PendingDone();
            c.PendingDone();
            z.PendingDone();

            CollectionAssert.AreEqual(new[] { "b", "c", "z" }, _showLog);
            Assert.AreEqual(1, _completedCount);
        }

        [Test]
        public void OnDone_CalledTwiceForB_DoesNotSkipC()
        {
            SetOrder("b");
            FakeWelcomeStep z = AddStep("z");
            FakeWelcomeStep c = AddStep("c");
            FakeWelcomeStep b = AddStep("b");

            _queue.Begin();
            Action firstDone = b.PendingDone;
            firstDone();
            firstDone();

            Assert.AreEqual(1, c.ShowCount);
            Assert.AreEqual(0, z.ShowCount);
            Assert.AreEqual(1, _gate.ActiveTokenCount);
        }

        [Test]
        public void OnDone_CalledTwiceForLastStep_NoExtraEventOrRelease()
        {
            FakeWelcomeStep a = AddStep("a");

            _queue.Begin();
            Action done = a.PendingDone;
            done();
            done();

            Assert.AreEqual(1, _completedCount);
            Assert.AreEqual(0, _gate.ActiveTokenCount);
        }

        [Test]
        public void Begin_CalledTwiceWhileRunning_NoExtraEventOrToken()
        {
            FakeWelcomeStep a = AddStep("a");

            _queue.Begin();
            _queue.Begin();

            Assert.AreEqual(1, a.ShowCount);
            Assert.AreEqual(1, _gate.ActiveTokenCount);
            Assert.AreEqual(0, _completedCount);
        }

        [Test]
        public void Show_CallsOnDoneSynchronously_AdvancesAndCompletes()
        {
            FakeWelcomeStep a = AddStep("a");
            FakeWelcomeStep b = AddStep("b");
            a.CompleteImmediately = true;
            b.CompleteImmediately = true;

            _queue.Begin();

            CollectionAssert.AreEqual(new[] { "a", "b" }, _showLog);
            Assert.AreEqual(0, _gate.ActiveTokenCount);
            Assert.AreEqual(1, _completedCount);
            Assert.AreEqual(2, _lastEvent.ShownStepCount);
        }

        [Test]
        public void Register_NullOrDuplicateId_ReturnsFalse()
        {
            AddStep("a");

            Assert.IsFalse(_queue.Register(null));
            Assert.IsFalse(_queue.Register(new FakeWelcomeStep("a", _showLog)));
            Assert.IsFalse(_queue.Register(new FakeWelcomeStep("", _showLog)));
        }

        [Test]
        public void WelcomeOrder_NameNotRegistered_Ignored()
        {
            SetOrder("ghost", "a");
            FakeWelcomeStep a = AddStep("a");

            _queue.Begin();
            a.PendingDone();

            CollectionAssert.AreEqual(new[] { "a" }, _showLog);
            Assert.AreEqual(1, _completedCount);
        }
    }
}
