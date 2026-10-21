using System;
using CozyGachapon.Contracts;

namespace CozyGachapon.Foundation
{
    public sealed class StartupCurtainController
    {
        private readonly BootConfig _bootConfig;
        private readonly IInputBlockGate _blockGate;
        private CurtainState _state = CurtainState.Covering;
        private int _curtainToken;
        private float _coveringElapsedSeconds;
        private float _revealingElapsedSeconds;
        private float _revealProgress;
        private float _totalVisibleSeconds;
        private float _bootToInteractiveDuration;
        private bool _isBootCompleted;
        private bool _isInteractive;
        private bool _isSimulationRunning;
        private bool _isBudgetExceeded;
        private bool _isVisible = true;

        public event Action<InteractiveEvent> OnInteractive;

        public CurtainState State => _state;
        public float RevealProgress => _revealProgress;
        public bool IsInteractive => _isInteractive;
        public bool IsSimulationRunning => _isSimulationRunning;
        public bool IsBudgetExceeded => _isBudgetExceeded;
        public float BootToInteractiveDuration => _bootToInteractiveDuration;

        public StartupCurtainController(BootConfig bootConfig, IInputBlockGate blockGate)
        {
            _bootConfig = bootConfig ?? throw new ArgumentNullException(nameof(bootConfig));
            _blockGate = blockGate ?? throw new ArgumentNullException(nameof(blockGate));
            _curtainToken = _blockGate.AcquireInputBlock("StartupCurtain");
        }

        public void NotifyBootCompleted(BootCompletedEvent evt)
        {
            _isBootCompleted = true;
        }

        public void NotifyBootFailed(BootFailedEvent evt)
        {
            _state = CurtainState.ErrorShown;
        }

        public void SetVisibility(bool isVisible)
        {
            _isVisible = isVisible;

            if (!_isVisible && _state == CurtainState.Revealing)
            {
                _state = CurtainState.RevealFrozen;
            }
            else if (_isVisible && _state == CurtainState.RevealFrozen)
            {
                _state = CurtainState.Revealing;
            }
        }

        public void Tick(float deltaTime)
        {
            if (_state == CurtainState.ErrorShown || _state == CurtainState.Open)
            {
                return;
            }

            if (_isVisible)
            {
                _totalVisibleSeconds += Math.Max(0f, deltaTime);
            }

            if (_state == CurtainState.Covering)
            {
                _coveringElapsedSeconds += Math.Max(0f, deltaTime);
                if (_isBootCompleted && _coveringElapsedSeconds >= _bootConfig.CurtainMinDisplaySeconds)
                {
                    _state = _isVisible ? CurtainState.Revealing : CurtainState.RevealFrozen;
                }
            }
            else if (_state == CurtainState.Revealing)
            {
                _revealingElapsedSeconds += Math.Max(0f, deltaTime);
                float duration = Math.Max(0.01f, _bootConfig.CurtainRevealDuration);
                _revealProgress = Math.Min(1.0f, _revealingElapsedSeconds / duration);

                if (_revealProgress >= 1.0f)
                {
                    _state = CurtainState.Open;
                    _blockGate.Release(_curtainToken);
                    _curtainToken = 0;
                    _isSimulationRunning = true;
                    _isInteractive = true;
                    _bootToInteractiveDuration = _totalVisibleSeconds;
                    _isBudgetExceeded = _bootToInteractiveDuration > _bootConfig.MaxBootToInteractiveSeconds;
                    OnInteractive?.Invoke(new InteractiveEvent(_bootToInteractiveDuration));
                }
            }
        }
    }
}
