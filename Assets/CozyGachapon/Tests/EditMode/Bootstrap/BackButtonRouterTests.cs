using System.Collections.Generic;
using CozyGachapon.Contracts;
using CozyGachapon.Foundation;
using NUnit.Framework;

namespace CozyGachapon.Tests.Bootstrap
{
    public class BackButtonRouterTests
    {
        private TabRouter _tabRouter;
        private FakeBootStatus _bootStatus;
        private FakeBackgroundRequester _requester;
        private List<string> _callLog;

        [SetUp]
        public void SetUp()
        {
            _tabRouter = new TabRouter(AppTab.Board);
            _bootStatus = new FakeBootStatus();
            _requester = new FakeBackgroundRequester();
            _callLog = new List<string>();
        }

        private BackButtonRouter CreateRouter(HostPlatform platform)
        {
            return new BackButtonRouter(_tabRouter, _bootStatus, _requester, platform);
        }

        [Test]
        public void BackWithTwoModals_CallsTopHandlerOnly()
        {
            BackButtonRouter router = CreateRouter(HostPlatform.Android);
            FakeModalBackHandler lower = new FakeModalBackHandler("lower", _callLog);
            FakeModalBackHandler upper = new FakeModalBackHandler("upper", _callLog);
            router.PushBackHandler(lower);
            router.PushBackHandler(upper);

            BackPressResult first = router.HandleBack();
            router.PopBackHandler(upper);
            BackPressResult second = router.HandleBack();

            Assert.AreEqual(BackPressResult.HandledByModal, first);
            Assert.AreEqual(BackPressResult.HandledByModal, second);
            CollectionAssert.AreEqual(new[] { "upper", "lower" }, _callLog);
            Assert.AreEqual(0, _requester.RequestCount);
        }

        [Test]
        public void PopBackHandlerNotOnTop_RemovesItAndBackSkipsClosedModal()
        {
            BackButtonRouter router = CreateRouter(HostPlatform.Android);
            FakeModalBackHandler lower = new FakeModalBackHandler("lower", _callLog);
            FakeModalBackHandler upper = new FakeModalBackHandler("upper", _callLog);
            router.PushBackHandler(lower);
            router.PushBackHandler(upper);

            bool popped = router.PopBackHandler(lower);
            router.PopBackHandler(upper);
            BackPressResult result = router.HandleBack();

            Assert.IsTrue(popped);
            Assert.AreEqual(0, router.HandlerCount);
            Assert.AreEqual(BackPressResult.MovedToBackground, result);
            CollectionAssert.IsEmpty(_callLog);
        }

        [Test]
        public void PopBackHandlerNotRegistered_ReturnsFalse()
        {
            BackButtonRouter router = CreateRouter(HostPlatform.Android);
            FakeModalBackHandler pushed = new FakeModalBackHandler("pushed", _callLog);
            FakeModalBackHandler stranger = new FakeModalBackHandler("stranger", _callLog);
            router.PushBackHandler(pushed);

            bool popped = router.PopBackHandler(stranger);

            Assert.IsFalse(popped);
            Assert.AreEqual(1, router.HandlerCount);
        }

        [Test]
        public void BackWithEmptyStackOnShelf_RequestsBoard()
        {
            _tabRouter.RequestTab(AppTab.Shelf);
            BackButtonRouter router = CreateRouter(HostPlatform.Android);

            BackPressResult result = router.HandleBack();

            Assert.AreEqual(BackPressResult.ReturnedToBoard, result);
            Assert.AreEqual(AppTab.Board, _tabRouter.ActiveTab);
            Assert.AreEqual(0, _requester.RequestCount);
        }

        [Test]
        public void BackWithEmptyStackOnShop_RequestsBoard()
        {
            _tabRouter.RequestTab(AppTab.Shop);
            BackButtonRouter router = CreateRouter(HostPlatform.WebGL);

            BackPressResult result = router.HandleBack();

            Assert.AreEqual(BackPressResult.ReturnedToBoard, result);
            Assert.AreEqual(AppTab.Board, _tabRouter.ActiveTab);
        }

        [Test]
        public void BackOnShelfWithTabLock_ReturnsTabLocked()
        {
            _tabRouter.RequestTab(AppTab.Shelf);
            _tabRouter.AcquireTabLock("test");
            BackButtonRouter router = CreateRouter(HostPlatform.Android);

            BackPressResult result = router.HandleBack();

            Assert.AreEqual(BackPressResult.TabLocked, result);
            Assert.AreEqual(AppTab.Shelf, _tabRouter.ActiveTab);
            Assert.AreEqual(0, _requester.RequestCount);
        }

        [Test]
        public void BackOnBoardAndroid_RequestsMoveToBackground()
        {
            BackButtonRouter router = CreateRouter(HostPlatform.Android);

            BackPressResult result = router.HandleBack();

            Assert.AreEqual(BackPressResult.MovedToBackground, result);
            Assert.AreEqual(1, _requester.RequestCount);
        }

        [Test]
        public void BackOnBoardWebGL_DoesNothing()
        {
            BackButtonRouter router = CreateRouter(HostPlatform.WebGL);

            BackPressResult result = router.HandleBack();

            Assert.AreEqual(BackPressResult.NoAction, result);
            Assert.AreEqual(0, _requester.RequestCount);
        }

        [Test]
        public void BackWhileBootingSteps_Ignored()
        {
            AppLifecycleWatchdog watchdog = new AppLifecycleWatchdog();
            BootStatusTracker tracker = new BootStatusTracker(watchdog);
            BackButtonRouter router = new BackButtonRouter(_tabRouter, tracker, _requester, HostPlatform.Android);

            BackPressResult result = router.HandleBack();

            Assert.AreEqual(BackPressResult.IgnoredWhileBooting, result);
            Assert.AreEqual(0, _requester.RequestCount);
        }

        [Test]
        public void BackWhileBootingCurtainWithModal_IgnoredAndHandlerNotCalled()
        {
            AppLifecycleWatchdog watchdog = new AppLifecycleWatchdog();
            watchdog.NotifyBootCompleted();
            BootStatusTracker tracker = new BootStatusTracker(watchdog);
            BackButtonRouter router = new BackButtonRouter(_tabRouter, tracker, _requester, HostPlatform.Android);
            FakeModalBackHandler handler = new FakeModalBackHandler("modal", _callLog);
            router.PushBackHandler(handler);

            BackPressResult result = router.HandleBack();

            Assert.AreEqual(BackPressResult.IgnoredWhileBooting, result);
            Assert.AreEqual(0, handler.CallCount);
        }

        [Test]
        public void BackAfterBootFailedOnAndroid_RequestsMoveToBackground()
        {
            AppLifecycleWatchdog watchdog = new AppLifecycleWatchdog();
            BootStatusTracker tracker = new BootStatusTracker(watchdog);
            tracker.NotifyBootFailed(new BootFailedEvent("step", BootPhase.ServiceInit, "reason"));
            BackButtonRouter router = new BackButtonRouter(_tabRouter, tracker, _requester, HostPlatform.Android);

            BackPressResult result = router.HandleBack();

            Assert.AreEqual(BackPressResult.MovedToBackground, result);
            Assert.AreEqual(1, _requester.RequestCount);
        }

        [Test]
        public void BackWhileBootingCurtainOnShelfWithoutModal_IgnoredAndTabKept()
        {
            _tabRouter.RequestTab(AppTab.Shelf);
            _bootStatus.IsBooting = true;
            BackButtonRouter router = CreateRouter(HostPlatform.Android);

            BackPressResult result = router.HandleBack();

            Assert.AreEqual(BackPressResult.IgnoredWhileBooting, result);
            Assert.AreEqual(AppTab.Shelf, _tabRouter.ActiveTab);
            Assert.AreEqual(0, _requester.RequestCount);
        }

        [Test]
        public void BackAfterBootFailedOnAndroidWithModal_MovesToBackgroundWithoutCallingModal()
        {
            _bootStatus.IsBootFailed = true;
            BackButtonRouter router = CreateRouter(HostPlatform.Android);
            FakeModalBackHandler handler = new FakeModalBackHandler("modal", _callLog);
            router.PushBackHandler(handler);

            BackPressResult result = router.HandleBack();

            Assert.AreEqual(BackPressResult.MovedToBackground, result);
            Assert.AreEqual(0, handler.CallCount);
            Assert.AreEqual(1, _requester.RequestCount);
        }

        [Test]
        public void BackAfterBootFailedOnWebGL_DoesNothing()
        {
            _bootStatus.IsBootFailed = true;
            BackButtonRouter router = CreateRouter(HostPlatform.WebGL);

            BackPressResult result = router.HandleBack();

            Assert.AreEqual(BackPressResult.NoAction, result);
            Assert.AreEqual(0, _requester.RequestCount);
        }

        [Test]
        public void BootStatusTracker_ReflectsWatchdogStates()
        {
            AppLifecycleWatchdog watchdog = new AppLifecycleWatchdog();
            BootStatusTracker tracker = new BootStatusTracker(watchdog);

            Assert.IsTrue(tracker.IsBooting);

            watchdog.NotifyBootCompleted();
            Assert.IsTrue(tracker.IsBooting);

            watchdog.NotifyInteractive();
            Assert.IsFalse(tracker.IsBooting);
            Assert.IsFalse(tracker.IsBootFailed);
        }
    }
}
