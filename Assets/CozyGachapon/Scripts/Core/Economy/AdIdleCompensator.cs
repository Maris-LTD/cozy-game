using System;
using CozyGachapon.Contracts;

namespace CozyGachapon.Economy
{
    public sealed class AdIdleCompensator
    {
        private const double ZeroTolerance = 1e-9d;
        private readonly IWallet _wallet;
        private readonly IIncomePipeline _pipeline;
        private readonly EconomyConfig _config;
        private readonly EconomyLifecycle _lifecycle;
        private readonly AdSimulatedTimeTracker _tracker = new AdSimulatedTimeTracker();

        public AdIdleCompensator(
            IWallet wallet,
            IIncomePipeline pipeline,
            EconomyConfig config,
            EconomyLifecycle lifecycle)
        {
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
        }

        public double SimulatedSeconds => _tracker.SimulatedSeconds;

        public void RecordStep(double clampedDt)
        {
            _tracker.RecordStep(clampedDt, _lifecycle.IsAdOpen);
        }

        public double HandleAdClosed(in AdClosedEvent evt)
        {
            double simulated = _tracker.ConsumeAndReset();
            if (!_lifecycle.CanAccrueIdle)
            {
                return 0d;
            }

            if (double.IsNaN(evt.AdSeconds) || double.IsInfinity(evt.AdSeconds) || evt.AdSeconds <= 0d)
            {
                return 0d;
            }

            double sanitizedAd = ValueSanitizer.Sanitize(evt.AdSeconds);
            double remaining = sanitizedAd - simulated;
            if (remaining <= ZeroTolerance)
            {
                return 0d;
            }

            double maxSeconds = ValueSanitizer.Sanitize(_config.AdCompensationMaxSeconds);
            if (maxSeconds <= 0d)
            {
                return 0d;
            }

            double clampedSeconds = remaining > maxSeconds ? maxSeconds : remaining;
            double cps = _pipeline.IdleCps;
            if (double.IsNaN(cps) || double.IsInfinity(cps) || cps <= 0d)
            {
                return 0d;
            }

            double amount = cps * clampedSeconds;
            if (double.IsNaN(amount) || double.IsInfinity(amount) || amount <= 0d)
            {
                return 0d;
            }

            _wallet.Add(EconomyIds.Coin, amount, WalletChangeReason.AdIdleCompensation);
            return amount;
        }
    }
}
