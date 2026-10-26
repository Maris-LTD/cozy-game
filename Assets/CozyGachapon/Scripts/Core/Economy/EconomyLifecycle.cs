using CozyGachapon.Contracts;

namespace CozyGachapon.Economy
{
    public sealed class EconomyLifecycle
    {
        private ISimulationState _simulationState;

        public EconomyRunState State { get; private set; } = EconomyRunState.Uninitialized;

        public bool IsSimulationRunning => _simulationState == null || _simulationState.IsSimulationRunning;

        public bool IsAdOpen => _simulationState != null && _simulationState.IsAdOpen;

        public bool CanAccrueIdle => State == EconomyRunState.Running && IsSimulationRunning;

        public void SetSimulationState(ISimulationState simulationState)
        {
            _simulationState = simulationState;
            if (State == EconomyRunState.Ready && _simulationState != null && _simulationState.IsSimulationRunning)
            {
                State = EconomyRunState.Running;
            }
        }

        public void OnInitialized(ISimulationState simulationState = null)
        {
            if (simulationState != null)
            {
                _simulationState = simulationState;
            }

            State = (_simulationState != null && _simulationState.IsSimulationRunning)
                ? EconomyRunState.Running
                : EconomyRunState.Ready;
        }

        public void HandleInteractive(in InteractiveEvent evt)
        {
            if (State == EconomyRunState.Ready)
            {
                State = EconomyRunState.Running;
            }
        }

        public void HandlePaused(in PausedEvent evt)
        {
            if (State == EconomyRunState.Ready || State == EconomyRunState.Running)
            {
                State = EconomyRunState.Paused;
            }
        }

        public void HandleResumed(in ResumedEvent evt)
        {
            if (State == EconomyRunState.Paused)
            {
                State = (_simulationState != null && _simulationState.IsSimulationRunning)
                    ? EconomyRunState.Running
                    : EconomyRunState.Ready;
            }
        }
    }
}
