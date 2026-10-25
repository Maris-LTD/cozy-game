using System;

namespace CozyGachapon.Contracts
{
    public interface IWallet
    {
        event Action<WalletChangedEvent> WalletChanged;
        event Action<SpendRejectedEvent> SpendRejected;

        bool Add(string currencyId, double amount, WalletChangeReason reason);

        bool TrySpend(string currencyId, double amount, SpendReason reason, in PlayerActionContext context);

        bool CanAfford(string currencyId, double amount);

        double GetBalance(string currencyId);
    }
}
