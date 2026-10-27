using System;
using System.Collections.Generic;
using UnityEngine;

namespace CozyGachapon.Core.PetCatalog
{
    [CreateAssetMenu(fileName = "PetCatalogConfig", menuName = "CozyGachapon/Pet/Catalog Config")]
    public sealed class PetCatalogConfig : ScriptableObject
    {
        [SerializeField] private PetDefinition[] pets = Array.Empty<PetDefinition>();
        [SerializeField] private PetRarityConfig rarityConfig;

        public IReadOnlyList<PetDefinition> Pets => pets ?? Array.Empty<PetDefinition>();
        public PetRarityConfig RarityConfig => rarityConfig;

        public void SetConfig(PetDefinition[] petDefinitions, PetRarityConfig rarityCfg = null)
        {
            pets = petDefinitions ?? Array.Empty<PetDefinition>();
            rarityConfig = rarityCfg;
        }
    }
}
