using System.Collections.Generic;
using CozyGachapon.Contracts.PetCatalog;

namespace CozyGachapon.Core.PetCatalog
{
    public interface IPetDatabase
    {
        int Count { get; }
        IReadOnlyList<PetDefinition> AllPets { get; }
        PetRarityConfig RarityConfig { get; }
        PetDefinition Get(string id);
        bool TryGet(string id, out PetDefinition pet);
        IReadOnlyList<PetDefinition> GetByGroup(PetGroup group);
        IReadOnlyList<PetDefinition> GetByRarity(PetRarity rarity);
        IReadOnlyList<PetDefinition> GetByRole(PetRole role);
        IReadOnlyList<PetDefinition> GetByTier(PetReleaseTier tier);
    }
}
