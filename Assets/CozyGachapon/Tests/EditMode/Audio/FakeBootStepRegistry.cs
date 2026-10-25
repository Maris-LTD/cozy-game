using System.Collections.Generic;
using CozyGachapon.Contracts;

namespace CozyGachapon.Tests.Audio
{
    public sealed class FakeBootStepRegistry
    {
        public List<IBootStep> Steps { get; } = new List<IBootStep>();

        public bool Add(IBootStep step)
        {
            Steps.Add(step);
            return true;
        }
    }
}
