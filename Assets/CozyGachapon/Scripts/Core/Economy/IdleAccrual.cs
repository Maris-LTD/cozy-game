using System;
using CozyGachapon.Contracts;

namespace CozyGachapon.Economy
{
    public sealed class IdleAccrual
    {
        private readonly IWallet _wallet;
        private readonly IIncomePipeline _pipeline;
        private readonly EconomyConfig _config;
        private readonly EconomyLifecycle _lifecycle;

        public IdleAccrual(
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

        public double Tick(double dt)
        {
            if (!_lifecycle.CanAccrueIdle)
            {
                return 0d;
            }

            double clampedDt = ClampDelta(dt, _config.MaxFrameDelta);
            if (clampedDt <= 0d)
            {
                return 0d;
            }

            double cps = _pipeline.IdleCps;
            if (!double.IsNaN(cps) && !double.IsInfinity(cps) && cps > 0d)
            {
                double amount = cps * clampedDt;
                if (!double.IsNaN(amount) && !double.IsInfinity(amount) && amount > 0d)
                {
                    _wallet.Add(EconomyIds.Coin, amount, WalletChangeReason.IdleIncome);
                }
            }

            return clampedDt;
        }

        public static double ClampDelta(double dt, double maxDelta)
        {
            if (double.IsNaN(dt) || double.IsInfinity(dt) || dt <= 0d)
            {
                return 0d;
            }

            if (double.IsNaN(maxDelta) || double.IsInfinity(maxDelta) || maxDelta <= 0d)
            {
                return 0d;
            }

            return dt > maxDelta ? maxDelta : dt;
        }
    }
}
