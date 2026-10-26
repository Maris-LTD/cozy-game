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

        public bool TryGet(double coef, int n, out double price) => TryCompute(_curve, coef, n, out price);

        internal static bool TryCompute(SpinPriceCurve curve, double coef, int n, out double price)
        {
            price = 0d;
            if (curve == null || !(coef > 0d) || double.IsPositiveInfinity(coef))
            {
                return false;
            }

            double candidate = curve.ToNicePrice(coef * curve.RawPrice(n));
            if (!(candidate > 0d))
            {
                return false;
            }

            price = candidate;
            return true;
        }
    }
}
