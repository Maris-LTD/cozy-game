using System;
using UnityEngine;

namespace CozyGachapon.Contracts
{
    [Serializable]
    public struct CurrencyBalanceEntry
    {
        [SerializeField] private string _currencyId;
        [SerializeField] private double _amount;

        public CurrencyBalanceEntry(string currencyId, double amount)
        {
            _currencyId = currencyId;
            _amount = amount;
        }

        public string CurrencyId => _currencyId;
        public double Amount => _amount;
    }
}
