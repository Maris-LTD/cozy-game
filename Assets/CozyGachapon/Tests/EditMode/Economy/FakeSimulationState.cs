using CozyGachapon.Contracts;

namespace CozyGachapon.Tests.Economy
{
    public sealed class FakeSimulationState : ISimulationState
    {
        public bool IsSimulationRunning { get; set; }
        public bool IsAdOpen { get; set; }

        public FakeSimulationState(bool isRunning = false, bool isAdOpen = false)
        {
            IsSimulationRunning = isRunning;
            IsAdOpen = isAdOpen;
        }
    }
}
