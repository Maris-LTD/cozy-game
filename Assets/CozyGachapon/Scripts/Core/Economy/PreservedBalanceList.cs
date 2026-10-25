using System.Collections.Generic;
using CozyGachapon.Contracts;

namespace CozyGachapon.Economy
{
    internal sealed class PreservedBalanceList
    {
        private readonly List<CurrencyBalanceEntry> _entries = new List<CurrencyBalanceEntry>();

        internal int Count => _entries.Count;

        internal CurrencyBalanceEntry Get(int index)
        {
            return _entries[index];
        }

        internal void Set(string currencyId, double amount)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (string.Equals(_entries[i].CurrencyId, currencyId, System.StringComparison.Ordinal))
                {
                    _entries[i] = new CurrencyBalanceEntry(currencyId, amount);
                    return;
                }
            }

            _entries.Add(new CurrencyBalanceEntry(currencyId, amount));
        }
    }
}
