using System;
using System.Collections.Generic;
using CozyGachapon.Contracts.PetCatalog;

namespace CozyGachapon.Contracts.Inventory
{
    public interface IInventoryService
    {
        InventoryState State { get; }
        int OwnedSpeciesCount { get; }
        long TotalCopies { get; }
        int ReleasedSpeciesCount { get; }
        double CollectionProgress { get; }

        event Action<PetAcquiredEvent> OnPetAcquired;

        AddPetResult AddPet(string id, int amount = 1);
        bool IsOwned(string id);
        int GetCopies(string id);
        int GetOwnedSpecies(List<string> buffer, PetGroup? group = null, PetRarity? rarity = null);
        int GetUnownedReleased(List<string> buffer);
        PetCopyEntry[] ExportSnapshot();
    }
}
