using System;
using CozyGachapon.Contracts.PetCatalog;

namespace CozyGachapon.Core.PetCatalog
{
    public static class PetPowerFormula
    {
        public const double DefaultBasePower = 10.0;

        public static double GetRoleWeight(PetRole role)
        {
            switch (role)
            {
                case PetRole.GoldBooster:
                    return 1.0;
                case PetRole.Multiplier:
                    return 1.2;
                case PetRole.Spawner:
                    return 1.1;
                case PetRole.SynergyEnabler:
                    return 1.3;
                default:
                    throw new ArgumentOutOfRangeException(nameof(role), role, "Invalid pet role");
            }
        }

        public static double CalculateTargetPower(
            PetRarity rarity,
            PetRole role,
            PetRarityConfig rarityConfig,
            double basePower = DefaultBasePower)
        {
            if (rarityConfig == null)
            {
                throw new ArgumentNullException(nameof(rarityConfig));
            }

            double rarityCoef = rarityConfig.GetCoefficient(rarity);
            double roleWeight = GetRoleWeight(role);
            return basePower * rarityCoef * roleWeight;
        }

        public static double CalculateTargetPower(
            PetRarity rarity,
            PetRole role,
            double basePower = DefaultBasePower)
        {
            double rarityCoef;
            switch (rarity)
            {
                case PetRarity.Common:
                    rarityCoef = PetRarityConfig.CommonCoefficient;
                    break;
                case PetRarity.Rare:
                    rarityCoef = 2.5;
                    break;
                case PetRarity.Epic:
                    rarityCoef = 6.0;
                    break;
                case PetRarity.Legendary:
                    rarityCoef = 15.0;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rarity), rarity, "Invalid pet rarity");
            }

            double roleWeight = GetRoleWeight(role);
            return basePower * rarityCoef * roleWeight;
        }
    }
}
