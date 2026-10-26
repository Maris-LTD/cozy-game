namespace CozyGachapon.Contracts
{
    public readonly struct SpinPriceChangedEvent
    {
        public int SpinsThisShop { get; }
        public double SpinPrice { get; }
        public double BaseCoinValue { get; }

        public SpinPriceChangedEvent(int spinsThisShop, double spinPrice, double baseCoinValue)
        {
            SpinsThisShop = spinsThisShop;
            SpinPrice = spinPrice;
            BaseCoinValue = baseCoinValue;
        }
    }
}
