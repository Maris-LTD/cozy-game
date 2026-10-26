using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using CozyGachapon.Foundation;

namespace CozyGachapon
{
    public sealed class EconomyWiring
    {
        private readonly EconomyConfig _config;
        private readonly IServiceRegistry _services;
        private BootstrapGraph _graph;

        public EconomyBootModule Module { get; private set; }
        public EconomyCore Core { get; private set; }
        public EconomyDiagnosticsRecorder Diagnostics { get; private set; }
        public SimulationStateAdapter SimulationState { get; private set; }
        public bool IsRegistered => _graph != null;

        public EconomyWiring(EconomyConfig config, IServiceRegistry services)
        {
            _config = config;
            _services = services;
            Diagnostics = new EconomyDiagnosticsRecorder();
        }

        public bool Register(BootstrapGraph graph)
        {
            if (graph == null || _graph != null)
            {
                return false;
            }

            _graph = graph;
            SimulationState = new SimulationStateAdapter(graph.Lifecycle);
            Module = new EconomyBootModule(_config, Diagnostics, _services, SimulationState);
            Core = Module.Core;

            bool registered = Module.RegisterSteps(graph.RegisterStep);
            graph.Lifecycle.OnPaused += HandlePaused;
            graph.Lifecycle.OnResumed += HandleResumed;
            graph.Lifecycle.OnAdClosed += HandleAdClosed;
            graph.Curtain.OnInteractive += HandleInteractive;
            return registered;
        }

        public void Tick(double dt)
        {
            Module?.Tick(dt);
        }

        public void Dispose()
        {
            if (_graph == null)
            {
                return;
            }

            _graph.Lifecycle.OnPaused -= HandlePaused;
            _graph.Lifecycle.OnResumed -= HandleResumed;
            _graph.Lifecycle.OnAdClosed -= HandleAdClosed;
            _graph.Curtain.OnInteractive -= HandleInteractive;
            _graph = null;
        }

        private void HandlePaused(PausedEvent evt)
        {
            Module.HandlePaused(in evt);
        }

        private void HandleResumed(ResumedEvent evt)
        {
            Module.HandleResumed(in evt);
        }

        private void HandleAdClosed(AdClosedEvent evt)
        {
            Module.HandleAdClosed(in evt);
        }

        private void HandleInteractive(InteractiveEvent evt)
        {
            Module.HandleInteractive(in evt);
        }
    }
}
