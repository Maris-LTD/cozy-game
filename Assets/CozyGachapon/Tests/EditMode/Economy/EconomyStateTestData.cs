using System;
using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Economy
{
    public static class EconomyStateTestData
    {
        public const string FutureId = "currency.future";

        public static EconomyStateData Build(int spins, params CurrencyBalanceEntry[] entries)
        {
            return new EconomyStateData(entries, spins);
        }

        public static EconomyConfig CreateConfigWithCurrencies(double startingCoins)
        {
            EconomyConfig config = ScriptableObject.CreateInstance<EconomyConfig>();
            config.SetCurrenciesForTest(new[] { EconomyTestAssets.LoadCoin(), EconomyTestAssets.LoadShopStamp() });
            config.SetStartingCoinsForTest(startingCoins);
            return config;
        }

        public static bool TryFind(EconomyStateData state, string currencyId, out double amount)
        {
            for (int i = 0; i < state.Balances.Count; i++)
            {
                if (string.Equals(state.Balances[i].CurrencyId, currencyId, StringComparison.Ordinal))
                {
                    amount = state.Balances[i].Amount;
                    return true;
                }
            }

            amount = 0d;
            return false;
        }

        public static double Find(EconomyStateData state, string currencyId)
        {
            Assert.IsTrue(TryFind(state, currencyId, out double amount), currencyId);
            return amount;
        }

        public static void AssertExactlyEqual(double expected, double actual)
        {
            Assert.AreEqual(BitConverter.DoubleToInt64Bits(expected), BitConverter.DoubleToInt64Bits(actual));
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
