using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CozyGachapon.Contracts.PetCatalog;
using CozyGachapon.Core.PetCatalog;

namespace CozyGachapon.Tests.EditMode.PetCatalog
{
    [TestFixture]
    public sealed class PetDatabaseTests
    {
        private PetDefinition[] pets;
        private PetDatabase database;

        [SetUp]
        public void SetUp()
        {
            pets = MvpPetFactory.CreateAllMvpPets();
            database = new PetDatabase(pets);
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
        public void Count_MatchesInputPets()
        {
            Assert.That(database.Count, Is.EqualTo(7));
            Assert.That(database.AllPets.Count, Is.EqualTo(7));
        }

        [Test]
        public void Get_ValidId_ReturnsPet()
        {
            var pet = database.Get("pet.woody_duck");
            Assert.That(pet, Is.Not.Null);
            Assert.That(pet.Id, Is.EqualTo("pet.woody_duck"));
            Assert.That(pet.Rarity, Is.EqualTo(PetRarity.Common));
        }

        [Test]
        public void Get_InvalidId_ThrowsKeyNotFoundException()
        {
            Assert.Throws<KeyNotFoundException>(() => database.Get("pet.unknown"));
            Assert.Throws<ArgumentNullException>(() => database.Get(null));
        }

        [Test]
        public void TryGet_ValidAndInvalidIds()
        {
            Assert.That(database.TryGet("pet.lucky_cat", out var pet), Is.True);
            Assert.That(pet, Is.Not.Null);
            Assert.That(pet.Id, Is.EqualTo("pet.lucky_cat"));

            Assert.That(database.TryGet("pet.non_existing", out var notFound), Is.False);
            Assert.That(notFound, Is.Null);

            Assert.That(database.TryGet(null, out var nullResult), Is.False);
            Assert.That(nullResult, Is.Null);
        }

        [Test]
        public void GetByGroup_ReturnsIndexedPets()
        {
            var woodland = database.GetByGroup(PetGroup.Woodland);
            Assert.That(woodland.Count, Is.EqualTo(3));

            var sky = database.GetByGroup(PetGroup.Sky);
            Assert.That(sky.Count, Is.EqualTo(0));
        }

        [Test]
        public void GetByRarity_ReturnsIndexedPets()
        {
            var common = database.GetByRarity(PetRarity.Common);
            Assert.That(common.Count, Is.EqualTo(3));

            var legendary = database.GetByRarity(PetRarity.Legendary);
            Assert.That(legendary.Count, Is.EqualTo(1));
        }

        [Test]
        public void GetByRole_ReturnsIndexedPets()
        {
            var goldBoosters = database.GetByRole(PetRole.GoldBooster);
            Assert.That(goldBoosters.Count, Is.EqualTo(2));

            var multipliers = database.GetByRole(PetRole.Multiplier);
            Assert.That(multipliers.Count, Is.EqualTo(2));
        }

        [Test]
        public void GetByTier_ReturnsIndexedPets()
        {
            var mvp = database.GetByTier(PetReleaseTier.Mvp);
            Assert.That(mvp.Count, Is.EqualTo(7));

            var postMvp = database.GetByTier(PetReleaseTier.PostMvp);
            Assert.That(postMvp.Count, Is.EqualTo(0));
        }

        [Test]
        public void DuplicatePetId_ThrowsInvalidOperationException()
        {
            var dup = MvpPetFactory.CreateWoodyDuck();
            var list = new List<PetDefinition>(pets) { dup };
            try
            {
                Assert.Throws<InvalidOperationException>(() => new PetDatabase(list));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(dup);
            }
        }
    }
}
