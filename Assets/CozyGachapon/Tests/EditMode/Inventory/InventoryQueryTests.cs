using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CozyGachapon.Contracts.PetCatalog;
using CozyGachapon.Core.Inventory;
using CozyGachapon.Core.PetCatalog;

namespace CozyGachapon.Tests.EditMode.Inventory
{
    [TestFixture]
    public sealed class InventoryQueryTests
    {
        private InventoryTestFixture fixture;
        private InventoryConfig config;

        [SetUp]
        public void SetUp()
        {
            fixture = new InventoryTestFixture();
            config = ScriptableObject.CreateInstance<InventoryConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            if (config != null)
            {
                Object.DestroyImmediate(config);
            }
            fixture.Dispose();
        }

        [Test]
        public void AC9_GetOwnedSpecies_RespectsRarityOrderDescendingIndependentOfAddOrder()
        {
            var catalog = fixture.CreateDefaultCatalog();

            config.SetConfig(rarityOrderDescending: true);
            var invDesc = new InventoryService(catalog, config);
            invDesc.Initialize();
            invDesc.AddPet("pet.a", 2);
            invDesc.AddPet("pet.b", 1);
            invDesc.AddPet("pet.c", 1);

            var buffer = new List<string>();
            invDesc.GetOwnedSpecies(buffer);
            Assert.That(buffer, Is.EqualTo(new[] { "pet.c", "pet.b", "pet.a" }));

            config.SetConfig(rarityOrderDescending: false);
            var invAsc = new InventoryService(catalog, config);
            invAsc.Initialize();
            invAsc.AddPet("pet.c", 1);
            invAsc.AddPet("pet.a", 2);
            invAsc.AddPet("pet.b", 1);

            invAsc.GetOwnedSpecies(buffer);
            Assert.That(buffer, Is.EqualTo(new[] { "pet.a", "pet.b", "pet.c" }));
        }

        [Test]
        public void AC10_SameRarityAndGroup_SortsByIdOrdinalAscending()
        {
            var p1 = fixture.CreatePet("pet.x1", PetRarity.Common, PetGroup.Woodland);
            var p2 = fixture.CreatePet("pet.x2", PetRarity.Common, PetGroup.Woodland);
            var catalog = new PetDatabase(new[] { p2, p1 });

            var inv = new InventoryService(catalog);
            inv.Initialize();
            inv.AddPet("pet.x2");
            inv.AddPet("pet.x1");

            var buffer = new List<string>();
            inv.GetOwnedSpecies(buffer);
            Assert.That(buffer, Is.EqualTo(new[] { "pet.x1", "pet.x2" }));
        }

        [Test]
        public void AC11_GetOwnedSpecies_FiltersByGroupAndRarityAndClearsBuffer()
        {
            var catalog = fixture.CreateDefaultCatalog();
            var inv = new InventoryService(catalog);
            inv.Initialize();
            inv.AddPet("pet.a");
            inv.AddPet("pet.b");
            inv.AddPet("pet.c");

            var buffer = new List<string> { "d1", "d2", "d3", "d4", "d5" };
            int countSweet = inv.GetOwnedSpecies(buffer, group: PetGroup.Sweets);
            Assert.That(countSweet, Is.EqualTo(1));
            Assert.That(buffer, Is.EqualTo(new[] { "pet.b" }));

            int countLegendary = inv.GetOwnedSpecies(buffer, rarity: PetRarity.Legendary);
            Assert.That(countLegendary, Is.EqualTo(1));
            Assert.That(buffer, Is.EqualTo(new[] { "pet.c" }));

            Assert.That(inv.GetOwnedSpecies(null), Is.EqualTo(0));
        }

        [Test]
        public void AC19_CollectionProgress_ExcludesDeprecatedAndHandlesZeroReleased()
        {
            var emptyCat = new PetDatabase(new PetDefinition[0]);
            var emptyInv = new InventoryService(emptyCat);
            emptyInv.Initialize();
            Assert.That(emptyInv.CollectionProgress, Is.EqualTo(0.0));

            var pets = new List<PetDefinition>();
            for (int i = 1; i <= 7; i++)
            {
                pets.Add(fixture.CreatePet("pet.p" + i, PetRarity.Common, PetGroup.Woodland));
            }
            var dep1 = fixture.CreatePet("pet.dep1", PetRarity.Common, PetGroup.Woodland, isDeprecated: true);
            var dep2 = fixture.CreatePet("pet.dep2", PetRarity.Common, PetGroup.Woodland, isDeprecated: true);
            pets.Add(dep1);
            pets.Add(dep2);

            var catalog = new PetDatabase(pets);
            var inv = new InventoryService(catalog);
            inv.Initialize();

            inv.AddPet("pet.p1");
            inv.AddPet("pet.p2");
            inv.AddPet("pet.p3");

            Assert.That(inv.ReleasedSpeciesCount, Is.EqualTo(7));
            Assert.That(inv.CollectionProgress, Is.EqualTo(3.0 / 7.0).Within(1e-6));

            inv.AddPet("pet.dep1");
            Assert.That(inv.OwnedSpeciesCount, Is.EqualTo(4));
            Assert.That(inv.CollectionProgress, Is.EqualTo(3.0 / 7.0).Within(1e-6));

            var unowned = new List<string>();
            inv.GetUnownedReleased(unowned);
            Assert.That(unowned, Does.Not.Contain("pet.dep1"));
            Assert.That(unowned, Does.Not.Contain("pet.dep2"));
            Assert.That(unowned.Count, Is.EqualTo(4));

            for (int i = 4; i <= 7; i++)
            {
                inv.AddPet("pet.p" + i);
            }
            Assert.That(inv.CollectionProgress, Is.EqualTo(1.0));
        }
    }
}
