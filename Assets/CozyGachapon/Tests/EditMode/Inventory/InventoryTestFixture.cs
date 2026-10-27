using System.Collections.Generic;
using UnityEngine;
using CozyGachapon.Contracts.PetCatalog;
using CozyGachapon.Core.PetCatalog;

namespace CozyGachapon.Tests.EditMode.Inventory
{
    public sealed class InventoryTestFixture
    {
        private readonly List<PetDefinition> createdPets = new List<PetDefinition>();

        public PetDefinition CreatePet(
            string id,
            PetRarity rarity,
            PetGroup group,
            PetRole role = PetRole.GoldBooster,
            bool isDeprecated = false)
        {
            var pet = ScriptableObject.CreateInstance<PetDefinition>();
            pet.name = id;
            pet.SetData(
                id: id,
                displayNameKey: id + ".name",
                rarity: rarity,
                group: group,
                role: role,
                releaseTier: PetReleaseTier.Mvp,
                effects: new[] { new PetEffect(PetEffectType.FlatGoldPerDrop, PetEffectScope.Self, 1.0) },
                isDeprecated: isDeprecated);
            createdPets.Add(pet);
            return pet;
        }

        public PetDatabase CreateDefaultCatalog()
        {
            var pA = CreatePet("pet.a", PetRarity.Common, PetGroup.Woodland);
            var pB = CreatePet("pet.b", PetRarity.Rare, PetGroup.Sweets);
            var pC = CreatePet("pet.c", PetRarity.Legendary, PetGroup.Savanna);
            return new PetDatabase(new[] { pA, pB, pC });
        }

        public void Dispose()
        {
            for (int i = 0; i < createdPets.Count; i++)
            {
                if (createdPets[i] != null)
                {
                    Object.DestroyImmediate(createdPets[i]);
                }
            }
            createdPets.Clear();
        }
    }
}
