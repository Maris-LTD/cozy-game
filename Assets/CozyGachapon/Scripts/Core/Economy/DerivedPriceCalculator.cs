using System;

namespace CozyGachapon.Economy
{
    public sealed class DerivedPriceCalculator
    {
        private readonly SpinPriceCurve _curve;

        public DerivedPriceCalculator(SpinPriceCurve curve)
        {
            _curve = curve ?? throw new ArgumentNullException(nameof(curve));
        }

        public bool TryGet(double coef, int n, out double price)
        {
            price = 0d;
            if (!(coef > 0d) || double.IsPositiveInfinity(coef))
            {
                return false;
            }

            double candidate = _curve.ToNicePrice(coef * _curve.RawPrice(n));
            if (!(candidate > 0d))
            {
                return false;
            }

            price = candidate;
            return true;
        }
    }
}
