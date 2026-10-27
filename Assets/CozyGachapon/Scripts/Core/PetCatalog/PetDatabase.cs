using System;
using System.Collections.Generic;
using CozyGachapon.Contracts.PetCatalog;

namespace CozyGachapon.Core.PetCatalog
{
    public sealed class PetDatabase : IPetDatabase
    {
        private readonly Dictionary<string, PetDefinition> petsById;
        private readonly List<PetDefinition> allPetsList;
        private readonly Dictionary<PetGroup, List<PetDefinition>> petsByGroup;
        private readonly Dictionary<PetRarity, List<PetDefinition>> petsByRarity;
        private readonly Dictionary<PetRole, List<PetDefinition>> petsByRole;
        private readonly Dictionary<PetReleaseTier, List<PetDefinition>> petsByTier;
        private readonly PetRarityConfig rarityConfig;

        private static readonly PetDefinition[] EmptyArray = Array.Empty<PetDefinition>();

        public PetDatabase(IEnumerable<PetDefinition> pets, PetRarityConfig rarityConfig = null)
        {
            this.rarityConfig = rarityConfig;
            petsById = new Dictionary<string, PetDefinition>(StringComparer.Ordinal);
            allPetsList = new List<PetDefinition>();
            petsByGroup = new Dictionary<PetGroup, List<PetDefinition>>();
            petsByRarity = new Dictionary<PetRarity, List<PetDefinition>>();
            petsByRole = new Dictionary<PetRole, List<PetDefinition>>();
            petsByTier = new Dictionary<PetReleaseTier, List<PetDefinition>>();

            if (pets == null)
            {
                return;
            }

            foreach (var pet in pets)
            {
                if (pet == null || string.IsNullOrEmpty(pet.Id))
                {
                    continue;
                }

                if (petsById.ContainsKey(pet.Id))
                {
                    throw new InvalidOperationException($"Duplicate pet id detected: {pet.Id}");
                }

                petsById.Add(pet.Id, pet);
                allPetsList.Add(pet);

                AddToBucket(petsByGroup, pet.Group, pet);
                AddToBucket(petsByRarity, pet.Rarity, pet);
                AddToBucket(petsByRole, pet.Role, pet);
                AddToBucket(petsByTier, pet.ReleaseTier, pet);
            }
        }

        public int Count => allPetsList.Count;
        public IReadOnlyList<PetDefinition> AllPets => allPetsList;
        public PetRarityConfig RarityConfig => rarityConfig;

        public PetDefinition Get(string id)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            if (petsById.TryGetValue(id, out var pet))
            {
                return pet;
            }

            throw new KeyNotFoundException($"Pet with id '{id}' was not found in PetDatabase.");
        }

        public bool TryGet(string id, out PetDefinition pet)
        {
            if (id == null)
            {
                pet = null;
                return false;
            }

            return petsById.TryGetValue(id, out pet);
        }

        public IReadOnlyList<PetDefinition> GetByGroup(PetGroup group)
        {
            if (petsByGroup.TryGetValue(group, out var list))
            {
                return list;
            }
            return EmptyArray;
        }

        public IReadOnlyList<PetDefinition> GetByRarity(PetRarity rarity)
        {
            if (petsByRarity.TryGetValue(rarity, out var list))
            {
                return list;
            }
            return EmptyArray;
        }

        public IReadOnlyList<PetDefinition> GetByRole(PetRole role)
        {
            if (petsByRole.TryGetValue(role, out var list))
            {
                return list;
            }
            return EmptyArray;
        }

        public IReadOnlyList<PetDefinition> GetByTier(PetReleaseTier tier)
        {
            if (petsByTier.TryGetValue(tier, out var list))
            {
                return list;
            }
            return EmptyArray;
        }

        private static void AddToBucket<TKey>(Dictionary<TKey, List<PetDefinition>> dict, TKey key, PetDefinition pet)
        {
            if (!dict.TryGetValue(key, out var list))
            {
                list = new List<PetDefinition>();
                dict[key] = list;
            }
            list.Add(pet);
        }
    }
}
