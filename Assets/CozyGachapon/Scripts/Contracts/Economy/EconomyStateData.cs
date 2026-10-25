using System;
using System.Collections.Generic;
using UnityEngine;

namespace CozyGachapon.Contracts
{
    [Serializable]
    public sealed class EconomyStateData
    {
        [SerializeField] private CurrencyBalanceEntry[] _balances;
        [SerializeField] private int _spinsThisShop;

        public EconomyStateData(CurrencyBalanceEntry[] balances, int spinsThisShop)
        {
            _balances = balances == null ? null : (CurrencyBalanceEntry[])balances.Clone();
            _spinsThisShop = spinsThisShop;
        }

        public IReadOnlyList<CurrencyBalanceEntry> Balances => _balances == null ? null : Array.AsReadOnly(_balances);
        public int SpinsThisShop => _spinsThisShop;
    }
}
