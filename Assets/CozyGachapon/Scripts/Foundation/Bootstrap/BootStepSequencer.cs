using System;
using System.Collections.Generic;
using CozyGachapon.Contracts;

namespace CozyGachapon.Foundation
{
    public sealed class BootStepSequencer
    {
        public const string MissingConfigStepId = "boot.config";

        private readonly List<IBootStep> _registeredSteps = new List<IBootStep>();
        private readonly HashSet<string> _registeredIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<IBootStep> _executionPlan = new List<IBootStep>();
        private readonly List<string> _skippedNonCriticalStepIds = new List<string>();
        private BootConfig _bootConfig;
        private SceneFlowConfig _sceneFlowConfig;
        private BootWatchdog _watchdog;
        private int _currentStepIndex = -1;
        private bool _currentStepRan;
        private float _elapsedTime;
        private bool _isStarted;
        private bool _isCompleted;
        private bool _hasFailed;

        public event Action<BootCompletedEvent> OnBootCompleted;
        public event Action<BootFailedEvent> OnBootFailed;

        public bool IsStarted => _isStarted;
        public bool IsCompleted => _isCompleted;
        public bool HasFailed => _hasFailed;
        public float ElapsedTime => _elapsedTime;
        public int RegisteredCount => _registeredSteps.Count;
        public IReadOnlyList<string> SkippedNonCriticalStepIds => _skippedNonCriticalStepIds;

        public BootStepSequencer(BootConfig bootConfig = null, SceneFlowConfig sceneFlowConfig = null)
        {
            _bootConfig = bootConfig;
            _sceneFlowConfig = sceneFlowConfig;
        }

        public void SetConfigs(BootConfig bootConfig, SceneFlowConfig sceneFlowConfig)
        {
            if (_isStarted)
            {
                return;
            }

            _bootConfig = bootConfig;
            _sceneFlowConfig = sceneFlowConfig;
        }

        public bool RegisterStep(IBootStep step)
        {
            if (step == null || _isStarted || _hasFailed)
            {
                return false;
            }

            if (string.IsNullOrEmpty(step.Id) || _registeredIds.Contains(step.Id))
            {
                _hasFailed = true;
                OnBootFailed?.Invoke(new BootFailedEvent(step.Id ?? string.Empty, step.Phase, "Duplicate or empty step ID"));
                return false;
            }

            _registeredIds.Add(step.Id);
            _registeredSteps.Add(step);
            return true;
        }

        public bool Start()
        {
            if (_isStarted || _hasFailed)
            {
                return false;
            }

            if (_bootConfig == null || _sceneFlowConfig == null)
            {
                _hasFailed = true;
                OnBootFailed?.Invoke(new BootFailedEvent(MissingConfigStepId, BootPhase.ServiceInit, "Missing mandatory configuration"));
                return false;
            }

            if (!BuildExecutionPlan())
            {
                _hasFailed = true;
                return false;
            }

            _watchdog = new BootWatchdog(_bootConfig.StepTimeoutSeconds);
            _isStarted = true;
            _currentStepIndex = 0;
            _currentStepRan = false;
            return true;
        }

        public void Tick(float deltaTime, bool isVisible = true)
        {
            if (!_isStarted || _isCompleted || _hasFailed)
            {
                return;
            }

            _elapsedTime += Math.Max(0f, deltaTime);

            while (_currentStepIndex < _executionPlan.Count)
            {
                IBootStep currentStep = _executionPlan[_currentStepIndex];

                if (!_currentStepRan)
                {
                    _currentStepRan = true;
                    _watchdog?.ResetStep();
                    try
                    {
                        currentStep.Run();
                    }
                    catch (Exception ex)
                    {
                        if (currentStep.IsCritical)
                        {
                            _hasFailed = true;
                            OnBootFailed?.Invoke(new BootFailedEvent(currentStep.Id, currentStep.Phase, ex.Message));
                            return;
                        }

                        _skippedNonCriticalStepIds.Add(currentStep.Id);
                        _currentStepIndex++;
                        _currentStepRan = false;
                        _watchdog?.ResetStep();
                        continue;
                    }
                }

                if (currentStep.IsFailed)
                {
                    if (currentStep.IsCritical)
                    {
                        _hasFailed = true;
                        OnBootFailed?.Invoke(new BootFailedEvent(currentStep.Id, currentStep.Phase, "Step execution failed"));
                        return;
                    }

                    _skippedNonCriticalStepIds.Add(currentStep.Id);
                    _currentStepIndex++;
                    _currentStepRan = false;
                    _watchdog?.ResetStep();
                    continue;
                }

                if (!currentStep.IsDone)
                {
                    _watchdog?.Tick(deltaTime, isVisible);
                    if (_watchdog != null && _watchdog.IsCurrentStepTimedOut)
                    {
                        if (currentStep.IsCritical)
                        {
                            _hasFailed = true;
                            OnBootFailed?.Invoke(new BootFailedEvent(currentStep.Id, currentStep.Phase, "Step timed out"));
                            return;
                        }

                        _skippedNonCriticalStepIds.Add(currentStep.Id);
                        _currentStepIndex++;
                        _currentStepRan = false;
                        _watchdog.ResetStep();
                        continue;
                    }

                    return;
                }

                _currentStepIndex++;
                _currentStepRan = false;
                _watchdog?.ResetStep();
            }

            _isCompleted = true;
            OnBootCompleted?.Invoke(new BootCompletedEvent(_elapsedTime));
        }

        private bool BuildExecutionPlan()
        {
            _executionPlan.Clear();
            if (!BootStepOrderResolver.TryBuildExecutionPlan(
                _registeredSteps,
                out List<IBootStep> plan,
                out string errorStepId,
                out BootPhase errorPhase,
                out string errorMessage))
            {
                _hasFailed = true;
                OnBootFailed?.Invoke(new BootFailedEvent(errorStepId, errorPhase, errorMessage));
                return false;
            }

            _executionPlan.AddRange(plan);
            return true;
        }
    }
}
