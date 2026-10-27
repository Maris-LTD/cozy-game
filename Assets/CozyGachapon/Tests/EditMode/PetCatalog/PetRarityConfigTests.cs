using System;
using NUnit.Framework;
using UnityEngine;
using CozyGachapon.Contracts.PetCatalog;
using CozyGachapon.Core.PetCatalog;

namespace CozyGachapon.Tests.EditMode.PetCatalog
{
    [TestFixture]
    public sealed class PetRarityConfigTests
    {
        private PetRarityConfig config;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<PetRarityConfig>();
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
        public void DefaultValues_MatchGddTable()
        {
            Assert.That(PetRarityConfig.CommonCoefficient, Is.EqualTo(1.0));
            Assert.That(config.GetCoefficient(PetRarity.Common), Is.EqualTo(1.0));
            Assert.That(config.GetCoefficient(PetRarity.Rare), Is.EqualTo(2.5));
            Assert.That(config.GetCoefficient(PetRarity.Epic), Is.EqualTo(6.0));
            Assert.That(config.GetCoefficient(PetRarity.Legendary), Is.EqualTo(15.0));
        }

        [Test]
        public void CustomCoefficients_AreRespected()
        {
            config.SetCoefficients(3.0, 7.5, 20.0);
            Assert.That(config.GetCoefficient(PetRarity.Common), Is.EqualTo(1.0));
            Assert.That(config.GetCoefficient(PetRarity.Rare), Is.EqualTo(3.0));
            Assert.That(config.GetCoefficient(PetRarity.Epic), Is.EqualTo(7.5));
            Assert.That(config.GetCoefficient(PetRarity.Legendary), Is.EqualTo(20.0));
        }

        [Test]
        public void InvalidRarity_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => config.GetCoefficient(PetRarity.None));
            Assert.Throws<ArgumentOutOfRangeException>(() => config.GetCoefficient((PetRarity)999));
        }
    }
}
