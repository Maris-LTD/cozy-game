using System;
using System.Collections.Generic;
using CozyGachapon.Contracts;

namespace CozyGachapon.Foundation
{
    public sealed class BootStepSequencer
    {
        private readonly List<IBootStep> _registeredSteps = new List<IBootStep>();
        private readonly HashSet<string> _registeredIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<IBootStep> _executionPlan = new List<IBootStep>();
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

            if (!BuildExecutionPlan())
            {
                _hasFailed = true;
                return false;
            }

            _isStarted = true;
            _currentStepIndex = 0;
            _currentStepRan = false;
            return true;
        }

        public void Tick(float deltaTime)
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

                        _currentStepIndex++;
                        _currentStepRan = false;
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

                    _currentStepIndex++;
                    _currentStepRan = false;
                    continue;
                }

                if (!currentStep.IsDone)
                {
                    return;
                }

                _currentStepIndex++;
                _currentStepRan = false;
            }

            _isCompleted = true;
            OnBootCompleted?.Invoke(new BootCompletedEvent(_elapsedTime));
        }

        private bool BuildExecutionPlan()
        {
            _executionPlan.Clear();
            Array phases = Enum.GetValues(typeof(BootPhase));

            for (int i = 0; i < phases.Length; i++)
            {
                BootPhase phase = (BootPhase)phases.GetValue(i);
                List<IBootStep> phaseSteps = new List<IBootStep>();

                for (int j = 0; j < _registeredSteps.Count; j++)
                {
                    if (_registeredSteps[j].Phase == phase)
                    {
                        phaseSteps.Add(_registeredSteps[j]);
                    }
                }

                if (phaseSteps.Count == 0)
                {
                    continue;
                }

                if (!SortPhaseSteps(phaseSteps, out List<IBootStep> sortedSteps))
                {
                    _hasFailed = true;
                    OnBootFailed?.Invoke(new BootFailedEvent(string.Empty, phase, "Invalid or cyclic dependency in phase " + phase));
                    return false;
                }

                _executionPlan.AddRange(sortedSteps);
            }

            return true;
        }

        private static bool SortPhaseSteps(List<IBootStep> phaseSteps, out List<IBootStep> sorted)
        {
            sorted = new List<IBootStep>(phaseSteps.Count);
            Dictionary<string, IBootStep> stepMap = new Dictionary<string, IBootStep>(StringComparer.Ordinal);
            Dictionary<string, int> inDegrees = new Dictionary<string, int>(StringComparer.Ordinal);
            Dictionary<string, List<string>> dependents = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            for (int i = 0; i < phaseSteps.Count; i++)
            {
                IBootStep step = phaseSteps[i];
                stepMap[step.Id] = step;
                inDegrees[step.Id] = 0;
                dependents[step.Id] = new List<string>();
            }

            for (int i = 0; i < phaseSteps.Count; i++)
            {
                IBootStep step = phaseSteps[i];
                IReadOnlyList<string> runsAfter = step.RunsAfter;
                if (runsAfter == null)
                {
                    continue;
                }

                HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
                for (int j = 0; j < runsAfter.Count; j++)
                {
                    string depId = runsAfter[j];
                    if (string.IsNullOrEmpty(depId) || !stepMap.ContainsKey(depId))
                    {
                        return false;
                    }

                    if (seen.Add(depId))
                    {
                        inDegrees[step.Id]++;
                        dependents[depId].Add(step.Id);
                    }
                }
            }

            List<string> ready = new List<string>();
            foreach (KeyValuePair<string, int> pair in inDegrees)
            {
                if (pair.Value == 0)
                {
                    ready.Add(pair.Key);
                }
            }

            ready.Sort(StringComparer.Ordinal);

            while (ready.Count > 0)
            {
                string nextId = ready[0];
                ready.RemoveAt(0);
                sorted.Add(stepMap[nextId]);

                List<string> deps = dependents[nextId];
                for (int i = 0; i < deps.Count; i++)
                {
                    string dep = deps[i];
                    inDegrees[dep]--;
                    if (inDegrees[dep] == 0)
                    {
                        ready.Add(dep);
                        ready.Sort(StringComparer.Ordinal);
                    }
                }
            }

            return sorted.Count == phaseSteps.Count;
        }
    }
}
