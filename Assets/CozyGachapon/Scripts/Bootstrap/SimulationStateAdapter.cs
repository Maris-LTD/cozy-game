using CozyGachapon.Contracts;
using CozyGachapon.Foundation;

namespace CozyGachapon
{
    public sealed class SimulationStateAdapter : ISimulationState
    {
        private readonly AppLifecycleWatchdog _lifecycle;

        public SimulationStateAdapter(AppLifecycleWatchdog lifecycle)
        {
            _lifecycle = lifecycle;
        }

        public bool IsSimulationRunning => _lifecycle != null && _lifecycle.IsSimulationRunning;
        public bool IsAdOpen => _lifecycle != null && _lifecycle.IsAdOpen;
    }
}
