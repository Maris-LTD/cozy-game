using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Economy
{
    [TestFixture]
    public sealed class OfflineRateSnapshotTests
    {
        private EconomyConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<EconomyConfig>();
            _config.SetIdleCpsUnitForTest(2d);
            _config.SetSynergyTierCapForTest(4d);
            _config.SetBoostTierCapForTest(3d);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void AC27_OfflineRateSnapshot_ExcludesTemporaryModifiers()
        {
            IncomePipeline pipeline = new IncomePipeline(_config);

            pipeline.RegisterIncomeSource("source.A", 10d);
            pipeline.RegisterIncomeSource("source.B", 4d);

            IncomeModifier synergyA = new IncomeModifier(
                "shelf.synA",
                IncomeChannel.Idle,
                ModifierTier.Synergy,
                0.5d,
                ModifierTarget.Source,
                "source.A",
                false);
            pipeline.RegisterModifier(in synergyA);

            IncomeModifier flatB = new IncomeModifier(
                "shelf.flatB",
                IncomeChannel.Idle,
                ModifierTier.Base,
                1.0d,
                ModifierTarget.Source,
                "source.B",
                false);
            pipeline.RegisterModifier(in flatB);

            IncomeModifier synergyIdle = new IncomeModifier(
                "shelf.synIdle",
                IncomeChannel.Idle,
                ModifierTier.Synergy,
                0.5d,
                ModifierTarget.Channel,
                string.Empty,
                false);
            pipeline.RegisterModifier(in synergyIdle);

            IncomeModifier tea = new IncomeModifier(
                "boost.tea",
                IncomeChannel.Both,
                ModifierTier.Boost,
                1.0d,
                ModifierTarget.Channel,
                string.Empty,
                true);
            pipeline.RegisterModifier(in tea);

            Assert.AreEqual(58d, pipeline.IdleCps, 1e-4);
            Assert.AreEqual(29d, pipeline.OfflineRateSnapshot, 1e-4);
            Assert.LessOrEqual(pipeline.OfflineRateSnapshot, pipeline.IdleCps);

            pipeline.Unregister("boost.tea");
            Assert.AreEqual(29d, pipeline.IdleCps, 1e-4);
            Assert.AreEqual(29d, pipeline.OfflineRateSnapshot, 1e-4);
        }

        [Test]
        public void OfflineRateSnapshot_TemporaryModifierInNonBoostTier_IsExcluded()
        {
            IncomePipeline pipeline = new IncomePipeline(_config);
            pipeline.RegisterIncomeSource("source.A", 10d);

            IncomeModifier tempSynergy = new IncomeModifier(
                "shelf.temp",
                IncomeChannel.Idle,
                ModifierTier.Synergy,
                1.0d,
                ModifierTarget.Channel,
                string.Empty,
                true);
            pipeline.RegisterModifier(in tempSynergy);

            Assert.AreEqual(20d, pipeline.IdleCps, 1e-4);
            Assert.AreEqual(10d, pipeline.OfflineRateSnapshot, 1e-4);
        }
    }
}
