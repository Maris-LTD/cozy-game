using CozyGachapon.Contracts;

namespace CozyGachapon.Foundation
{
    public sealed class BootStatusTracker : IBootStatusReader
    {
        private readonly AppLifecycleWatchdog _watchdog;

        public bool IsBootFailed { get; private set; }

        public bool IsBooting
        {
            get
            {
                if (IsBootFailed)
                {
                    return false;
                }

                AppLifecycleState state = _watchdog.State;
                return state == AppLifecycleState.BootingSteps || state == AppLifecycleState.BootingCurtain;
            }
        }

        public BootStatusTracker(AppLifecycleWatchdog watchdog)
        {
            _watchdog = watchdog;
        }

        public void NotifyBootFailed(BootFailedEvent bootFailed)
        {
            IsBootFailed = true;
        }
    }
}
