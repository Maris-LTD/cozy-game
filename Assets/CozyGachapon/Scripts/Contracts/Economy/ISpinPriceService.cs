using System;

namespace CozyGachapon.Contracts
{
    public interface ISpinPriceService
    {
        event Action<SpinPriceChangedEvent> SpinPriceChanged;
        event Action<ShopEconomyResetEvent> ShopEconomyReset;

        int SpinsThisShop { get; }
        double SpinPrice { get; }
        double BaseCoinValue { get; }

        double GetSpinPrice();
        double GetDerivedPrice(double priceCoef);
        bool TryGetDerivedPrice(double priceCoef, out double price);
        bool CanAffordSpin();
        bool TryPaySpin(in PlayerActionContext context);
        bool ResetForNewShop(in PlayerActionContext context);
    }
}
