using System;

namespace CozyGachapon.Contracts
{
    public interface ITabRouter
    {
        AppTab ActiveTab { get; }
        bool IsBooting { get; set; }
        RequestTabResult RequestTab(AppTab targetTab, int token = 0);
        int AcquireTabLock(string owner = null);
        bool LockTab(AppTab targetTab, out int token);
        void ReleaseTabLock(int token);
        event Action<TabChangedEvent> OnTabChanged;
    }
}
