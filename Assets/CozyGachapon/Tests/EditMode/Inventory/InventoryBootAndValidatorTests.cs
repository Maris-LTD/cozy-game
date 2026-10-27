using NUnit.Framework;
using UnityEngine;
using CozyGachapon.Bootstrap;
using CozyGachapon.Contracts;
using CozyGachapon.Contracts.Inventory;
using CozyGachapon.Contracts.PetCatalog;
using CozyGachapon.Core.Inventory;
using CozyGachapon.Core.PetCatalog;
using CozyGachapon.Foundation;

namespace CozyGachapon.Tests.EditMode.Inventory
{
    [TestFixture]
    public sealed class InventoryBootAndValidatorTests
    {
        private InventoryTestFixture fixture;
        private InventoryConfig config;
        private ServiceRegistry services;

        [SetUp]
        public void SetUp()
        {
            fixture = new InventoryTestFixture();
            config = ScriptableObject.CreateInstance<InventoryConfig>();
            services = new ServiceRegistry();
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
        public void AC17_Validator_CatchesUnknownInvalidAndDuplicateStartingPets()
        {
            var catalog = fixture.CreateDefaultCatalog();
            config.SetConfig(startingPets: new[]
            {
                new PetCopyEntry("pet.zzz", 1),
                new PetCopyEntry("pet.a", 0),
                new PetCopyEntry("pet.b", 1),
                new PetCopyEntry("pet.b", 2)
            });

            var report = InventoryConfigValidator.Validate(config, catalog);
            Assert.That(report.Errors.Count, Is.EqualTo(3));

            var inv = new InventoryService(catalog, config);
            inv.Initialize();
            Assert.That(inv.OwnedSpeciesCount, Is.EqualTo(1));
            Assert.That(inv.GetCopies("pet.b"), Is.EqualTo(3));
        }

        [Test]
        public void AC18_Validator_WarnsOnDeprecatedStartingPet()
        {
            var depPet = fixture.CreatePet("pet.dep", PetRarity.Common, PetGroup.Woodland, isDeprecated: true);
            var catalog = new PetDatabase(new[] { depPet });
            config.SetConfig(startingPets: new[] { new PetCopyEntry("pet.dep", 1) });

            var report = InventoryConfigValidator.Validate(config, catalog);
            Assert.That(report.Errors.Count, Is.EqualTo(0));
            Assert.That(report.Warnings.Count, Is.EqualTo(1));
        }

        [Test]
        public void AC21_Validator_CatchesCopiesDisplayMaxOutOfBounds()
        {
            var catalog = fixture.CreateDefaultCatalog();

            config.SetConfig(copiesDisplayMax: 8);
            var rLow = InventoryConfigValidator.Validate(config, catalog);
            Assert.That(rLow.Errors.Count, Is.EqualTo(1));

            config.SetConfig(copiesDisplayMax: 1000);
            var rHigh = InventoryConfigValidator.Validate(config, catalog);
            Assert.That(rHigh.Errors.Count, Is.EqualTo(1));
        }

        [Test]
        public void AC23_AC25_BootStep_SucceedsWhenCatalogRegisteredAndFailsWhenMissing()
        {
            var stepFail = new InventoryBootStep(config, services);
            Assert.That(stepFail.Id, Is.EqualTo("inventory.core"));
            Assert.That(stepFail.Phase, Is.EqualTo(BootPhase.GameplayInit));
            Assert.That(stepFail.RunsAfter, Is.EqualTo(new[] { "economy.core" }));
            Assert.That(stepFail.IsCritical, Is.True);

            stepFail.Run();
            Assert.That(stepFail.IsFailed, Is.True);
            Assert.That(stepFail.IsDone, Is.False);
            Assert.That(stepFail.Service.AddPet("pet.a"), Is.EqualTo(AddPetResult.Rejected(AddPetRejectReason.NotReady)));

            var catalog = fixture.CreateDefaultCatalog();
            services.Register<IPetDatabase>(catalog);

            var stepOk = new InventoryBootStep(config, services);
            stepOk.Run();
            Assert.That(stepOk.IsDone, Is.True);
            Assert.That(stepOk.IsFailed, Is.False);
            Assert.That(services.TryGet<IInventoryService>(out var registeredInv), Is.True);
            Assert.That(registeredInv, Is.Not.Null);
        }
    }
}
