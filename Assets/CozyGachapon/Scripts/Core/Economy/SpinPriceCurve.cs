using System;

namespace CozyGachapon.Economy
{
    public sealed class SpinPriceCurve
    {
        private readonly EconomyConfig _config;

        public SpinPriceCurve(EconomyConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public double RawPrice(int n)
        {
            int safeN = n < 0 ? 0 : n;
            return _config.SpinBasePrice * Math.Pow(_config.SpinGrowth, safeN);
        }

        public double Price(int n)
        {
            return ToNicePrice(RawPrice(n));
        }

        internal double ToNicePrice(double raw)
        {
            double cap = _config.WalletSafeCap;
            if (!(raw <= cap))
            {
                return cap;
            }

            double nice = NiceCeiling.Apply(raw, _config.PriceNiceSigDigits);
            return nice > cap ? cap : nice;
        }
    }
}
