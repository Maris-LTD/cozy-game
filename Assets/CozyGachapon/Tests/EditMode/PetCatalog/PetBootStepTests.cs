using NUnit.Framework;
using UnityEngine;
using CozyGachapon.Bootstrap;
using CozyGachapon.Contracts;
using CozyGachapon.Core.PetCatalog;
using CozyGachapon.Foundation;

namespace CozyGachapon.Tests.EditMode.PetCatalog
{
    [TestFixture]
    public sealed class PetBootStepTests
    {
        private ServiceRegistry services;
        private PetCatalogConfig config;

        [SetUp]
        public void SetUp()
        {
            services = new ServiceRegistry();
            config = ScriptableObject.CreateInstance<PetCatalogConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            if (config != null)
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void BootStep_AttributesMatchGdd()
        {
            var step = new PetCatalogBootStep(config, services);
            Assert.That(step.Id, Is.EqualTo("pet.catalog"));
            Assert.That(step.Phase, Is.EqualTo(BootPhase.DataLoad));
            Assert.That(step.IsCritical, Is.True);
            Assert.That(step.RunsAfter.Count, Is.EqualTo(0));
            Assert.That(step.IsDone, Is.False);
            Assert.That(step.IsFailed, Is.False);
        }

        [Test]
        public void BootStep_ExecuteRegistersDatabase()
        {
            var step = new PetCatalogBootStep(null, services);
            step.Run();

            Assert.That(step.IsDone, Is.True);
            Assert.That(step.IsFailed, Is.False);

            Assert.That(services.TryGet<IPetDatabase>(out var db), Is.True);
            Assert.That(db, Is.Not.Null);
            Assert.That(db.Count, Is.EqualTo(7));
        }
    }
}
