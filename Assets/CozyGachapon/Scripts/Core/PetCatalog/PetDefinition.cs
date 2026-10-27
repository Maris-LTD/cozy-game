using System;
using UnityEngine;
using CozyGachapon.Contracts.PetCatalog;

namespace CozyGachapon.Core.PetCatalog
{
    [CreateAssetMenu(fileName = "PetDefinition", menuName = "CozyGachapon/Pet/Definition")]
    public sealed class PetDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayNameKey;
        [SerializeField] private PetRarity rarity;
        [SerializeField] private PetGroup group;
        [SerializeField] private PetRole role;
        [SerializeField] private PetReleaseTier releaseTier;
        [SerializeField] private PetEffect[] effects = Array.Empty<PetEffect>();
        [SerializeField] private string iconAddress;
        [SerializeField] private string prefabAddress;
        [SerializeField] private bool isDeprecated;

        public string Id => id;
        public string DisplayNameKey => displayNameKey;
        public PetRarity Rarity => rarity;
        public PetGroup Group => group;
        public PetRole Role => role;
        public PetReleaseTier ReleaseTier => releaseTier;
        public PetEffect[] Effects => effects;
        public string IconAddress => iconAddress;
        public string PrefabAddress => prefabAddress;
        public bool IsDeprecated => isDeprecated;

        public void SetData(
            string id,
            string displayNameKey,
            PetRarity rarity,
            PetGroup group,
            PetRole role,
            PetReleaseTier releaseTier,
            PetEffect[] effects,
            string iconAddress = null,
            string prefabAddress = null,
            bool isDeprecated = false)
        {
            this.id = id;
            this.displayNameKey = displayNameKey;
            this.rarity = rarity;
            this.group = group;
            this.role = role;
            this.releaseTier = releaseTier;
            this.effects = effects ?? Array.Empty<PetEffect>();
            this.iconAddress = iconAddress ?? string.Empty;
            this.prefabAddress = prefabAddress ?? string.Empty;
            this.isDeprecated = isDeprecated;
        }
    }
}
