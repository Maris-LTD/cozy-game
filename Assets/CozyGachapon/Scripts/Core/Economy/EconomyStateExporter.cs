using System.Collections.Generic;
using CozyGachapon.Contracts;

namespace CozyGachapon.Economy
{
    internal static class EconomyStateExporter
    {
        internal static EconomyStateData Export(
            IReadOnlyList<string> registeredIds,
            WalletService wallet,
            PreservedBalanceList preserved,
            int spinsThisShop)
        {
            int registeredCount = registeredIds.Count;
            CurrencyBalanceEntry[] entries = new CurrencyBalanceEntry[registeredCount + preserved.Count];
            for (int i = 0; i < registeredCount; i++)
            {
                string id = registeredIds[i];
                entries[i] = new CurrencyBalanceEntry(id, wallet.GetBalance(id));
            }

            for (int i = 0; i < preserved.Count; i++)
            {
                entries[registeredCount + i] = preserved.Get(i);
            }

            return new EconomyStateData(entries, spinsThisShop);
        }
    }
}
