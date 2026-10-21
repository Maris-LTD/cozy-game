using System.Collections.Generic;

namespace CozyGachapon.Contracts
{
    public interface IBootStep
    {
        string Id { get; }
        BootPhase Phase { get; }
        IReadOnlyList<string> RunsAfter { get; }
        bool IsCritical { get; }
        bool IsDone { get; }
        bool IsFailed { get; }
        void Run();
    }
}
