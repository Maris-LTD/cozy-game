using System;
using System.Collections.Generic;

namespace CozyGachapon.Economy
{
    public enum PlinkoRateModelPreset
    {
        BaseNoAuto = 0,
        AutoFloor = 1,
        BounceCap = 2,
        AutoFloorAndBounceCap = 3
    }

    public sealed class EconomyBalanceModel
    {
        private readonly EconomyConfig _config;
        private readonly EconomyBalanceTargets _targets;
        private readonly SpinPriceCurve _curve;
        private readonly BaseCoinValueCalculator _baseCoinValue;

        public EconomyBalanceModel(EconomyConfig config, EconomyBalanceTargets targets)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _targets = targets ?? throw new ArgumentNullException(nameof(targets));
            _curve = new SpinPriceCurve(config);
            _baseCoinValue = new BaseCoinValueCalculator(config, _curve);
        }

        public double FirstSpinTime(double medianFallTime)
        {
            double coinsNeeded = Math.Max(0d, _config.SpinBasePrice - _config.StartingCoins);
            double denominator = _targets.RealisticTapRate * _targets.PlinkoEvEffectiveAssumed * _baseCoinValue.Compute(0);
            if (!(denominator > 0d))
            {
                return double.PositiveInfinity;
            }

            return (coinsNeeded / denominator) + Math.Max(0d, medianFallTime);
        }

        public double SecondsPerSpin(int n)
        {
            double spinPrice = _curve.Price(n);
            double denominator = _targets.RealisticTapRate * _targets.PlinkoEvEffectiveAssumed * _baseCoinValue.Compute(n);
            if (!(denominator > 0d))
            {
                return double.PositiveInfinity;
            }

            return spinPrice / denominator;
        }

        public double PlinkoRateModel(PlinkoRateModelPreset preset, int n)
        {
            return PlinkoRateModel((int)preset, n);
        }

        public double PlinkoRateModel(int configIndex, int n)
        {
            IReadOnlyList<double> ceilings = _targets.PlinkoRawRateCeilingAssumed;
            if (ceilings == null || configIndex < 0 || configIndex >= ceilings.Count)
            {
                return 0d;
            }

            return ceilings[configIndex] * _baseCoinValue.Compute(n);
        }
    }
}
