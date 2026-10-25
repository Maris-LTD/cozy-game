using System;
using System.Collections.Generic;
using CozyGachapon.Contracts;

namespace CozyGachapon.Economy
{
    public static class EconomyCurrencyValidator
    {
        public static void Validate(IReadOnlyList<CurrencyDefinition> currencies, List<EconomyConfigIssue> results)
        {
            if (currencies == null || currencies.Count == 0)
            {
                results.Add(EconomyConfigIssue.ForSubject(EconomyConfigIssueCode.CurrenciesEmpty, null));
                return;
            }

            bool hasCoin = false;
            for (int i = 0; i < currencies.Count; i++)
            {
                CurrencyDefinition currency = currencies[i];
                if (currency == null)
                {
                    results.Add(EconomyConfigIssue.ForIndex(EconomyConfigIssueCode.CurrencyMissing, i));
                    continue;
                }

                if (string.IsNullOrEmpty(currency.Id))
                {
                    results.Add(EconomyConfigIssue.ForIndex(EconomyConfigIssueCode.CurrencyIdMissing, i));
                    continue;
                }

                if (string.Equals(currency.Id, EconomyIds.Coin, StringComparison.Ordinal))
                {
                    hasCoin = true;
                }

                if (IsDuplicateOfEarlier(currencies, i))
                {
                    results.Add(EconomyConfigIssue.ForSubject(EconomyConfigIssueCode.DuplicateCurrencyId, currency.Id));
                }
            }

            if (!hasCoin)
            {
                results.Add(EconomyConfigIssue.ForSubject(EconomyConfigIssueCode.CoinCurrencyMissing, EconomyIds.Coin));
            }
        }

        private static bool IsDuplicateOfEarlier(IReadOnlyList<CurrencyDefinition> currencies, int index)
        {
            string id = currencies[index].Id;
            for (int i = 0; i < index; i++)
            {
                CurrencyDefinition other = currencies[i];
                if (other != null && string.Equals(other.Id, id, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
