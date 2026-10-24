using CozyGachapon.Contracts;
using CozyGachapon.Foundation;
using NUnit.Framework;

namespace CozyGachapon.Tests.Bootstrap
{
    public class WorldPressRouterTests
    {
        private InputBlockGate _gate;
        private TabRouter _tabRouter;
        private FakeUiHitTester _uiHitTester;
        private FakeWorldViewport _viewport;
        private WorldPressRouter _router;

        [SetUp]
        public void SetUp()
        {
            _gate = new InputBlockGate();
            _tabRouter = new TabRouter(AppTab.Board);
            _uiHitTester = new FakeUiHitTester();
            _viewport = new FakeWorldViewport(0f, 0f, 500f, 500f, 10f, -20f);
            _router = new WorldPressRouter(_gate, _tabRouter, _uiHitTester, new FakeFrameRect(1000f, 1000f));
            _router.RegisterViewport(AppTab.Board, _viewport);
        }

        [Test]
        public void FirstPressOnBoardInsideViewport_DeliversOneWorldPress()
        {
            WorldPressResult result = _router.RoutePress(1, 100f, 200f, 0);

            Assert.AreEqual(WorldPressResult.Routed, result);
            Assert.AreEqual(1, _viewport.ReceiveCount);
        }

        [Test]
        public void ThreePressesSameFrame_DeliversExactlyOne()
        {
            WorldPressResult first = _router.RoutePress(5, 100f, 100f, 0);
            WorldPressResult second = _router.RoutePress(5, 110f, 110f, 1);
            WorldPressResult third = _router.RoutePress(5, 120f, 120f, 2);

            Assert.AreEqual(WorldPressResult.Routed, first);
            Assert.AreEqual(WorldPressResult.NotFirstInFrame, second);
            Assert.AreEqual(WorldPressResult.NotFirstInFrame, third);
            Assert.AreEqual(1, _viewport.ReceiveCount);
            Assert.AreEqual(100f, _viewport.LastPress.ScreenX);
        }

        [Test]
        public void NextFramePress_IsDeliveredAgain()
        {
            _router.RoutePress(5, 100f, 100f, 0);
            WorldPressResult next = _router.RoutePress(6, 100f, 100f, 0);

            Assert.AreEqual(WorldPressResult.Routed, next);
            Assert.AreEqual(2, _viewport.ReceiveCount);
        }

        [Test]
        public void FirstPressRejectedThenSecondSameFrame_StillIgnored()
        {
            _uiHitTester.Hits = true;
            WorldPressResult first = _router.RoutePress(7, 100f, 100f, 0);
            _uiHitTester.Hits = false;
            WorldPressResult second = _router.RoutePress(7, 120f, 120f, 1);

            Assert.AreEqual(WorldPressResult.HitUi, first);
            Assert.AreEqual(WorldPressResult.NotFirstInFrame, second);
            Assert.AreEqual(0, _viewport.ReceiveCount);
        }

        [Test]
        public void PressHitsUi_DeliversNothing()
        {
            _uiHitTester.Hits = true;

            WorldPressResult result = _router.RoutePress(1, 100f, 100f, 0);

            Assert.AreEqual(WorldPressResult.HitUi, result);
            Assert.AreEqual(0, _viewport.ReceiveCount);
        }

        [Test]
        public void PressWhileGateHoldsToken_DeliversNothing()
        {
            _gate.AcquireInputBlock("test");

            WorldPressResult result = _router.RoutePress(1, 100f, 100f, 0);

            Assert.AreEqual(WorldPressResult.Blocked, result);
            Assert.AreEqual(0, _viewport.ReceiveCount);
        }

        [Test]
        public void PressAfterTokenReleased_Delivers()
        {
            int token = _gate.AcquireInputBlock("test");
            _router.RoutePress(1, 100f, 100f, 0);
            _gate.Release(token);

            WorldPressResult result = _router.RoutePress(2, 100f, 100f, 0);

            Assert.AreEqual(WorldPressResult.Routed, result);
            Assert.AreEqual(1, _viewport.ReceiveCount);
        }

        [Test]
        public void PressWhileShelfActive_DeliversNothing()
        {
            _tabRouter.RequestTab(AppTab.Shelf);

            WorldPressResult result = _router.RoutePress(1, 100f, 100f, 0);

            Assert.AreEqual(WorldPressResult.NoViewport, result);
            Assert.AreEqual(0, _viewport.ReceiveCount);
        }

        [Test]
        public void PressOutsideFrameRect_DeliversNothing()
        {
            WorldPressResult result = _router.RoutePress(1, 1500f, 100f, 0);

            Assert.AreEqual(WorldPressResult.OutsideFrame, result);
            Assert.AreEqual(0, _viewport.ReceiveCount);
        }

        [Test]
        public void PressOutsideAllViewports_ReturnsNoViewport()
        {
            WorldPressResult result = _router.RoutePress(1, 800f, 800f, 0);

            Assert.AreEqual(WorldPressResult.NoViewport, result);
            Assert.AreEqual(0, _viewport.ReceiveCount);
        }

        [Test]
        public void RoutedPress_UsesViewportConverterForWorldPos()
        {
            _router.RoutePress(1, 100f, 200f, 0);

            Assert.AreEqual(110f, _viewport.LastPress.WorldX);
            Assert.AreEqual(180f, _viewport.LastPress.WorldY);
            Assert.AreEqual(100f, _viewport.LastPress.ScreenX);
            Assert.AreEqual(200f, _viewport.LastPress.ScreenY);
        }

        [Test]
        public void NullFrameRectProvider_TreatsWholeScreenAsInside()
        {
            WorldPressRouter router = new WorldPressRouter(_gate, _tabRouter, _uiHitTester);
            router.RegisterViewport(AppTab.Board, _viewport);

            WorldPressResult result = router.RoutePress(1, 400f, 400f, 0);

            Assert.AreEqual(WorldPressResult.Routed, result);
        }

        [Test]
        public void UnregisteredViewport_NoLongerReceivesPress()
        {
            bool removed = _router.UnregisterViewport(AppTab.Board, _viewport);

            WorldPressResult result = _router.RoutePress(1, 100f, 100f, 0);

            Assert.IsTrue(removed);
            Assert.AreEqual(WorldPressResult.NoViewport, result);
            Assert.AreEqual(0, _viewport.ReceiveCount);
        }

        [Test]
        public void SecondViewportOnSameTab_ReceivesPressFirstViewportRejects()
        {
            FakeWorldViewport secondViewport = new FakeWorldViewport(600f, 600f, 900f, 900f);
            _router.RegisterViewport(AppTab.Board, secondViewport);

            WorldPressResult result = _router.RoutePress(1, 700f, 700f, 0);

            Assert.AreEqual(WorldPressResult.Routed, result);
            Assert.AreEqual(0, _viewport.ReceiveCount);
            Assert.AreEqual(1, secondViewport.ReceiveCount);
        }

        [Test]
        public void ViewportOnActiveShelfTab_ReceivesPressBoardViewportDoesNot()
        {
            FakeWorldViewport shelfViewport = new FakeWorldViewport(0f, 0f, 500f, 500f);
            _router.RegisterViewport(AppTab.Shelf, shelfViewport);
            _tabRouter.RequestTab(AppTab.Shelf);

            WorldPressResult result = _router.RoutePress(1, 100f, 100f, 0);

            Assert.AreEqual(WorldPressResult.Routed, result);
            Assert.AreEqual(1, shelfViewport.ReceiveCount);
            Assert.AreEqual(0, _viewport.ReceiveCount);
        }
    }
}
