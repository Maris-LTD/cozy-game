using System;
using UnityEngine;
using CozyGachapon.Contracts.PetCatalog;

namespace CozyGachapon.Core.PetCatalog
{
    [CreateAssetMenu(fileName = "PetRarityConfig", menuName = "CozyGachapon/Pet/Rarity Config")]
    public sealed class PetRarityConfig : ScriptableObject
    {
        [SerializeField] private double rareCoefficient = 2.5;
        [SerializeField] private double epicCoefficient = 6.0;
        [SerializeField] private double legendaryCoefficient = 15.0;

        public const double CommonCoefficient = 1.0;

        public double RareCoefficient => rareCoefficient;
        public double EpicCoefficient => epicCoefficient;
        public double LegendaryCoefficient => legendaryCoefficient;

        public void SetCoefficients(double rare, double epic, double legendary)
        {
            rareCoefficient = rare;
            epicCoefficient = epic;
            legendaryCoefficient = legendary;
        }

        public double GetCoefficient(PetRarity rarity)
        {
            switch (rarity)
            {
                case PetRarity.Common:
                    return CommonCoefficient;
                case PetRarity.Rare:
                    return rareCoefficient;
                case PetRarity.Epic:
                    return epicCoefficient;
                case PetRarity.Legendary:
                    return legendaryCoefficient;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rarity), rarity, "Invalid pet rarity");
            }
        }
    }
}
