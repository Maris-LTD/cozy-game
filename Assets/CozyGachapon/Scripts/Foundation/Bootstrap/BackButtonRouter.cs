using System.Collections.Generic;
using CozyGachapon.Contracts;

namespace CozyGachapon.Foundation
{
    public sealed class BackButtonRouter
    {
        private readonly ITabRouter _tabRouter;
        private readonly IBootStatusReader _bootStatus;
        private readonly IAppBackgroundRequester _backgroundRequester;
        private readonly HostPlatform _platform;
        private readonly List<IModalBackHandler> _handlers = new List<IModalBackHandler>(4);

        public int HandlerCount => _handlers.Count;

        public BackButtonRouter(ITabRouter tabRouter, IBootStatusReader bootStatus, IAppBackgroundRequester backgroundRequester, HostPlatform platform)
        {
            _tabRouter = tabRouter;
            _bootStatus = bootStatus;
            _backgroundRequester = backgroundRequester;
            _platform = platform;
        }

        public void PushBackHandler(IModalBackHandler handler)
        {
            if (handler != null)
            {
                _handlers.Add(handler);
            }
        }

        public bool PopBackHandler(IModalBackHandler handler)
        {
            for (int i = _handlers.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(_handlers[i], handler))
                {
                    _handlers.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public BackPressResult HandleBack()
        {
            if (_bootStatus.IsBootFailed)
            {
                return MoveToBackgroundIfAndroid();
            }

            if (_bootStatus.IsBooting)
            {
                return BackPressResult.IgnoredWhileBooting;
            }

            if (_handlers.Count > 0)
            {
                _handlers[_handlers.Count - 1].HandleBack();
                return BackPressResult.HandledByModal;
            }

            if (_tabRouter.ActiveTab != AppTab.Board)
            {
                RequestTabResult result = _tabRouter.RequestTab(AppTab.Board);
                return result == RequestTabResult.Changed ? BackPressResult.ReturnedToBoard : BackPressResult.TabLocked;
            }

            return MoveToBackgroundIfAndroid();
        }

        private BackPressResult MoveToBackgroundIfAndroid()
        {
            if (_platform != HostPlatform.Android)
            {
                return BackPressResult.NoAction;
            }

            _backgroundRequester.RequestMoveToBackground();
            return BackPressResult.MovedToBackground;
        }
    }
}
