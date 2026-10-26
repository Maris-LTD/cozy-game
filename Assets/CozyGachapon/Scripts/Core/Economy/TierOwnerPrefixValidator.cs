using System;
using System.Collections.Generic;
using CozyGachapon.Contracts;

namespace CozyGachapon.Economy
{
    public static class TierOwnerPrefixValidator
    {
        public static void Validate(TierOwnerPrefixes prefixes, List<EconomyConfigIssue> results)
        {
            if (prefixes == null)
            {
                results.Add(EconomyConfigIssue.ForSubject(EconomyConfigIssueCode.TierPrefixListEmpty, null));
                return;
            }

            ValidateTier(prefixes.Base, ModifierTier.Base, results);
            ValidateTier(prefixes.Synergy, ModifierTier.Synergy, results);
            ValidateTier(prefixes.Boost, ModifierTier.Boost, results);
            ValidateTier(prefixes.Prestige, ModifierTier.Prestige, results);
            ValidateTier(prefixes.Global, ModifierTier.Global, results);
        }

        private static void ValidateTier(IReadOnlyList<string> list, ModifierTier tier, List<EconomyConfigIssue> results)
        {
            string tierName = tier.ToString();
            if (list == null || list.Count == 0)
            {
                results.Add(EconomyConfigIssue.ForSubject(EconomyConfigIssueCode.TierPrefixListEmpty, tierName));
                return;
            }

            bool isElevated = tier == ModifierTier.Boost || tier == ModifierTier.Prestige || tier == ModifierTier.Global;
            for (int i = 0; i < list.Count; i++)
            {
                string prefix = list[i];
                if (string.IsNullOrEmpty(prefix))
                {
                    results.Add(EconomyConfigIssue.ForSubject(EconomyConfigIssueCode.TierPrefixListEmpty, tierName));
                    continue;
                }

                if (isElevated && IsForbiddenPrefix(prefix))
                {
                    results.Add(EconomyConfigIssue.ForSubject(EconomyConfigIssueCode.TierPrefixForbidden, tierName));
                }
            }
        }

        private static bool IsForbiddenPrefix(string prefix)
        {
            return string.Equals(prefix, "shelf.", StringComparison.Ordinal)
                || string.Equals(prefix, "pet.", StringComparison.Ordinal);
        }
    }
}
