using System;
using CozyGachapon.Contracts.PetCatalog;

namespace CozyGachapon.Core.PetCatalog
{
    public static class PetBalanceTargets
    {
        public const double DefaultBasePower = 10.0;
        public const double DefaultTolerance = 0.15;

        public static double GetTargetPower(PetRarity rarity, PetRole role, PetRarityConfig rarityConfig = null)
        {
            if (rarityConfig != null)
            {
                return PetPowerFormula.CalculateTargetPower(rarity, role, rarityConfig, DefaultBasePower);
            }
            return PetPowerFormula.CalculateTargetPower(rarity, role, DefaultBasePower);
        }

        public static double CalculateActualPower(PetDefinition pet)
        {
            if (pet == null || pet.Effects == null)
            {
                return 0.0;
            }

            double totalPower = 0.0;
            for (int i = 0; i < pet.Effects.Length; i++)
            {
                var effect = pet.Effects[i];
                double effectBaseWeight;

                switch (effect.Type)
                {
                    case PetEffectType.FlatGoldPerDrop:
                        effectBaseWeight = 10.0;
                        break;
                    case PetEffectType.PercentGoldBoost:
                        effectBaseWeight = 100.0;
                        break;
                    case PetEffectType.ScoreMultiplier:
                        effectBaseWeight = 10.0;
                        break;
                    case PetEffectType.SpawnRateBoost:
                        effectBaseWeight = 40.0;
                        break;
                    case PetEffectType.SynergyCountBonus:
                        effectBaseWeight = 20.0;
                        break;
                    default:
                        effectBaseWeight = 1.0;
                        break;
                }

                double scopeMultiplier;
                switch (effect.Scope)
                {
                    case PetEffectScope.Self:
                        scopeMultiplier = 1.0;
                        break;
                    case PetEffectScope.Group:
                        scopeMultiplier = 1.25;
                        break;
                    case PetEffectScope.Board:
                        scopeMultiplier = 1.5;
                        break;
                    case PetEffectScope.Both:
                        scopeMultiplier = 1.75;
                        break;
                    default:
                        scopeMultiplier = 1.0;
                        break;
                }

                totalPower += effect.Magnitude * effectBaseWeight * scopeMultiplier;
            }

            return totalPower;
        }

        public static bool IsWithinBudget(PetDefinition pet, PetRarityConfig rarityConfig = null, double tolerance = DefaultTolerance)
        {
            if (pet == null)
            {
                return false;
            }

            double target = GetTargetPower(pet.Rarity, pet.Role, rarityConfig);
            double actual = CalculateActualPower(pet);

            double minAllowed = target * (1.0 - tolerance);
            double maxAllowed = target * (1.0 + tolerance);

            return actual >= minAllowed && actual <= maxAllowed;
        }
    }
}
