using System.Linq;
using NUnit.Framework;
using UnityEngine;
using CozyGachapon.Contracts.PetCatalog;
using CozyGachapon.Core.PetCatalog;

namespace CozyGachapon.Tests.EditMode.PetCatalog
{
    [TestFixture]
    public sealed class MvpRosterTests
    {
        private PetDefinition[] pets;

        [SetUp]
        public void SetUp()
        {
            pets = MvpPetFactory.CreateAllMvpPets();
        }

        [TearDown]
        public void TearDown()
        {
            if (pets != null)
            {
                for (int i = 0; i < pets.Length; i++)
                {
                    if (pets[i] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(pets[i]);
                    }
                }
            }
        }

        [Test]
        public void MvpRoster_HasExact7Pets()
        {
            Assert.That(pets.Length, Is.EqualTo(7));
        }

        [Test]
        public void MvpRoster_HasExactRarityDistribution()
        {
            int commonCount = pets.Count(p => p.Rarity == PetRarity.Common);
            int rareCount = pets.Count(p => p.Rarity == PetRarity.Rare);
            int epicCount = pets.Count(p => p.Rarity == PetRarity.Epic);
            int legendaryCount = pets.Count(p => p.Rarity == PetRarity.Legendary);

            Assert.That(commonCount, Is.EqualTo(3));
            Assert.That(rareCount, Is.EqualTo(2));
            Assert.That(epicCount, Is.EqualTo(1));
            Assert.That(legendaryCount, Is.EqualTo(1));
        }

        [Test]
        public void MvpRoster_HasUniqueIdsStartingWithPetDot()
        {
            var ids = pets.Select(p => p.Id).ToArray();
            Assert.That(ids.Distinct().Count(), Is.EqualTo(7));
            for (int i = 0; i < ids.Length; i++)
            {
                Assert.That(ids[i], Does.StartWith("pet."));
            }
        }

        [Test]
        public void MvpRoster_NoFieldHasNoneValue()
        {
            for (int i = 0; i < pets.Length; i++)
            {
                var p = pets[i];
                Assert.That(p.Rarity, Is.Not.EqualTo(PetRarity.None));
                Assert.That(p.Group, Is.Not.EqualTo(PetGroup.None));
                Assert.That(p.Role, Is.Not.EqualTo(PetRole.None));
                Assert.That(p.ReleaseTier, Is.EqualTo(PetReleaseTier.Mvp));
                Assert.That(p.Effects.Length, Is.GreaterThan(0));

                for (int j = 0; j < p.Effects.Length; j++)
                {
                    var effect = p.Effects[j];
                    Assert.That(effect.Type, Is.Not.EqualTo(PetEffectType.None));
                    Assert.That(effect.Scope, Is.Not.EqualTo(PetEffectScope.None));
                }
            }
        }
    }
}
