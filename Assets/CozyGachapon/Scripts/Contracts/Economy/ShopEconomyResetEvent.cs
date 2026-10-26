using System.Collections.Generic;

namespace CozyGachapon.Contracts
{
    public readonly struct ShopEconomyResetEvent
    {
        public IReadOnlyList<string> ResetCurrencyIds { get; }
        public int SpinsThisShop { get; }

        public ShopEconomyResetEvent(IReadOnlyList<string> resetCurrencyIds, int spinsThisShop)
        {
            ResetCurrencyIds = resetCurrencyIds;
            SpinsThisShop = spinsThisShop;
        }
    }
}
