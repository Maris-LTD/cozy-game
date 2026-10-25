using System;

namespace CozyGachapon.Contracts
{
    public interface IWallet
    {
        event Action<WalletChangedEvent> WalletChanged;

        bool Add(string currencyId, double amount, WalletChangeReason reason);

        double GetBalance(string currencyId);
    }
}
