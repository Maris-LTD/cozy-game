using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Economy
{
    [TestFixture]
    public sealed class ModifierReplacementTests
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
        public void AC14_DuplicateRegistration_ReplacesRatherThanDuplicates()
        {
            IncomePipeline pipeline = new IncomePipeline(_config);
            pipeline.RegisterIncomeSource("source.A", 10d);
            Assert.AreEqual(10d, pipeline.IdleCps, 1e-4);

            IncomeModifier first = new IncomeModifier(
                "shelf.dog",
                IncomeChannel.Idle,
                ModifierTier.Synergy,
                0.5d,
                ModifierTarget.Channel,
                string.Empty,
                false);
            Assert.IsTrue(pipeline.RegisterModifier(in first));
            Assert.AreEqual(15d, pipeline.IdleCps, 1e-4);

            IncomeModifier second = new IncomeModifier(
                "shelf.dog",
                IncomeChannel.Idle,
                ModifierTier.Synergy,
                0.5d,
                ModifierTarget.Channel,
                string.Empty,
                false);
            Assert.IsTrue(pipeline.RegisterModifier(in second));
            Assert.AreEqual(15d, pipeline.IdleCps, 1e-4);

            int removed = pipeline.Unregister("shelf.dog");
            Assert.AreEqual(1, removed);
            Assert.AreEqual(10d, pipeline.IdleCps, 1e-4);

            int removedMissing = pipeline.Unregister("khong.ton.tai");
            Assert.AreEqual(0, removedMissing);
            Assert.AreEqual(10d, pipeline.IdleCps, 1e-4);
        }

        [Test]
        public void AC14_SameSourceIdDifferentTarget_AreSeparateModifiersAndUnregisteredTogether()
        {
            IncomePipeline pipeline = new IncomePipeline(_config);
            pipeline.RegisterIncomeSource("source.A", 10d);
            pipeline.RegisterIncomeSource("source.B", 10d);

            IncomeModifier modTargetA = new IncomeModifier(
                "shelf.combo",
                IncomeChannel.Idle,
                ModifierTier.Synergy,
                0.5d,
                ModifierTarget.Source,
                "source.A",
                false);
            pipeline.RegisterModifier(in modTargetA);

            IncomeModifier modTargetB = new IncomeModifier(
                "shelf.combo",
                IncomeChannel.Idle,
                ModifierTier.Synergy,
                0.5d,
                ModifierTarget.Source,
                "source.B",
                false);
            pipeline.RegisterModifier(in modTargetB);

            Assert.AreEqual(30d, pipeline.IdleCps, 1e-4);

            int removed = pipeline.Unregister("shelf.combo");
            Assert.AreEqual(2, removed);
            Assert.AreEqual(20d, pipeline.IdleCps, 1e-4);
        }
    }
}
