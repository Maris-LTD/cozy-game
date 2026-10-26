using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Economy
{
    [TestFixture]
    public sealed class IdleCpsTests
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
        public void AC20_IdleCps_CalculatesAccurately()
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
        }

        [Test]
        public void EmptyShelf_IdleCpsIsZero()
        {
            IncomePipeline pipeline = new IncomePipeline(_config);
            Assert.AreEqual(0d, pipeline.IdleCps, 1e-4);
        }

        [Test]
        public void PlinkoSources_NotCountedInIdleCps()
        {
            IncomePipeline pipeline = new IncomePipeline(_config);
            pipeline.RegisterIncomeSource(EconomyIds.PlinkoCoinSource, IncomeChannel.Plinko, 50d);
            Assert.AreEqual(0d, pipeline.IdleCps, 1e-4);
        }
    }
}
