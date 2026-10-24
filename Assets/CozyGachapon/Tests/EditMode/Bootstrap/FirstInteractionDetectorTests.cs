using CozyGachapon.Contracts;
using CozyGachapon.Foundation;
using NUnit.Framework;

namespace CozyGachapon.Tests.Bootstrap
{
    public class FirstInteractionDetectorTests
    {
        private FirstInteractionDetector _detector;
        private int _fireCount;

        [SetUp]
        public void SetUp()
        {
            _detector = new FirstInteractionDetector();
            _fireCount = 0;
            _detector.OnFirstInteraction += _ => _fireCount++;
        }

        private WorldPressRouter CreateRouter(InputBlockGate gate, FakeUiHitTester uiHitTester, FakeFrameRect frameRect)
        {
            return new WorldPressRouter(gate, new TabRouter(AppTab.Board), uiHitTester, frameRect);
        }

        private void Press(WorldPressRouter router, int frame, float x, float y)
        {
            _detector.NotifyUserGesture();
            router.RoutePress(frame, x, y, 0);
        }

        [Test]
        public void FirstGesture_FiresExactlyOnce()
        {
            bool fired = _detector.NotifyUserGesture();

            Assert.IsTrue(fired);
            Assert.IsTrue(_detector.HasFired);
            Assert.AreEqual(1, _fireCount);
        }

        [Test]
        public void SubsequentGestures_DoNotFireAgain()
        {
            _detector.NotifyUserGesture();

            bool second = _detector.NotifyUserGesture();
            bool third = _detector.NotifyUserGesture();

            Assert.IsFalse(second);
            Assert.IsFalse(third);
            Assert.AreEqual(1, _fireCount);
        }

        [Test]
        public void GestureWhileGateBlocked_StillFires()
        {
            InputBlockGate gate = new InputBlockGate();
            gate.AcquireInputBlock("curtain");
            WorldPressRouter router = CreateRouter(gate, new FakeUiHitTester(), new FakeFrameRect(1000f, 1000f));

            Press(router, 1, 100f, 100f);
            Press(router, 2, 100f, 100f);

            Assert.AreEqual(1, _fireCount);
        }

        [Test]
        public void GestureOnUi_StillFires()
        {
            FakeUiHitTester uiHitTester = new FakeUiHitTester { Hits = true };
            WorldPressRouter router = CreateRouter(new InputBlockGate(), uiHitTester, new FakeFrameRect(1000f, 1000f));

            Press(router, 1, 100f, 100f);

            Assert.AreEqual(1, _fireCount);
        }

        [Test]
        public void GestureOutsideFrame_StillFires()
        {
            WorldPressRouter router = CreateRouter(new InputBlockGate(), new FakeUiHitTester(), new FakeFrameRect(1000f, 1000f));

            Press(router, 1, 5000f, 5000f);
            Press(router, 2, 100f, 100f);

            Assert.AreEqual(1, _fireCount);
        }

        [Test]
        public void GestureWhileBooting_StillFires()
        {
            AppLifecycleWatchdog watchdog = new AppLifecycleWatchdog();
            BootStatusTracker tracker = new BootStatusTracker(watchdog);
            BackButtonRouter backRouter = new BackButtonRouter(new TabRouter(AppTab.Board), tracker, new FakeBackgroundRequester(), HostPlatform.Android);

            _detector.NotifyUserGesture();
            BackPressResult result = backRouter.HandleBack();

            Assert.AreEqual(BackPressResult.IgnoredWhileBooting, result);
            Assert.AreEqual(1, _fireCount);
        }

        [Test]
        public void KeyGestureFirst_FiresAndLaterPressDoesNot()
        {
            WorldPressRouter router = CreateRouter(new InputBlockGate(), new FakeUiHitTester(), null);

            bool keyFired = _detector.NotifyUserGesture();
            Press(router, 1, 100f, 100f);

            Assert.IsTrue(keyFired);
            Assert.AreEqual(1, _fireCount);
        }
    }
}
