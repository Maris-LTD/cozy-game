using System.Collections.Generic;
using CozyGachapon.Contracts;

namespace CozyGachapon.Pooling
{
    public sealed class PoolingServiceBootStep : IBootStep
    {
        public const string StepId = "pooling.service";

        private static readonly string[] NoDependencies = new string[0];

        private readonly PoolingConfig _config;
        private readonly IPoolDiagnosticsSink _sink;

        public string Id => StepId;
        public BootPhase Phase => BootPhase.ServiceInit;
        public IReadOnlyList<string> RunsAfter => NoDependencies;
        public bool IsCritical => true;
        public bool IsDone { get; private set; }
        public bool IsFailed { get; private set; }
        public ObjectPoolService Service { get; private set; }

        public PoolingServiceBootStep(PoolingConfig config, IPoolDiagnosticsSink sink)
        {
            _config = config;
            _sink = sink;
        }

        public void Run()
        {
            if (_config == null || _sink == null)
            {
                IsFailed = true;
                return;
            }

            Service = new ObjectPoolService(_config, _sink);
            IsDone = true;
        }
    }
}
