using System;
using System.Collections.Generic;
using CozyGachapon.Contracts;

namespace CozyGachapon.Foundation
{
    public sealed class TabRouter : ITabRouter
    {
        private readonly HashSet<int> _activeTabLocks = new HashSet<int>(16);
        private AppTab _activeTab;
        private bool _isBooting;
        private int _nextLockTokenId = 1;

        public event Action<TabChangedEvent> OnTabChanged;

        public AppTab ActiveTab => _activeTab;

        public bool IsBooting
        {
            get => _isBooting;
            set => _isBooting = value;
        }

        public int ActiveLockCount => _activeTabLocks.Count;

        public TabRouter(AppTab defaultTab = AppTab.Board, bool isBooting = false)
        {
            _activeTab = defaultTab;
            _isBooting = isBooting;
        }

        public TabRouter(SceneFlowConfig config)
        {
            _activeTab = config != null ? config.DefaultTab : AppTab.Board;
            _isBooting = false;
        }

        public RequestTabResult RequestTab(AppTab targetTab, int token = 0)
        {
            if (_isBooting)
            {
                return RequestTabResult.Locked;
            }

            if (targetTab == _activeTab)
            {
                return RequestTabResult.NoChange;
            }

            if (_activeTabLocks.Count > 0)
            {
                if (_activeTabLocks.Count == 1 && token != 0 && _activeTabLocks.Contains(token))
                {
                    return SwitchToTab(targetTab);
                }

                return RequestTabResult.Locked;
            }

            return SwitchToTab(targetTab);
        }

        public int AcquireTabLock(string owner = null)
        {
            int token = _nextLockTokenId++;
            _activeTabLocks.Add(token);
            return token;
        }

        public bool LockTab(AppTab targetTab, out int token)
        {
            token = AcquireTabLock();
            return true;
        }

        public void ReleaseTabLock(int token)
        {
            if (token > 0)
            {
                _activeTabLocks.Remove(token);
            }
        }

        private RequestTabResult SwitchToTab(AppTab targetTab)
        {
            AppTab previous = _activeTab;
            _activeTab = targetTab;
            OnTabChanged?.Invoke(new TabChangedEvent(previous, targetTab));
            return RequestTabResult.Changed;
        }
    }
}
