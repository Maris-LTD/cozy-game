using System;
using CozyGachapon.Contracts;
using UnityEngine;

namespace CozyGachapon.Pooling
{
    public sealed class PoolingBootModule
    {
        public PoolingServiceBootStep ServiceStep { get; }
        public PoolingPrewarmBootStep PrewarmStep { get; }
        public ObjectPoolService Service => ServiceStep.Service;

        public PoolingBootModule(PoolingConfig config, Transform root, IPoolDiagnosticsSink sink, int targetFps)
        {
            ServiceStep = new PoolingServiceBootStep(config, sink);
            PrewarmStep = new PoolingPrewarmBootStep(ServiceStep, config, root, sink, targetFps);
        }

        public bool RegisterSteps(Func<IBootStep, bool> register)
        {
            bool serviceAccepted = register(ServiceStep);
            bool prewarmAccepted = register(PrewarmStep);
            return serviceAccepted && prewarmAccepted;
        }

        public void Tick()
        {
            PrewarmStep.Pump();
        }
    }
}
