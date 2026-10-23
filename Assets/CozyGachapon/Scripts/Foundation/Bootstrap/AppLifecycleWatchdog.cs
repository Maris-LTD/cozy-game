using System;
using CozyGachapon.Contracts;

namespace CozyGachapon.Foundation
{
    public sealed class AppLifecycleWatchdog
    {
        private readonly ITimeProvider _timeProvider;
        private AppLifecycleState _state = AppLifecycleState.BootingSteps;
        private AppLifecycleState _previousState = AppLifecycleState.BootingSteps;
        private PauseReason _lastPauseReason = PauseReason.Hidden;
        private DateTimeOffset _pauseUtcTime;
        private DateTimeOffset _adOpenedUtcTime;
        private bool _isPageVisible = true;
        private bool _isAdOpen;

        public event Action<PausedEvent> OnPaused;
        public event Action<ResumedEvent> OnResumed;
        public event Action<QuittingEvent> OnQuitting;
        public event Action<AdClosedEvent> OnAdClosed;

        public AppLifecycleState State => _state;
        public AppLifecycleState PreviousState => _previousState;
        public bool IsSimulationRunning => _state == AppLifecycleState.Running;
        public bool IsPaused => _state == AppLifecycleState.Paused;
        public bool IsQuitting => _state == AppLifecycleState.Quitting;
        public bool IsPageVisible => _isPageVisible;
        public bool IsAdOpen => _isAdOpen;

        public AppLifecycleWatchdog(ITimeProvider timeProvider = null)
        {
            _timeProvider = timeProvider ?? new SystemTimeProvider();
        }

        public void NotifyBootCompleted()
        {
            if (_state != AppLifecycleState.BootingSteps)
            {
                return;
            }

            if (_isPageVisible)
            {
                _state = AppLifecycleState.BootingCurtain;
            }
            else
            {
                _previousState = AppLifecycleState.BootingCurtain;
                _state = AppLifecycleState.Paused;
                _pauseUtcTime = _timeProvider.UtcNow;
                OnPaused?.Invoke(new PausedEvent(_lastPauseReason));
            }
        }

        public void NotifyInteractive()
        {
            if (_state == AppLifecycleState.BootingCurtain)
            {
                _state = AppLifecycleState.Running;
            }
        }

        public void SetAdOpen(bool isOpen)
        {
            if (_state == AppLifecycleState.Quitting)
            {
                return;
            }

            if (_isAdOpen == isOpen)
            {
                return;
            }

            if (isOpen)
            {
                _isAdOpen = true;
                _adOpenedUtcTime = _timeProvider.UtcNow;
                return;
            }

            _isAdOpen = false;
            double delta = (_timeProvider.UtcNow - _adOpenedUtcTime).TotalSeconds;
            double adSeconds = double.IsNaN(delta) || delta < 0.0 ? 0.0 : delta;
            OnAdClosed?.Invoke(new AdClosedEvent(adSeconds));

            if (!_isPageVisible)
            {
                if (_state == AppLifecycleState.BootingCurtain || _state == AppLifecycleState.Running)
                {
                    _previousState = _state;
                    _state = AppLifecycleState.Paused;
                    _pauseUtcTime = _timeProvider.UtcNow;
                    OnPaused?.Invoke(new PausedEvent(_lastPauseReason));
                }
            }
        }

        public void OnWindowFocusChanged(bool hasFocus)
        {
        }

        public void OnVisibilityChanged(bool isVisible, PauseReason reason = PauseReason.Hidden)
        {
            _isPageVisible = isVisible;

            if (_state == AppLifecycleState.Quitting)
            {
                return;
            }

            if (!isVisible)
            {
                _lastPauseReason = reason;

                if (_isAdOpen)
                {
                    return;
                }

                if (_state == AppLifecycleState.Paused || _state == AppLifecycleState.BootingSteps)
                {
                    return;
                }

                if (_state == AppLifecycleState.BootingCurtain || _state == AppLifecycleState.Running)
                {
                    _previousState = _state;
                    _state = AppLifecycleState.Paused;
                    _pauseUtcTime = _timeProvider.UtcNow;
                    OnPaused?.Invoke(new PausedEvent(reason));
                }
            }
            else
            {
                if (_state == AppLifecycleState.Paused)
                {
                    double delta = (_timeProvider.UtcNow - _pauseUtcTime).TotalSeconds;
                    double awaySeconds = double.IsNaN(delta) || delta < 0.0 ? 0.0 : delta;

                    _state = _previousState;
                    OnResumed?.Invoke(new ResumedEvent(_lastPauseReason, awaySeconds));
                }
            }
        }

        public void OnApplicationQuit()
        {
            if (_state == AppLifecycleState.Quitting)
            {
                return;
            }

            _state = AppLifecycleState.Quitting;
            OnQuitting?.Invoke(new QuittingEvent());
        }
    }
}
