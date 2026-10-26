using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Economy
{
    [TestFixture]
    public sealed class SourceIdleCpsTests
    {
        private const string SourceA = "shelf.slot1.idle";
        private const string SourceB = "shelf.slot2.idle";
        private EconomyConfig _config;
        private EconomyBalanceTargets _targets;
        private double _relTol;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<EconomyConfig>();
            _config.SetIdleCpsUnitForTest(2d);
            _config.SetSynergyTierCapForTest(4d);
            _config.SetBoostTierCapForTest(3d);

            _targets = EconomyTestAssets.LoadTargets();
            _relTol = _targets != null ? _targets.ValueRelTolerance : 1e-4d;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void AC50_GetSourceIdleCps_MatchesPerSourceContributionAndSumsToIdleCps()
        {
            IncomePipeline pipeline = new IncomePipeline(_config);

            pipeline.RegisterIncomeSource(SourceA, 10d);
            pipeline.RegisterIncomeSource(SourceB, 4d);

            IncomeModifier synA = IncomeModifier.ForSource("shelf.synA", IncomeChannel.Idle, ModifierTier.Synergy, 0.5d, SourceA);
            pipeline.RegisterModifier(in synA);

            IncomeModifier flatB = IncomeModifier.ForSource("shelf.flatB", IncomeChannel.Idle, ModifierTier.Base, 1.0d, SourceB);
            pipeline.RegisterModifier(in flatB);

            IncomeModifier synIdle = IncomeModifier.ForChannel("shelf.synIdle", IncomeChannel.Idle, ModifierTier.Synergy, 0.5d);
            pipeline.RegisterModifier(in synIdle);

            IncomeModifier tea = IncomeModifier.ForChannel("boost.tea", IncomeChannel.Both, ModifierTier.Boost, 1.0d, true);
            pipeline.RegisterModifier(in tea);

            Assert.AreEqual(58d, pipeline.IdleCps, EconomyTestAssets.RelativeTolerance(58d, _relTol));

            double cpsA = pipeline.GetSourceIdleCps(SourceA);
            double cpsB = pipeline.GetSourceIdleCps(SourceB);
            double cpsUnknown = pipeline.GetSourceIdleCps("shelf.slot99.idle");

            Assert.AreEqual(40d, cpsA, EconomyTestAssets.RelativeTolerance(40d, _relTol));
            Assert.AreEqual(18d, cpsB, EconomyTestAssets.RelativeTolerance(18d, _relTol));
            Assert.AreEqual(0d, cpsUnknown);
            Assert.AreEqual(pipeline.IdleCps, cpsA + cpsB, EconomyTestAssets.RelativeTolerance(pipeline.IdleCps, _relTol));

            pipeline.UnregisterIncomeSource(SourceB);

            double cpsAAfter = pipeline.GetSourceIdleCps(SourceA);
            double cpsBAfter = pipeline.GetSourceIdleCps(SourceB);
            Assert.AreEqual(40d, cpsAAfter, EconomyTestAssets.RelativeTolerance(40d, _relTol));
            Assert.AreEqual(0d, cpsBAfter);
            Assert.AreEqual(pipeline.IdleCps, cpsAAfter + cpsBAfter, EconomyTestAssets.RelativeTolerance(pipeline.IdleCps, _relTol));
        }

        [Test]
        public void GetSourceIdleCps_SameFrameAfterUpsert_ReflectsImmediateStateWithoutIncrementingRecomputeCount()
        {
            IncomePipeline pipeline = new IncomePipeline(_config);
            pipeline.RegisterIncomeSource(SourceA, 5d);
            Assert.AreEqual(5d, pipeline.IdleCps, 1e-6d);
            int recomputeBefore = pipeline.IdleRecomputeCount;

            pipeline.RegisterIncomeSource(SourceA, 12d);
            Assert.AreEqual(12d, pipeline.GetSourceIdleCps(SourceA), 1e-6d);
            Assert.AreEqual(recomputeBefore, pipeline.IdleRecomputeCount);
        }
    }
}
