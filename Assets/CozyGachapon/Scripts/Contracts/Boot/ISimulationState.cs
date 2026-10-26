namespace CozyGachapon.Contracts
{
    public interface ISimulationState
    {
        bool IsSimulationRunning { get; }
        bool IsAdOpen { get; }
    }
}
