using System;
using System.Collections.Generic;
using CozyGachapon.Contracts.PetCatalog;
using UnityEngine;

namespace CozyGachapon.Core.PetCatalog
{
    public static class MvpPetFactory
    {
        public static PetDefinition CreateWoodyDuck()
        {
            var pet = ScriptableObject.CreateInstance<PetDefinition>();
            pet.name = "pet.woody_duck";
            pet.SetData(
                id: "pet.woody_duck",
                displayNameKey: "pet.woody_duck.name",
                rarity: PetRarity.Common,
                group: PetGroup.Woodland,
                role: PetRole.GoldBooster,
                releaseTier: PetReleaseTier.Mvp,
                effects: new[]
                {
                    new PetEffect(PetEffectType.FlatGoldPerDrop, PetEffectScope.Self, 1.0, PetGroup.None)
                },
                iconAddress: "Assets/CozyGachapon/Art/Icons/pet_woody_duck.png",
                prefabAddress: "Assets/CozyGachapon/Prefabs/Pets/pet_woody_duck.prefab"
            );
            return pet;
        }

        public static PetDefinition CreateBouncyBunny()
        {
            var pet = ScriptableObject.CreateInstance<PetDefinition>();
            pet.name = "pet.bouncy_bunny";
            pet.SetData(
                id: "pet.bouncy_bunny",
                displayNameKey: "pet.bouncy_bunny.name",
                rarity: PetRarity.Common,
                group: PetGroup.Woodland,
                role: PetRole.Multiplier,
                releaseTier: PetReleaseTier.Mvp,
                effects: new[]
                {
                    new PetEffect(PetEffectType.ScoreMultiplier, PetEffectScope.Self, 1.2, PetGroup.None)
                },
                iconAddress: "Assets/CozyGachapon/Art/Icons/pet_bouncy_bunny.png",
                prefabAddress: "Assets/CozyGachapon/Prefabs/Pets/pet_bouncy_bunny.prefab"
            );
            return pet;
        }

        public static PetDefinition CreateCozyFox()
        {
            var pet = ScriptableObject.CreateInstance<PetDefinition>();
            pet.name = "pet.cozy_fox";
            pet.SetData(
                id: "pet.cozy_fox",
                displayNameKey: "pet.cozy_fox.name",
                rarity: PetRarity.Rare,
                group: PetGroup.Woodland,
                role: PetRole.SynergyEnabler,
                releaseTier: PetReleaseTier.Mvp,
                effects: new[]
                {
                    new PetEffect(PetEffectType.SynergyCountBonus, PetEffectScope.Group, 1.0, PetGroup.Woodland),
                    new PetEffect(PetEffectType.PercentGoldBoost, PetEffectScope.Group, 0.15, PetGroup.Woodland)
                },
                iconAddress: "Assets/CozyGachapon/Art/Icons/pet_cozy_fox.png",
                prefabAddress: "Assets/CozyGachapon/Prefabs/Pets/pet_cozy_fox.prefab"
            );
            return pet;
        }

        public static PetDefinition CreateRainbowCandy()
        {
            var pet = ScriptableObject.CreateInstance<PetDefinition>();
            pet.name = "pet.rainbow_candy";
            pet.SetData(
                id: "pet.rainbow_candy",
                displayNameKey: "pet.rainbow_candy.name",
                rarity: PetRarity.Common,
                group: PetGroup.Sweets,
                role: PetRole.GoldBooster,
                releaseTier: PetReleaseTier.Mvp,
                effects: new[]
                {
                    new PetEffect(PetEffectType.FlatGoldPerDrop, PetEffectScope.Self, 1.0, PetGroup.None)
                },
                iconAddress: "Assets/CozyGachapon/Art/Icons/pet_rainbow_candy.png",
                prefabAddress: "Assets/CozyGachapon/Prefabs/Pets/pet_rainbow_candy.prefab"
            );
            return pet;
        }

        public static PetDefinition CreateDreamyFrog()
        {
            var pet = ScriptableObject.CreateInstance<PetDefinition>();
            pet.name = "pet.dreamy_frog";
            pet.SetData(
                id: "pet.dreamy_frog",
                displayNameKey: "pet.dreamy_frog.name",
                rarity: PetRarity.Rare,
                group: PetGroup.Aquatic,
                role: PetRole.Spawner,
                releaseTier: PetReleaseTier.Mvp,
                effects: new[]
                {
                    new PetEffect(PetEffectType.SpawnRateBoost, PetEffectScope.Board, 0.25, PetGroup.None),
                    new PetEffect(PetEffectType.PercentGoldBoost, PetEffectScope.Self, 0.10, PetGroup.None)
                },
                iconAddress: "Assets/CozyGachapon/Art/Icons/pet_dreamy_frog.png",
                prefabAddress: "Assets/CozyGachapon/Prefabs/Pets/pet_dreamy_frog.prefab"
            );
            return pet;
        }

        public static PetDefinition CreateLuckyCat()
        {
            var pet = ScriptableObject.CreateInstance<PetDefinition>();
            pet.name = "pet.lucky_cat";
            pet.SetData(
                id: "pet.lucky_cat",
                displayNameKey: "pet.lucky_cat.name",
                rarity: PetRarity.Epic,
                group: PetGroup.Savanna,
                role: PetRole.Multiplier,
                releaseTier: PetReleaseTier.Mvp,
                effects: new[]
                {
                    new PetEffect(PetEffectType.ScoreMultiplier, PetEffectScope.Both, 2.0, PetGroup.Savanna),
                    new PetEffect(PetEffectType.PercentGoldBoost, PetEffectScope.Board, 0.30, PetGroup.None)
                },
                iconAddress: "Assets/CozyGachapon/Art/Icons/pet_lucky_cat.png",
                prefabAddress: "Assets/CozyGachapon/Prefabs/Pets/pet_lucky_cat.prefab"
            );
            return pet;
        }

        public static PetDefinition CreateChubbyKingCat()
        {
            var pet = ScriptableObject.CreateInstance<PetDefinition>();
            pet.name = "pet.chubby_king_cat";
            pet.SetData(
                id: "pet.chubby_king_cat",
                displayNameKey: "pet.chubby_king_cat.name",
                rarity: PetRarity.Legendary,
                group: PetGroup.Savanna,
                role: PetRole.SynergyEnabler,
                releaseTier: PetReleaseTier.Mvp,
                effects: new[]
                {
                    new PetEffect(PetEffectType.SynergyCountBonus, PetEffectScope.Both, 2.0, PetGroup.Savanna),
                    new PetEffect(PetEffectType.PercentGoldBoost, PetEffectScope.Board, 0.50, PetGroup.None),
                    new PetEffect(PetEffectType.ScoreMultiplier, PetEffectScope.Both, 1.5, PetGroup.Savanna)
                },
                iconAddress: "Assets/CozyGachapon/Art/Icons/pet_chubby_king_cat.png",
                prefabAddress: "Assets/CozyGachapon/Prefabs/Pets/pet_chubby_king_cat.prefab"
            );
            return pet;
        }

        public static PetDefinition[] CreateAllMvpPets()
        {
            return new[]
            {
                CreateWoodyDuck(),
                CreateBouncyBunny(),
                CreateCozyFox(),
                CreateRainbowCandy(),
                CreateDreamyFrog(),
                CreateLuckyCat(),
                CreateChubbyKingCat()
            };
        }
    }
}
