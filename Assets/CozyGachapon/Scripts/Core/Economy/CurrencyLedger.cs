using System;
using System.Collections.Generic;

namespace CozyGachapon.Economy
{
    internal sealed class CurrencyLedger
    {
        private readonly Dictionary<string, double> _balances = new Dictionary<string, double>(StringComparer.Ordinal);

        internal bool TryRegister(string currencyId)
        {
            if (string.IsNullOrEmpty(currencyId) || _balances.ContainsKey(currencyId))
            {
                return false;
            }

            _balances.Add(currencyId, 0d);
            return true;
        }

        internal bool TryGet(string currencyId, out double balance)
        {
            if (string.IsNullOrEmpty(currencyId))
            {
                balance = 0d;
                return false;
            }

            return _balances.TryGetValue(currencyId, out balance);
        }

        internal void Set(string currencyId, double balance)
        {
            _balances[currencyId] = balance;
        }
    }
}
