using System;
using System.Collections.Generic;
using UnityEngine;
using CozyGachapon.Contracts.Inventory;

namespace CozyGachapon.Core.Inventory
{
    [CreateAssetMenu(fileName = "InventoryConfig", menuName = "CozyGachapon/Inventory/Config")]
    public sealed class InventoryConfig : ScriptableObject
    {
        [SerializeField] private PetCopyEntry[] startingPets = Array.Empty<PetCopyEntry>();
        [SerializeField] private bool showUnownedSlots = true;
        [SerializeField] private bool rarityOrderDescending = true;
        [SerializeField] private int maxQueuedEvents = 64;
        [SerializeField] private int copiesDisplayMax = 99;

        public IReadOnlyList<PetCopyEntry> StartingPets => startingPets ?? Array.Empty<PetCopyEntry>();
        public bool ShowUnownedSlots => showUnownedSlots;
        public bool RarityOrderDescending => rarityOrderDescending;
        public int MaxQueuedEvents => maxQueuedEvents;
        public int CopiesDisplayMax => copiesDisplayMax;

        public void SetConfig(
            PetCopyEntry[] startingPets = null,
            bool showUnownedSlots = true,
            bool rarityOrderDescending = true,
            int maxQueuedEvents = 64,
            int copiesDisplayMax = 99)
        {
            this.startingPets = startingPets ?? Array.Empty<PetCopyEntry>();
            this.showUnownedSlots = showUnownedSlots;
            this.rarityOrderDescending = rarityOrderDescending;
            this.maxQueuedEvents = maxQueuedEvents;
            this.copiesDisplayMax = copiesDisplayMax;
        }
    }
}
