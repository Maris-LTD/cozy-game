using System;
using NUnit.Framework;
using UnityEngine;
using CozyGachapon.Contracts.PetCatalog;
using CozyGachapon.Core.PetCatalog;

namespace CozyGachapon.Tests.EditMode.PetCatalog
{
    [TestFixture]
    public sealed class PetDefinitionTests
    {
        private PetDefinition pet;

        [TearDown]
        public void TearDown()
        {
            if (pet != null)
            {
                UnityEngine.Object.DestroyImmediate(pet);
            }
        }

        [Test]
        public void SetData_SetsAllPropertiesCorrectly()
        {
            pet = ScriptableObject.CreateInstance<PetDefinition>();
            var effect = new PetEffect(PetEffectType.FlatGoldPerDrop, PetEffectScope.Self, 10.0, PetGroup.Woodland);

            pet.SetData(
                id: "pet.test_duck",
                displayNameKey: "pet.test_duck.name",
                rarity: PetRarity.Rare,
                group: PetGroup.Woodland,
                role: PetRole.GoldBooster,
                releaseTier: PetReleaseTier.Mvp,
                effects: new[] { effect },
                iconAddress: "icon_path",
                prefabAddress: "prefab_path"
            );

            Assert.That(pet.Id, Is.EqualTo("pet.test_duck"));
            Assert.That(pet.DisplayNameKey, Is.EqualTo("pet.test_duck.name"));
            Assert.That(pet.Rarity, Is.EqualTo(PetRarity.Rare));
            Assert.That(pet.Group, Is.EqualTo(PetGroup.Woodland));
            Assert.That(pet.Role, Is.EqualTo(PetRole.GoldBooster));
            Assert.That(pet.ReleaseTier, Is.EqualTo(PetReleaseTier.Mvp));
            Assert.That(pet.Effects.Length, Is.EqualTo(1));
            Assert.That(pet.Effects[0], Is.EqualTo(effect));
            Assert.That(pet.IconAddress, Is.EqualTo("icon_path"));
            Assert.That(pet.PrefabAddress, Is.EqualTo("prefab_path"));
        }
    }
}
