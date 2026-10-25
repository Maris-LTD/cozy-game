using System;
using System.Collections.Generic;
using CozyGachapon.Contracts;
using CozyGachapon.Foundation;
using CozyGachapon.Tests.Pooling;
using NUnit.Framework;

namespace CozyGachapon.Tests.Bootstrap
{
    public class InputBarrierTabRouterTests
    {
        [Test]
        public void AC25_InputBarrier_TokenAcquisitionAndReleaseLifecycle()
        {
            InputBarrier barrier = new InputBarrier();

            Assert.AreEqual(InputGateStatus.Open, barrier.Status);
            Assert.IsFalse(barrier.IsInputBlocked);

            int tokenX = barrier.AcquireInputBlock("SystemX");
            Assert.AreEqual(InputGateStatus.Blocked, barrier.Status);
            Assert.IsTrue(barrier.IsInputBlocked);

            int tokenY = barrier.AcquireInputBlock("SystemY");
            Assert.AreEqual(InputGateStatus.Blocked, barrier.Status);
            Assert.IsTrue(barrier.IsInputBlocked);

            barrier.Release(tokenX);
            Assert.AreEqual(InputGateStatus.Blocked, barrier.Status);
            Assert.IsTrue(barrier.IsInputBlocked);

            barrier.Release(tokenY);
            Assert.AreEqual(InputGateStatus.Open, barrier.Status);
            Assert.IsFalse(barrier.IsInputBlocked);

            Assert.DoesNotThrow(() => barrier.Release(tokenX));
            Assert.AreEqual(InputGateStatus.Open, barrier.Status);

            Assert.DoesNotThrow(() => barrier.Release(9999));
            Assert.DoesNotThrow(() => barrier.Release(-1));
            Assert.AreEqual(InputGateStatus.Open, barrier.Status);
        }

        [Test]
        public void AC27_RequestTab_SameActiveTab_ReturnsNoChange()
        {
            TabRouter router = new TabRouter(AppTab.Board);
            int eventCount = 0;
            router.OnTabChanged += _ => eventCount++;

            RequestTabResult result = router.RequestTab(AppTab.Board);

            Assert.AreEqual(RequestTabResult.NoChange, result);
            Assert.AreEqual(0, eventCount);
            Assert.AreEqual(AppTab.Board, router.ActiveTab);
        }

        [Test]
        public void AC27_RequestTab_WhenSingleLockExists_WithoutToken_ReturnsLocked()
        {
            TabRouter router = new TabRouter(AppTab.Board);
            int eventCount = 0;
            router.OnTabChanged += _ => eventCount++;

            int token = router.AcquireTabLock("TestLock");

            RequestTabResult result = router.RequestTab(AppTab.Shop);

            Assert.AreEqual(RequestTabResult.Locked, result);
            Assert.AreEqual(0, eventCount);
            Assert.AreEqual(AppTab.Board, router.ActiveTab);
        }

        [Test]
        public void AC27_RequestTab_WhenSingleLockExists_WithMatchingToken_ReturnsChanged()
        {
            TabRouter router = new TabRouter(AppTab.Board);
            int eventCount = 0;
            TabChangedEvent? lastEvent = null;

            router.OnTabChanged += evt =>
            {
                eventCount++;
                lastEvent = evt;
            };

            int token = router.AcquireTabLock("TestLock");

            RequestTabResult result = router.RequestTab(AppTab.Shop, token);

            Assert.AreEqual(RequestTabResult.Changed, result);
            Assert.AreEqual(1, eventCount);
            Assert.IsNotNull(lastEvent);
            Assert.AreEqual(AppTab.Board, lastEvent.Value.Previous);
            Assert.AreEqual(AppTab.Shop, lastEvent.Value.Current);
            Assert.AreEqual(AppTab.Shop, router.ActiveTab);
        }

        [Test]
        public void AC27_RequestTab_WhenMultipleLocksExist_WithSingleToken_ReturnsLocked()
        {
            TabRouter router = new TabRouter(AppTab.Board);
            int eventCount = 0;
            router.OnTabChanged += _ => eventCount++;

            int tokenA = router.AcquireTabLock("LockA");
            int tokenB = router.AcquireTabLock("LockB");

            RequestTabResult result = router.RequestTab(AppTab.Shelf, tokenA);

            Assert.AreEqual(RequestTabResult.Locked, result);
            Assert.AreEqual(0, eventCount);
            Assert.AreEqual(AppTab.Board, router.ActiveTab);
        }

        [Test]
        public void AC27_RequestTab_DuringBooting_ReturnsLocked()
        {
            TabRouter router = new TabRouter(AppTab.Board, isBooting: true);
            int eventCount = 0;
            router.OnTabChanged += _ => eventCount++;

            RequestTabResult result = router.RequestTab(AppTab.Shelf);

            Assert.AreEqual(RequestTabResult.Locked, result);
            Assert.AreEqual(0, eventCount);
            Assert.AreEqual(AppTab.Board, router.ActiveTab);
        }

        [Test]
        public void AC27_ReleaseTabLock_RestoresUnrestrictedTabSwitching()
        {
            TabRouter router = new TabRouter(AppTab.Board);
            int token = router.AcquireTabLock("Lock");

            Assert.AreEqual(RequestTabResult.Locked, router.RequestTab(AppTab.Shelf));

            router.ReleaseTabLock(token);

            Assert.AreEqual(RequestTabResult.Changed, router.RequestTab(AppTab.Shelf));
            Assert.AreEqual(AppTab.Shelf, router.ActiveTab);
        }

        [Test]
        public void AC27_LockTab_ReturnsTokenAndLocksTabRouter()
        {
            ITabRouter router = new TabRouter(AppTab.Board);
            bool success = router.LockTab(AppTab.Shelf, out int token);

            Assert.IsTrue(success);
            Assert.Greater(token, 0);
            Assert.AreEqual(RequestTabResult.Locked, router.RequestTab(AppTab.Shelf));

            router.ReleaseTabLock(token);
            Assert.AreEqual(RequestTabResult.Changed, router.RequestTab(AppTab.Shelf));
        }

        [Test]
        public void AC37_ZeroAllocation_During600FramesOfTokensAndTabSwitching()
        {
            InputBarrier barrier = new InputBarrier();
            ITabRouter router = new TabRouter(AppTab.Board);

            int warmupToken = barrier.AcquireInputBlock("Warmup");
            barrier.Release(warmupToken);
            router.LockTab(AppTab.Board, out int warmupLock);
            router.ReleaseTabLock(warmupLock);
            router.RequestTab(AppTab.Shelf);
            router.RequestTab(AppTab.Board);

            _barrier = barrier;
            _router = router;
            RunSixHundredFrames();

            int allocations = GcAllocProbe.CountAllocations(RunSixHundredFrames);

            Assert.AreEqual(0, allocations);
        }

        private InputBarrier _barrier;
        private ITabRouter _router;

        private void RunSixHundredFrames()
        {
            for (int i = 0; i < 600; i++)
            {
                int token = _barrier.AcquireInputBlock("FrameLoop");
                _barrier.Release(token);

                _router.LockTab(AppTab.Board, out int lockToken);
                _router.ReleaseTabLock(lockToken);

                _router.RequestTab(AppTab.Shelf);
                _router.RequestTab(AppTab.Shop);
                _router.RequestTab(AppTab.Board);
            }
        }
    }
}
