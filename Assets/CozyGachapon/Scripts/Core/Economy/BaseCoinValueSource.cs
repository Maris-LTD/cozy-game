using System;
using CozyGachapon.Contracts;

namespace CozyGachapon.Economy
{
    public sealed class BaseCoinValueSource : IBaseCoinValueSource
    {
        private readonly SpinPriceState _state;

        internal BaseCoinValueSource(SpinPriceState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public double BaseCoinValue
        {
            get
            {
                double value = _state.BaseCoinValue;
                return value >= 1d ? value : 1d;
            }
        }
    }
}
