using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CozyGachapon.Contracts.Inventory;
using CozyGachapon.Core.Inventory;
using CozyGachapon.Core.PetCatalog;

namespace CozyGachapon.Tests.EditMode.Inventory
{
    [TestFixture]
    public sealed class InventoryEventQueueTests
    {
        private InventoryTestFixture fixture;
        private PetDatabase catalog;
        private InventoryConfig config;

        [SetUp]
        public void SetUp()
        {
            fixture = new InventoryTestFixture();
            catalog = fixture.CreateDefaultCatalog();
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
        public void AC22_NestedAddPetInListener_DispatchesSequentiallyInFifoOrder()
        {
            var inv = new InventoryService(catalog, config);
            inv.Initialize();

            var l1Log = new List<PetAcquiredEvent>();
            var l2Log = new List<PetAcquiredEvent>();
            AddPetResult nestedResult = default;
            int copiesOfBDuringL1 = 0;

            inv.OnPetAcquired += evt =>
            {
                l1Log.Add(evt);
                if (evt.Id == "pet.a")
                {
                    nestedResult = inv.AddPet("pet.b");
                    copiesOfBDuringL1 = inv.GetCopies("pet.b");
                }
            };

            inv.OnPetAcquired += evt =>
            {
                l2Log.Add(evt);
            };

            inv.AddPet("pet.a");

            Assert.That(nestedResult, Is.EqualTo(AddPetResult.Added(true, 1)));
            Assert.That(copiesOfBDuringL1, Is.EqualTo(1));

            var expected = new[]
            {
                new PetAcquiredEvent("pet.a", true, 1),
                new PetAcquiredEvent("pet.b", true, 1)
            };

            Assert.That(l1Log, Is.EqualTo(expected));
            Assert.That(l2Log, Is.EqualTo(expected));
        }

        [Test]
        public void AC29_ExceedingMaxQueuedEvents_RejectsWithQueueFull()
        {
            config.SetConfig(maxQueuedEvents: 8);
            var inv = new InventoryService(catalog, config);
            inv.Initialize();

            int receivedEvents = 0;
            AddPetResult ninthResult = default;

            inv.OnPetAcquired += _ =>
            {
                receivedEvents++;
                var res = inv.AddPet("pet.a");
                if (!res.IsAdded)
                {
                    ninthResult = res;
                }
            };

            inv.AddPet("pet.a");

            Assert.That(receivedEvents, Is.EqualTo(8));
            Assert.That(ninthResult, Is.EqualTo(AddPetResult.Rejected(AddPetRejectReason.QueueFull)));
            Assert.That(inv.GetCopies("pet.a"), Is.EqualTo(8));
        }
    }
}
