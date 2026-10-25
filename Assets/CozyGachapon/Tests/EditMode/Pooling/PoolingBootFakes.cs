using System;
using System.Collections.Generic;
using CozyGachapon.Contracts;

namespace CozyGachapon.Tests.Pooling
{
    public sealed class RegisteringBootStep : IBootStep
    {
        private readonly Func<IPoolService> _serviceProvider;
        private readonly string _poolId;
        private readonly int _declaredMax;

        public string Id { get; }
        public BootPhase Phase => BootPhase.GameplayInit;
        public IReadOnlyList<string> RunsAfter => Array.Empty<string>();
        public bool IsCritical => true;
        public bool IsDone { get; private set; }
        public bool IsFailed => false;
        public PoolServicePhase PhaseAtRun { get; private set; }
        public PoolRegistrationResult Result { get; private set; }

        public RegisteringBootStep(string id, Func<IPoolService> serviceProvider, string poolId, int declaredMax)
        {
            Id = id;
            _serviceProvider = serviceProvider;
            _poolId = poolId;
            _declaredMax = declaredMax;
        }

        public void Run()
        {
            IPoolService service = _serviceProvider();
            PhaseAtRun = service.Phase;
            Result = service.Register(_poolId, _declaredMax, false);
            IsDone = true;
        }
    }

    public sealed class FakeStepRegistry
    {
        public List<IBootStep> Steps { get; } = new List<IBootStep>();

        public bool Add(IBootStep step)
        {
            Steps.Add(step);
            return true;
        }
    }
}
