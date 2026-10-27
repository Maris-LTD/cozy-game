using System;
using NUnit.Framework;
using UnityEngine;
using CozyGachapon.Contracts.PetCatalog;
using CozyGachapon.Core.PetCatalog;

namespace CozyGachapon.Tests.EditMode.PetCatalog
{
    [TestFixture]
    public sealed class PetPowerFormulaTests
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
        public void RoleWeights_MatchGdd()
        {
            Assert.That(PetPowerFormula.GetRoleWeight(PetRole.GoldBooster), Is.EqualTo(1.0));
            Assert.That(PetPowerFormula.GetRoleWeight(PetRole.Multiplier), Is.EqualTo(1.2));
            Assert.That(PetPowerFormula.GetRoleWeight(PetRole.Spawner), Is.EqualTo(1.1));
            Assert.That(PetPowerFormula.GetRoleWeight(PetRole.SynergyEnabler), Is.EqualTo(1.3));
            Assert.Throws<ArgumentOutOfRangeException>(() => PetPowerFormula.GetRoleWeight(PetRole.None));
        }

        [Test]
        public void CalculateTargetPower_CommonGoldBooster_Equals10()
        {
            double power = PetPowerFormula.CalculateTargetPower(PetRarity.Common, PetRole.GoldBooster, config);
            Assert.That(power, Is.EqualTo(10.0));
        }

        [Test]
        public void CalculateTargetPower_RareSynergyEnabler_Equals32Point5()
        {
            double power = PetPowerFormula.CalculateTargetPower(PetRarity.Rare, PetRole.SynergyEnabler, config);
            Assert.That(power, Is.EqualTo(32.5));
        }

        [Test]
        public void CalculateTargetPower_EpicMultiplier_Equals72()
        {
            double power = PetPowerFormula.CalculateTargetPower(PetRarity.Epic, PetRole.Multiplier, config);
            Assert.That(power, Is.EqualTo(72.0));
        }

        [Test]
        public void CalculateTargetPower_LegendarySynergyEnabler_Equals195()
        {
            double power = PetPowerFormula.CalculateTargetPower(PetRarity.Legendary, PetRole.SynergyEnabler, config);
            Assert.That(power, Is.EqualTo(195.0));
        }

        [Test]
        public void EnumsHaveNoneAsZero()
        {
            Assert.That((int)PetRarity.None, Is.EqualTo(0));
            Assert.That((int)PetGroup.None, Is.EqualTo(0));
            Assert.That((int)PetRole.None, Is.EqualTo(0));
            Assert.That((int)PetReleaseTier.None, Is.EqualTo(0));
            Assert.That((int)PetEffectType.None, Is.EqualTo(0));
            Assert.That((int)PetEffectScope.None, Is.EqualTo(0));
        }
    }
}
