using System.Collections.Generic;
using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Economy
{
    [TestFixture]
    public sealed class TierOwnershipTests
    {
        private EconomyConfig _config;
        private CountingEconomyDiagnosticsSink _sink;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<EconomyConfig>();
            _config.SetIdleCpsUnitForTest(2d);
            _config.SetSynergyTierCapForTest(4d);
            _config.SetBoostTierCapForTest(3d);
            _config.SetPrestigeTierCapForTest(10d);
            _config.SetGlobalTierCapForTest(2d);
            _sink = new CountingEconomyDiagnosticsSink();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void AC13_TierOwnership_AllowedAndRejectedModifiers()
        {
            IncomePipeline pipeline = new IncomePipeline(_config, _sink);
            pipeline.RegisterIncomeSource("source.A", 10d);
            Assert.AreEqual(10d, pipeline.IdleCps, 1e-4);

            IncomeModifier shelfBoost = new IncomeModifier(
                "shelf.boostTest",
                IncomeChannel.Idle,
                ModifierTier.Boost,
                1.0d,
                ModifierTarget.Channel,
                string.Empty,
                true);
            Assert.IsFalse(pipeline.RegisterModifier(in shelfBoost));
            Assert.AreEqual(10d, pipeline.IdleCps, 1e-4);

            IncomeModifier shelfPrestige = new IncomeModifier(
                "shelf.prestigeTest",
                IncomeChannel.Idle,
                ModifierTier.Prestige,
                1.0d,
                ModifierTarget.Channel,
                string.Empty,
                false);
            Assert.IsFalse(pipeline.RegisterModifier(in shelfPrestige));
            Assert.AreEqual(10d, pipeline.IdleCps, 1e-4);

            IncomeModifier shelfGlobal = new IncomeModifier(
                "shelf.globalTest",
                IncomeChannel.Idle,
                ModifierTier.Global,
                1.0d,
                ModifierTarget.Channel,
                string.Empty,
                false);
            Assert.IsFalse(pipeline.RegisterModifier(in shelfGlobal));
            Assert.AreEqual(10d, pipeline.IdleCps, 1e-4);

            IncomeModifier boostSynergy = new IncomeModifier(
                "boost.synergyTest",
                IncomeChannel.Idle,
                ModifierTier.Synergy,
                1.0d,
                ModifierTarget.Channel,
                string.Empty,
                false);
            Assert.IsFalse(pipeline.RegisterModifier(in boostSynergy));
            Assert.AreEqual(10d, pipeline.IdleCps, 1e-4);

            Assert.AreEqual(4, _sink.CountWarnings(EconomyDiagnosticCode.ModifierTierRejected));

            IncomeModifier adBoost = new IncomeModifier(
                "ad.coffee",
                IncomeChannel.Idle,
                ModifierTier.Boost,
                1.0d,
                ModifierTarget.Channel,
                string.Empty,
                true);
            Assert.IsTrue(pipeline.RegisterModifier(in adBoost));
            Assert.AreEqual(20d, pipeline.IdleCps, 1e-4);

            IncomeModifier shelfSynergy = new IncomeModifier(
                "shelf.cat",
                IncomeChannel.Idle,
                ModifierTier.Synergy,
                0.5d,
                ModifierTarget.Channel,
                string.Empty,
                false);
            Assert.IsTrue(pipeline.RegisterModifier(in shelfSynergy));
            Assert.AreEqual(30d, pipeline.IdleCps, 1e-4);
        }

        [Test]
        public void AC13_Validator_ReportsError_WhenShelfInGlobal()
        {
            List<EconomyConfigIssue> results = new List<EconomyConfigIssue>();
            TierOwnerPrefixValidator.Validate(_config.TierOwnerPrefixes, results);
            Assert.AreEqual(0, results.Count);

            _config.TierOwnerPrefixes.SetGlobalForTest(new[] { "shelf.", "event." });
            results.Clear();
            TierOwnerPrefixValidator.Validate(_config.TierOwnerPrefixes, results);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(EconomyConfigIssueCode.TierPrefixForbidden, results[0].Code);
            Assert.AreEqual("Global", results[0].Subject);
        }

        [Test]
        public void TierOwnership_EdgeCases_CaseSensitivityAndNull()
        {
            IncomePipeline pipeline = new IncomePipeline(_config, _sink);
            pipeline.RegisterIncomeSource("source.A", 10d);

            IncomeModifier uppercaseShelf = new IncomeModifier(
                "SHELF.cat",
                IncomeChannel.Idle,
                ModifierTier.Synergy,
                0.5d,
                ModifierTarget.Channel,
                string.Empty,
                false);
            Assert.IsFalse(pipeline.RegisterModifier(in uppercaseShelf));
            Assert.AreEqual(10d, pipeline.IdleCps, 1e-4);

            IncomeModifier emptySource = new IncomeModifier(
                string.Empty,
                IncomeChannel.Idle,
                ModifierTier.Synergy,
                0.5d,
                ModifierTarget.Channel,
                string.Empty,
                false);
            Assert.IsFalse(pipeline.RegisterModifier(in emptySource));
            Assert.AreEqual(10d, pipeline.IdleCps, 1e-4);
        }
    }
}
