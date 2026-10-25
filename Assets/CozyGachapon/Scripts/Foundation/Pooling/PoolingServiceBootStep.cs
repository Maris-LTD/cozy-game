using System;
using System.Collections.Generic;
using CozyGachapon.Contracts;

namespace CozyGachapon.Pooling
{
    public sealed class PoolingServiceBootStep : IBootStep
    {
        public const string StepId = "pooling.service";

        private readonly PoolingConfig _config;
        private readonly IPoolDiagnosticsSink _sink;

        public string Id => StepId;
        public BootPhase Phase => BootPhase.ServiceInit;
        public IReadOnlyList<string> RunsAfter => Array.Empty<string>();
        public bool IsCritical => true;
        public bool IsDone { get; private set; }
        public bool IsFailed { get; private set; }
        public PoolSession Session { get; private set; }
        public ObjectPoolService Service => Session != null ? Session.Service : null;

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

            if (Session != null)
            {
                Session.Teardown();
            }

            Session = new PoolSession(_config, _sink);
            IsDone = true;
        }
    }
}
