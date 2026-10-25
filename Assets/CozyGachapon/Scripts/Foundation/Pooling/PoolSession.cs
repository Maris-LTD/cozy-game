using System;
using CozyGachapon.Contracts;

namespace CozyGachapon.Pooling
{
    public sealed class PoolSession : IDisposable
    {
        private readonly PoolContainer _containers;

        public ObjectPoolService Service { get; }
        public bool IsTornDown { get; private set; }

        public PoolSession(PoolingConfig config, IPoolDiagnosticsSink sink)
        {
            _containers = new PoolContainer();
            Service = new ObjectPoolService(config, sink, _containers);
        }

        public void Teardown()
        {
            if (IsTornDown)
            {
                return;
            }

            IsTornDown = true;
            Service.Shutdown();
            _containers.DestroyAll();
        }

        public void Dispose()
        {
            Teardown();
        }
    }
}
