using System;
using System.Collections.Generic;
using CozyGachapon.Contracts;

namespace CozyGachapon.Foundation
{
    public sealed class WorldPressRouter
    {
        private readonly IInputBlockGate _gate;
        private readonly ITabRouter _tabRouter;
        private readonly IUiHitTester _uiHitTester;
        private readonly IFrameRectProvider _frameRect;
        private readonly List<IWorldViewport>[] _viewportsByTab;
        private bool _hasConsumedFrame;
        private int _lastConsumedFrame;

        public WorldPressRouter(IInputBlockGate gate, ITabRouter tabRouter, IUiHitTester uiHitTester, IFrameRectProvider frameRect = null)
        {
            _gate = gate;
            _tabRouter = tabRouter;
            _uiHitTester = uiHitTester;
            _frameRect = frameRect;

            Array tabs = Enum.GetValues(typeof(AppTab));
            _viewportsByTab = new List<IWorldViewport>[tabs.Length];
            for (int i = 0; i < _viewportsByTab.Length; i++)
            {
                _viewportsByTab[i] = new List<IWorldViewport>(2);
            }
        }

        public void RegisterViewport(AppTab tab, IWorldViewport viewport)
        {
            List<IWorldViewport> list = _viewportsByTab[(int)tab];
            if (viewport != null && !list.Contains(viewport))
            {
                list.Add(viewport);
            }
        }

        public bool UnregisterViewport(AppTab tab, IWorldViewport viewport)
        {
            return _viewportsByTab[(int)tab].Remove(viewport);
        }

        public WorldPressResult RoutePress(int frameIndex, float screenX, float screenY, int pointerId)
        {
            if (_hasConsumedFrame && frameIndex == _lastConsumedFrame)
            {
                return WorldPressResult.NotFirstInFrame;
            }

            _hasConsumedFrame = true;
            _lastConsumedFrame = frameIndex;

            if (_frameRect != null && !_frameRect.Contains(screenX, screenY))
            {
                return WorldPressResult.OutsideFrame;
            }

            if (_uiHitTester != null && _uiHitTester.HitsUi(screenX, screenY, pointerId))
            {
                return WorldPressResult.HitUi;
            }

            if (_gate.IsInputBlocked)
            {
                return WorldPressResult.Blocked;
            }

            List<IWorldViewport> viewports = _viewportsByTab[(int)_tabRouter.ActiveTab];
            for (int i = 0; i < viewports.Count; i++)
            {
                IWorldViewport viewport = viewports[i];
                if (viewport.TryScreenToWorld(screenX, screenY, out float worldX, out float worldY))
                {
                    viewport.ReceiveWorldPress(new WorldPressEvent(worldX, worldY, screenX, screenY));
                    return WorldPressResult.Routed;
                }
            }

            return WorldPressResult.NoViewport;
        }
    }
}
