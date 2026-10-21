using System;

namespace CozyGachapon.Foundation
{
    public sealed class BootWatchdog
    {
        private readonly float _stepTimeoutSeconds;
        private float _currentStepVisibleTime;

        public float StepTimeoutSeconds => _stepTimeoutSeconds;
        public float CurrentStepVisibleTime => _currentStepVisibleTime;
        public bool IsCurrentStepTimedOut => _currentStepVisibleTime > _stepTimeoutSeconds;

        public BootWatchdog(float stepTimeoutSeconds)
        {
            _stepTimeoutSeconds = Math.Max(0.1f, stepTimeoutSeconds);
            _currentStepVisibleTime = 0f;
        }

        public void ResetStep()
        {
            _currentStepVisibleTime = 0f;
        }

        public void Tick(float deltaTime, bool isVisible)
        {
            if (isVisible)
            {
                _currentStepVisibleTime += Math.Max(0f, deltaTime);
            }
        }
    }
}
