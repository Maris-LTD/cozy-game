using System;

namespace CozyGachapon.Economy
{
    internal sealed class SpinPriceState
    {
        private readonly SpinPriceCurve _curve;
        private readonly BaseCoinValueCalculator _baseCoinValueCalculator;

        public int SpinsThisShop { get; private set; }
        public double SpinPrice { get; private set; }
        public double BaseCoinValue { get; private set; }

        public SpinPriceState(SpinPriceCurve curve, BaseCoinValueCalculator baseCoinValueCalculator)
        {
            _curve = curve ?? throw new ArgumentNullException(nameof(curve));
            _baseCoinValueCalculator = baseCoinValueCalculator ?? throw new ArgumentNullException(nameof(baseCoinValueCalculator));
            SetSpins(0);
        }

        public void SetSpins(int spins)
        {
            SpinsThisShop = Math.Max(0, spins);
            SpinPrice = _curve.Price(SpinsThisShop);
            BaseCoinValue = _baseCoinValueCalculator.Compute(SpinsThisShop);
        }

        public void AdvanceSpin()
        {
            SetSpins(SpinsThisShop + 1);
        }

        public void Reset()
        {
            SetSpins(0);
        }
    }
}
