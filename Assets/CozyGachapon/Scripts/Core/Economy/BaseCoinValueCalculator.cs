using System;

namespace CozyGachapon.Economy
{
    public sealed class BaseCoinValueCalculator
    {
        private readonly EconomyConfig _config;
        private readonly SpinPriceCurve _curve;

        public BaseCoinValueCalculator(EconomyConfig config, SpinPriceCurve curve)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _curve = curve ?? throw new ArgumentNullException(nameof(curve));
        }

        public double Compute(int n)
        {
            double ratio = _curve.RawPrice(n) / _config.SpinBasePrice;
            double value = Math.Pow(ratio, _config.CoinValueExponent);
            if (!(value >= 1d))
            {
                return 1d;
            }

            double cap = _config.WalletSafeCap;
            return value > cap ? cap : value;
        }
    }
}
