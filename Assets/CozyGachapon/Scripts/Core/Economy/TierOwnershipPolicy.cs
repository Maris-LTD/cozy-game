using System;
using System.Collections.Generic;
using CozyGachapon.Contracts;

namespace CozyGachapon.Economy
{
    public sealed class TierOwnershipPolicy
    {
        private readonly TierOwnerPrefixes _prefixes;

        public TierOwnershipPolicy(TierOwnerPrefixes prefixes)
        {
            _prefixes = prefixes;
        }

        public bool IsAllowed(string sourceId, ModifierTier tier)
        {
            if (string.IsNullOrEmpty(sourceId) || _prefixes == null)
            {
                return false;
            }

            IReadOnlyList<string> prefixes = GetPrefixesForTier(tier);
            if (prefixes == null || prefixes.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < prefixes.Count; i++)
            {
                string prefix = prefixes[i];
                if (string.IsNullOrEmpty(prefix))
                {
                    continue;
                }

                if (sourceId.StartsWith(prefix, StringComparison.Ordinal))
                {
                    if (IsElevatedTier(tier) && IsPetOrShelfPrefix(prefix))
                    {
                        return false;
                    }

                    return true;
                }
            }

            return false;
        }

        private IReadOnlyList<string> GetPrefixesForTier(ModifierTier tier)
        {
            switch (tier)
            {
                case ModifierTier.Base:
                    return _prefixes.Base;
                case ModifierTier.Synergy:
                    return _prefixes.Synergy;
                case ModifierTier.Boost:
                    return _prefixes.Boost;
                case ModifierTier.Prestige:
                    return _prefixes.Prestige;
                case ModifierTier.Global:
                    return _prefixes.Global;
                default:
                    return null;
            }
        }

        private static bool IsElevatedTier(ModifierTier tier)
        {
            return tier == ModifierTier.Boost || tier == ModifierTier.Prestige || tier == ModifierTier.Global;
        }

        private static bool IsPetOrShelfPrefix(string prefix)
        {
            return string.Equals(prefix, "shelf.", StringComparison.Ordinal)
                || string.Equals(prefix, "pet.", StringComparison.Ordinal);
        }
    }
}
