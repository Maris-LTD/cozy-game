using System.Collections.Generic;
using NUnit.Framework;
using CozyGachapon.Contracts.Inventory;
using CozyGachapon.Core.Inventory;
using CozyGachapon.Core.PetCatalog;

namespace CozyGachapon.Tests.EditMode.Inventory
{
    [TestFixture]
    public sealed class InventoryAddPetTests
    {
        private InventoryTestFixture fixture;
        private PetDatabase catalog;
        private InventoryService inventory;
        private List<PetAcquiredEvent> events;

        [SetUp]
        public void SetUp()
        {
            fixture = new InventoryTestFixture();
            catalog = fixture.CreateDefaultCatalog();
            inventory = new InventoryService(catalog);
            inventory.Initialize();
            events = new List<PetAcquiredEvent>();
            inventory.OnPetAcquired += e => events.Add(e);
        }

        [TearDown]
        public void TearDown()
        {
            fixture.Dispose();
        }

        [Test]
        public void AC1_AddPet_NewSpecies_ReturnsAddedAndFiresEventOnce()
        {
            var result = inventory.AddPet("pet.a");

            Assert.That(result, Is.EqualTo(AddPetResult.Added(true, 1)));
            Assert.That(inventory.IsOwned("pet.a"), Is.True);
            Assert.That(inventory.GetCopies("pet.a"), Is.EqualTo(1));
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0], Is.EqualTo(new PetAcquiredEvent("pet.a", true, 1)));
        }

        [Test]
        public void AC2_AddPet_DuplicateSpecies_IncrementsCopiesAndIsNewFalse()
        {
            inventory.AddPet("pet.a", 4);
            events.Clear();

            var result = inventory.AddPet("pet.a");

            Assert.That(result, Is.EqualTo(AddPetResult.Added(false, 5)));
            Assert.That(inventory.OwnedSpeciesCount, Is.EqualTo(1));
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0], Is.EqualTo(new PetAcquiredEvent("pet.a", false, 5)));
        }

        [Test]
        public void AC3_AddPet_MultipleAmount_FiresSingleEvent()
        {
            inventory.AddPet("pet.a", 2);
            events.Clear();

            var result = inventory.AddPet("pet.a", 3);

            Assert.That(result.Copies, Is.EqualTo(5));
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0].Copies, Is.EqualTo(5));
        }

        [Test]
        public void AC4_AddPet_UnknownEmptyOrNullId_RejectsWithUnknownId()
        {
            Assert.That(inventory.AddPet("pet.zzz"), Is.EqualTo(AddPetResult.Rejected(AddPetRejectReason.UnknownId)));
            Assert.That(inventory.AddPet(""), Is.EqualTo(AddPetResult.Rejected(AddPetRejectReason.UnknownId)));
            Assert.That(inventory.AddPet(null), Is.EqualTo(AddPetResult.Rejected(AddPetRejectReason.UnknownId)));

            Assert.That(events.Count, Is.EqualTo(0));
            Assert.That(inventory.OwnedSpeciesCount, Is.EqualTo(0));
        }

        [Test]
        public void AC5_AddPet_ZeroOrNegativeAmount_RejectsWithInvalidAmount()
        {
            Assert.That(inventory.AddPet("pet.a", 0), Is.EqualTo(AddPetResult.Rejected(AddPetRejectReason.InvalidAmount)));
            Assert.That(inventory.AddPet("pet.a", -2), Is.EqualTo(AddPetResult.Rejected(AddPetRejectReason.InvalidAmount)));

            Assert.That(events.Count, Is.EqualTo(0));
            Assert.That(inventory.GetCopies("pet.a"), Is.EqualTo(0));
        }

        [Test]
        public void AC6_UninitializedAndFailedInventory_RejectsWithNotReady()
        {
            var uninit = new InventoryService(catalog);
            Assert.That(uninit.AddPet("pet.a"), Is.EqualTo(AddPetResult.Rejected(AddPetRejectReason.NotReady)));
            Assert.That(uninit.IsOwned("pet.a"), Is.False);
            Assert.That(uninit.GetCopies("pet.a"), Is.EqualTo(0));

            var failed = new InventoryService(null);
            failed.Initialize();
            Assert.That(failed.State, Is.EqualTo(InventoryState.Failed));
            Assert.That(failed.AddPet("pet.a"), Is.EqualTo(AddPetResult.Rejected(AddPetRejectReason.NotReady)));
            Assert.That(failed.IsOwned("pet.a"), Is.False);
            Assert.That(failed.GetCopies("pet.a"), Is.EqualTo(0));
        }

        [Test]
        public void AC7_AddPet_ClampsAtIntMaxWithoutOverflow()
        {
            inventory.AddPet("pet.a", int.MaxValue - 1);
            events.Clear();

            var res1 = inventory.AddPet("pet.a", 5);
            Assert.That(res1, Is.EqualTo(AddPetResult.Added(false, int.MaxValue)));
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0].Copies, Is.EqualTo(int.MaxValue));

            var res2 = inventory.AddPet("pet.a", 1);
            Assert.That(res2.Copies, Is.EqualTo(int.MaxValue));
            Assert.That(inventory.GetCopies("pet.a"), Is.EqualTo(int.MaxValue));
        }
    }
}
