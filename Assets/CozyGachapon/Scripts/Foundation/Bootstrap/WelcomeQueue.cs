using System;
using System.Collections.Generic;
using CozyGachapon.Contracts;

namespace CozyGachapon.Foundation
{
    public class WelcomeQueue
    {
        private readonly SceneFlowConfig _config;
        private readonly IInputBlockGate _gate;
        private readonly List<IWelcomeStep> _registered = new List<IWelcomeStep>();
        private readonly List<IWelcomeStep> _ordered = new List<IWelcomeStep>();
        private int _currentIndex = -1;
        private int _activeToken;
        private bool _hasToken;
        private int _shownCount;
        private bool _advancing;
        private bool _pendingAdvance;

        public WelcomeQueueState State { get; private set; } = WelcomeQueueState.Waiting;

        public event Action<WelcomeQueueCompletedEvent> OnCompleted;

        public WelcomeQueue(SceneFlowConfig config, IInputBlockGate gate)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        }

        public bool Register(IWelcomeStep step)
        {
            if (State != WelcomeQueueState.Waiting || step == null || string.IsNullOrEmpty(step.Id))
            {
                return false;
            }

            for (int i = 0; i < _registered.Count; i++)
            {
                if (string.Equals(_registered[i].Id, step.Id, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            _registered.Add(step);
            return true;
        }

        public void Begin()
        {
            if (State != WelcomeQueueState.Waiting)
            {
                return;
            }

            State = WelcomeQueueState.Running;
            WelcomeStepOrder.Build(_config.WelcomeOrder, _registered, _ordered);
            Advance();
        }

        public IReadOnlyList<string> Validate()
        {
            List<string> unnamed = new List<string>();
            WelcomeStepOrder.CollectUnnamed(_config.WelcomeOrder, _registered, unnamed);
            return unnamed;
        }

        private void OnStepDone(StepCompletion completion)
        {
            if (completion.Fired || State != WelcomeQueueState.Running || completion.Index != _currentIndex)
            {
                return;
            }

            completion.Fired = true;
            if (_hasToken)
            {
                _gate.Release(_activeToken);
                _hasToken = false;
            }

            Advance();
        }

        private void Advance()
        {
            if (_advancing)
            {
                _pendingAdvance = true;
                return;
            }

            _advancing = true;
            try
            {
                do
                {
                    _pendingAdvance = false;
                    IWelcomeStep step = NextVisibleStep();
                    if (step == null)
                    {
                        Complete();
                        return;
                    }

                    _activeToken = _gate.AcquireInputBlock(step.Id);
                    _hasToken = true;
                    _shownCount++;
                    step.Show(new StepCompletion(this, _currentIndex).Invoke);
                }
                while (_pendingAdvance);
            }
            finally
            {
                _advancing = false;
            }
        }

        private IWelcomeStep NextVisibleStep()
        {
            while (++_currentIndex < _ordered.Count)
            {
                if (_ordered[_currentIndex].ShouldShow())
                {
                    return _ordered[_currentIndex];
                }
            }

            return null;
        }

        private void Complete()
        {
            State = WelcomeQueueState.Done;
            OnCompleted?.Invoke(new WelcomeQueueCompletedEvent(_shownCount));
        }

        private sealed class StepCompletion
        {
            private readonly WelcomeQueue _owner;

            public int Index { get; }
            public bool Fired { get; set; }

            public StepCompletion(WelcomeQueue owner, int index)
            {
                _owner = owner;
                Index = index;
            }

            public void Invoke()
            {
                _owner.OnStepDone(this);
            }
        }
    }
}
