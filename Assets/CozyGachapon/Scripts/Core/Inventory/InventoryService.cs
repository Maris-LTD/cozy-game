using System;
using System.Collections.Generic;
using CozyGachapon.Contracts.Inventory;
using CozyGachapon.Contracts.PetCatalog;
using CozyGachapon.Core.PetCatalog;

namespace CozyGachapon.Core.Inventory
{
    public sealed class InventoryService : IInventoryService
    {
        private readonly IPetDatabase catalog;
        private readonly InventoryConfig config;
        private readonly Dictionary<string, int> ownedCopies = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> orphanCopies = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly List<string> sortedOwnedIds = new List<string>();
        private readonly PetDisplayComparer displayComparer;
        private readonly InventoryEventDispatcher dispatcher;

        private long totalCopiesCache;
        private int releasedOwnedCount;
        private int releasedSpeciesCountCache;

        public InventoryService(IPetDatabase catalog, InventoryConfig config = null)
        {
            this.catalog = catalog;
            this.config = config;
            bool descending = config == null || config.RarityOrderDescending;
            displayComparer = new PetDisplayComparer(catalog, descending);
            int maxQueue = config != null && config.MaxQueuedEvents > 0 ? config.MaxQueuedEvents : 64;
            dispatcher = new InventoryEventDispatcher(maxQueue);
            State = InventoryState.Uninitialized;
        }

        public InventoryState State { get; private set; }
        public int OwnedSpeciesCount => State == InventoryState.Ready ? ownedCopies.Count : 0;
        public long TotalCopies => State == InventoryState.Ready ? totalCopiesCache : 0L;
        public int ReleasedSpeciesCount => State == InventoryState.Ready ? releasedSpeciesCountCache : 0;
        public double CollectionProgress => State == InventoryState.Ready
            ? InventoryMath.CalculateCollectionProgress(releasedOwnedCount, releasedSpeciesCountCache)
            : 0.0;

        public event Action<PetAcquiredEvent> OnPetAcquired
        {
            add => dispatcher.OnPetAcquired += value;
            remove => dispatcher.OnPetAcquired -= value;
        }

        public bool Initialize(IReadOnlyList<PetCopyEntry> savedEntries = null)
        {
            ownedCopies.Clear();
            orphanCopies.Clear();
            sortedOwnedIds.Clear();
            dispatcher.Clear();
            totalCopiesCache = 0L;
            releasedOwnedCount = 0;
            releasedSpeciesCountCache = 0;

            if (catalog == null)
            {
                State = InventoryState.Failed;
                return false;
            }

            CountReleasedSpecies();
            if (savedEntries != null)
            {
                LoadEntries(savedEntries, allowOrphans: true);
            }
            else if (config != null && config.StartingPets != null && config.StartingPets.Count > 0)
            {
                LoadEntries(config.StartingPets, allowOrphans: false);
            }

            RebuildSortedList();
            State = InventoryState.Ready;
            return true;
        }

        public void MarkFailed() => State = InventoryState.Failed;

        public AddPetResult AddPet(string id, int amount = 1)
        {
            if (State != InventoryState.Ready) return AddPetResult.Rejected(AddPetRejectReason.NotReady);
            if (amount < 1) return AddPetResult.Rejected(AddPetRejectReason.InvalidAmount);
            if (string.IsNullOrEmpty(id) || catalog == null || !catalog.TryGet(id, out var petDef) || petDef == null)
                return AddPetResult.Rejected(AddPetRejectReason.UnknownId);
            if (!dispatcher.CanEnqueue()) return AddPetResult.Rejected(AddPetRejectReason.QueueFull);

            ownedCopies.TryGetValue(id, out int before);
            int after = InventoryMath.AddCopiesClamped(before, amount, out bool isNew);
            ownedCopies[id] = after;
            totalCopiesCache += (after - before);

            if (isNew)
            {
                if (!petDef.IsDeprecated) releasedOwnedCount++;
                sortedOwnedIds.Add(id);
                sortedOwnedIds.Sort(displayComparer);
            }

            dispatcher.EnqueueAndDrain(new PetAcquiredEvent(id, isNew, after));
            return AddPetResult.Added(isNew, after);
        }

        public bool IsOwned(string id) => State == InventoryState.Ready && !string.IsNullOrEmpty(id) && ownedCopies.ContainsKey(id);

        public int GetCopies(string id)
        {
            if (State != InventoryState.Ready || string.IsNullOrEmpty(id)) return 0;
            return ownedCopies.TryGetValue(id, out int copies) ? copies : 0;
        }

        public int GetOwnedSpecies(List<string> buffer, PetGroup? group = null, PetRarity? rarity = null)
        {
            if (buffer == null) return 0;
            buffer.Clear();
            if (State != InventoryState.Ready) return 0;

            for (int i = 0; i < sortedOwnedIds.Count; i++)
            {
                string id = sortedOwnedIds[i];
                if (group.HasValue || rarity.HasValue)
                {
                    if (!catalog.TryGet(id, out var def) || def == null) continue;
                    if ((group.HasValue && def.Group != group.Value) || (rarity.HasValue && def.Rarity != rarity.Value)) continue;
                }
                buffer.Add(id);
            }
            return buffer.Count;
        }

        public int GetUnownedReleased(List<string> buffer)
        {
            if (buffer == null) return 0;
            buffer.Clear();
            if (State != InventoryState.Ready || catalog == null) return 0;

            var all = catalog.AllPets;
            for (int i = 0; i < all.Count; i++)
            {
                var def = all[i];
                if (def != null && !string.IsNullOrEmpty(def.Id) && !def.IsDeprecated && !ownedCopies.ContainsKey(def.Id))
                    buffer.Add(def.Id);
            }
            buffer.Sort(displayComparer);
            return buffer.Count;
        }

        public PetCopyEntry[] ExportSnapshot()
        {
            int totalEntries = ownedCopies.Count + orphanCopies.Count;
            if (totalEntries == 0) return Array.Empty<PetCopyEntry>();

            var result = new PetCopyEntry[totalEntries];
            int idx = 0;
            foreach (var kvp in ownedCopies) result[idx++] = new PetCopyEntry(kvp.Key, kvp.Value);
            foreach (var kvp in orphanCopies) result[idx++] = new PetCopyEntry(kvp.Key, kvp.Value);

            Array.Sort(result, (a, b) => string.CompareOrdinal(a.Id, b.Id));
            return result;
        }

        private void CountReleasedSpecies()
        {
            var all = catalog.AllPets;
            if (all == null) return;
            for (int i = 0; i < all.Count; i++)
            {
                var p = all[i];
                if (p != null && !string.IsNullOrEmpty(p.Id) && !p.IsDeprecated) releasedSpeciesCountCache++;
            }
        }

        private void LoadEntries(IReadOnlyList<PetCopyEntry> entries, bool allowOrphans)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (string.IsNullOrEmpty(entry.Id) || entry.Copies < 1) continue;

                if (catalog.TryGet(entry.Id, out var def) && def != null)
                {
                    ownedCopies.TryGetValue(entry.Id, out int before);
                    int after = InventoryMath.AddCopiesClamped(before, entry.Copies, out bool isNew);
                    ownedCopies[entry.Id] = after;
                    totalCopiesCache += (after - before);
                    if (isNew && !def.IsDeprecated) releasedOwnedCount++;
                }
                else if (allowOrphans)
                {
                    orphanCopies.TryGetValue(entry.Id, out int before);
                    orphanCopies[entry.Id] = InventoryMath.AddCopiesClamped(before, entry.Copies, out _);
                }
            }
        }

        private void RebuildSortedList()
        {
            sortedOwnedIds.Clear();
            foreach (var key in ownedCopies.Keys) sortedOwnedIds.Add(key);
            sortedOwnedIds.Sort(displayComparer);
        }
    }
}
