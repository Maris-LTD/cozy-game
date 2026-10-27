using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CozyGachapon.Contracts.Inventory;
using CozyGachapon.Contracts.PetCatalog;
using CozyGachapon.Core.Inventory;
using CozyGachapon.Core.PetCatalog;

namespace CozyGachapon.Tests.EditMode.Inventory
{
    [TestFixture]
    public sealed class InventoryOrphanAndSaveTests
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
        public void AC12_AC13_AC14_AC20_OrphansHiddenButPreservedInExport()
        {
            var catalog = fixture.CreateDefaultCatalog();
            var inv = new InventoryService(catalog, config);
            int eventCount = 0;
            inv.OnPetAcquired += _ => eventCount++;

            var save = new[]
            {
                new PetCopyEntry("pet.a", 3),
                new PetCopyEntry("pet.d", 7)
            };

            inv.Initialize(save);

            Assert.That(inv.IsOwned("pet.d"), Is.False);
            Assert.That(inv.GetCopies("pet.d"), Is.EqualTo(0));
            Assert.That(inv.OwnedSpeciesCount, Is.EqualTo(1));
            Assert.That(inv.TotalCopies, Is.EqualTo(3L));
            Assert.That(eventCount, Is.EqualTo(0));

            var buffer = new List<string>();
            inv.GetOwnedSpecies(buffer);
            Assert.That(buffer, Does.Not.Contain("pet.d"));

            var reject = inv.AddPet("pet.d");
            Assert.That(reject, Is.EqualTo(AddPetResult.Rejected(AddPetRejectReason.UnknownId)));

            var exported = inv.ExportSnapshot();
            Assert.That(exported, Is.EqualTo(new[]
            {
                new PetCopyEntry("pet.a", 3),
                new PetCopyEntry("pet.d", 7)
            }));

            var pD = fixture.CreatePet("pet.d", PetRarity.Epic, PetGroup.Woodland);
            var fullCatalog = new PetDatabase(new[]
            {
                catalog.Get("pet.a"),
                catalog.Get("pet.b"),
                catalog.Get("pet.c"),
                pD
            });

            var inv2 = new InventoryService(fullCatalog, config);
            inv2.Initialize(exported);
            Assert.That(inv2.GetCopies("pet.d"), Is.EqualTo(7));
            Assert.That(inv2.OwnedSpeciesCount, Is.EqualTo(2));
        }

        [Test]
        public void AC15_MalformedSaveEntries_MergedOrDiscardedSafely()
        {
            var catalog = fixture.CreateDefaultCatalog();
            var inv = new InventoryService(catalog, config);

            var save = new[]
            {
                new PetCopyEntry("pet.a", 3),
                new PetCopyEntry("pet.a", 2),
                new PetCopyEntry("pet.b", 0),
                new PetCopyEntry("", 4),
                new PetCopyEntry(null, 1)
            };

            inv.Initialize(save);

            Assert.That(inv.GetCopies("pet.a"), Is.EqualTo(5));
            Assert.That(inv.IsOwned("pet.b"), Is.False);
            Assert.That(inv.OwnedSpeciesCount, Is.EqualTo(1));

            var exported = inv.ExportSnapshot();
            Assert.That(exported.Length, Is.EqualTo(1));
            Assert.That(exported[0], Is.EqualTo(new PetCopyEntry("pet.a", 5)));
        }

        [Test]
        public void AC16_AC29_StartingPets_AppliedOnlyWhenSaveIsNull()
        {
            var catalog = fixture.CreateDefaultCatalog();
            config.SetConfig(startingPets: new[] { new PetCopyEntry("pet.a", 1) });

            var invNew = new InventoryService(catalog, config);
            int events = 0;
            invNew.OnPetAcquired += _ => events++;
            invNew.Initialize(savedEntries: null);

            Assert.That(invNew.GetCopies("pet.a"), Is.EqualTo(1));
            Assert.That(events, Is.EqualTo(0));

            var invWithSave = new InventoryService(catalog, config);
            invWithSave.Initialize(new[] { new PetCopyEntry("pet.b", 2) });
            Assert.That(invWithSave.IsOwned("pet.a"), Is.False);
            Assert.That(invWithSave.GetCopies("pet.b"), Is.EqualTo(2));

            var invEmptySave = new InventoryService(catalog, config);
            invEmptySave.Initialize(new PetCopyEntry[0]);
            Assert.That(invEmptySave.IsOwned("pet.a"), Is.False);
        }
    }
}
