using System;
using System.Collections.Generic;
using CozyGachapon.Contracts.PetCatalog;

namespace CozyGachapon.Core.PetCatalog
{
    public static class PetBalanceValidator
    {
        public static List<string> ValidateBudgets(IEnumerable<PetDefinition> pets, PetRarityConfig rarityConfig = null, double tolerance = PetBalanceTargets.DefaultTolerance)
        {
            var errors = new List<string>();
            if (pets == null)
            {
                errors.Add("Pets collection is null.");
                return errors;
            }

            foreach (var pet in pets)
            {
                if (pet == null)
                {
                    continue;
                }

                double target = PetBalanceTargets.GetTargetPower(pet.Rarity, pet.Role, rarityConfig);
                double actual = PetBalanceTargets.CalculateActualPower(pet);

                double minAllowed = target * (1.0 - tolerance);
                double maxAllowed = target * (1.0 + tolerance);

                if (actual < minAllowed || actual > maxAllowed)
                {
                    errors.Add($"Pet '{pet.Id}' is out of balance budget. Actual power {actual:F1} is outside allowed range [{minAllowed:F1}, {maxAllowed:F1}] (Target: {target:F1}).");
                }
            }

            return errors;
        }
    }
}
