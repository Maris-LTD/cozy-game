using System;
using CozyGachapon.Contracts;

namespace CozyGachapon.Economy
{
    public sealed class EconomyBootModule
    {
        public EconomyCore Core { get; }
        public EconomyBootStep BootStep { get; }
        public NumberFormatter Formatter { get; }

        public EconomyBootModule(
            EconomyConfig config,
            IEconomyDiagnosticsSink sink,
            IServiceRegistry serviceRegistry,
            ISimulationState simulationState = null,
            ReleaseTier buildTier = ReleaseTier.Mvp,
            EconomyStateData loaded = null)
        {
            Core = new EconomyCore(sink ?? new EconomyDiagnosticsRecorder(), simulationState);
            BootStep = new EconomyBootStep(Core, config, serviceRegistry, simulationState, buildTier, loaded);
            if (config != null)
            {
                Formatter = new NumberFormatter(config);
            }
        }

        public bool RegisterSteps(Func<IBootStep, bool> register)
        {
            return register != null && register(BootStep);
        }

        public double Tick(double dt)
        {
            return Core.Tick(dt);
        }

        public void HandleInteractive(in InteractiveEvent evt)
        {
            Core.HandleInteractive(in evt);
        }

        public void HandlePaused(in PausedEvent evt)
        {
            Core.HandlePaused(in evt);
        }

        public void HandleResumed(in ResumedEvent evt)
        {
            Core.HandleResumed(in evt);
        }

        public double HandleAdClosed(in AdClosedEvent evt)
        {
            return Core.HandleAdClosed(in evt);
        }
    }
}
