using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using CozyGachapon.Contracts.PetCatalog;

namespace CozyGachapon.Core.PetCatalog
{
    public static class PetCatalogValidator
    {
        private static readonly Regex IdRegex = new Regex(@"^pet\.[a-z0-9_]+$", RegexOptions.Compiled);

        public static int GetMaxEffects(PetRarity rarity)
        {
            switch (rarity)
            {
                case PetRarity.Common:
                    return 1;
                case PetRarity.Rare:
                case PetRarity.Epic:
                    return 2;
                case PetRarity.Legendary:
                    return 3;
                default:
                    return 0;
            }
        }

        public static bool IsRoleEffectTypeMatch(PetRole role, PetEffectType type)
        {
            switch (role)
            {
                case PetRole.GoldBooster:
                    return type == PetEffectType.FlatGoldPerDrop || type == PetEffectType.PercentGoldBoost;
                case PetRole.Multiplier:
                    return type == PetEffectType.ScoreMultiplier;
                case PetRole.Spawner:
                    return type == PetEffectType.SpawnRateBoost;
                case PetRole.SynergyEnabler:
                    return type == PetEffectType.SynergyCountBonus;
                default:
                    return false;
            }
        }

        public static List<string> Validate(IEnumerable<PetDefinition> pets)
        {
            var errors = new List<string>();
            if (pets == null)
            {
                errors.Add("Pets collection is null.");
                return errors;
            }

            var seenIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (var pet in pets)
            {
                if (pet == null)
                {
                    errors.Add("Encountered null PetDefinition in catalog.");
                    continue;
                }

                if (string.IsNullOrEmpty(pet.Id))
                {
                    errors.Add($"Pet '{pet.name}' has null or empty ID.");
                }
                else
                {
                    if (!IdRegex.IsMatch(pet.Id))
                    {
                        errors.Add($"Pet '{pet.Id}' has invalid ID format. Must match 'pet.[a-z0-9_]+'.");
                    }

                    if (!seenIds.Add(pet.Id))
                    {
                        errors.Add($"Duplicate pet ID detected: '{pet.Id}'.");
                    }
                }

                if (pet.Rarity == PetRarity.None)
                {
                    errors.Add($"Pet '{pet.Id}' has Rarity = None.");
                }
                if (pet.Group == PetGroup.None)
                {
                    errors.Add($"Pet '{pet.Id}' has Group = None.");
                }
                if (pet.Role == PetRole.None)
                {
                    errors.Add($"Pet '{pet.Id}' has Role = None.");
                }
                if (pet.ReleaseTier == PetReleaseTier.None)
                {
                    errors.Add($"Pet '{pet.Id}' has ReleaseTier = None.");
                }

                if (pet.Effects == null || pet.Effects.Length == 0)
                {
                    errors.Add($"Pet '{pet.Id}' has no effects defined (minimum 1 required).");
                    continue;
                }

                int maxEffects = GetMaxEffects(pet.Rarity);
                if (pet.Effects.Length > maxEffects)
                {
                    errors.Add($"Pet '{pet.Id}' exceeds max effects for rarity {pet.Rarity} ({pet.Effects.Length} > {maxEffects}).");
                }

                if (!IsRoleEffectTypeMatch(pet.Role, pet.Effects[0].Type))
                {
                    errors.Add($"Pet '{pet.Id}' primary effect ({pet.Effects[0].Type}) does not match its Role ({pet.Role}).");
                }

                for (int i = 0; i < pet.Effects.Length; i++)
                {
                    var effect = pet.Effects[i];
                    if (effect.Type == PetEffectType.None)
                    {
                        errors.Add($"Pet '{pet.Id}' effect [{i}] has Type = None.");
                    }
                    if (effect.Scope == PetEffectScope.None)
                    {
                        errors.Add($"Pet '{pet.Id}' effect [{i}] has Scope = None.");
                    }
                    if (effect.Scope == PetEffectScope.Both)
                    {
                        if (pet.Rarity != PetRarity.Epic && pet.Rarity != PetRarity.Legendary)
                        {
                            errors.Add($"Pet '{pet.Id}' has Scope.Both which is only allowed for Epic or Legendary.");
                        }
                    }
                }
            }

            return errors;
        }
    }
}
