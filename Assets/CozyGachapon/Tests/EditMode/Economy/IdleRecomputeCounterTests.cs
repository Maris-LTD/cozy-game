using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Economy
{
    [TestFixture]
    public sealed class IdleRecomputeCounterTests
    {
        private EconomyConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<EconomyConfig>();
            _config.SetIdleCpsUnitForTest(2d);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void AC24_IdleRecomputeCount_IncreasesOnlyOnDirtyQuery()
        {
            IncomePipeline pipeline = new IncomePipeline(_config);
            pipeline.RegisterIncomeSource("source.A", 10d);

            double initial = pipeline.IdleCps;
            Assert.AreEqual(10d, initial, 1e-4);
            int baseCount = pipeline.IdleRecomputeCount;
            Assert.AreEqual(1, baseCount);

            for (int i = 0; i < 600; i++)
            {
                double value = pipeline.IdleCps;
                Assert.AreEqual(10d, value, 1e-4);
            }
            Assert.AreEqual(baseCount, pipeline.IdleRecomputeCount);

            for (int i = 0; i < 3; i++)
            {
                IncomeModifier mod = new IncomeModifier(
                    "shelf.mod" + i,
                    IncomeChannel.Idle,
                    ModifierTier.Synergy,
                    0.1d,
                    ModifierTarget.Channel,
                    string.Empty,
                    false);
                pipeline.RegisterModifier(in mod);
            }

            double afterMods = pipeline.IdleCps;
            Assert.Greater(afterMods, 10d);
            int newCount = pipeline.IdleRecomputeCount;
            int diff = newCount - baseCount;
            Assert.GreaterOrEqual(diff, 1);
            Assert.LessOrEqual(diff, 3);
        }

        [Test]
        public void OfflineRateSnapshot_DoesNotIncrementMainRecomputeCount()
        {
            IncomePipeline pipeline = new IncomePipeline(_config);
            pipeline.RegisterIncomeSource("source.A", 10d);
            double initial = pipeline.IdleCps;
            int countBefore = pipeline.IdleRecomputeCount;

            double snapshot = pipeline.OfflineRateSnapshot;
            Assert.AreEqual(10d, snapshot, 1e-4);
            Assert.AreEqual(countBefore, pipeline.IdleRecomputeCount);
        }

        [Test]
        public void UnregisterIncomeSource_MarksDirtyAndRecomputes()
        {
            IncomePipeline pipeline = new IncomePipeline(_config);
            pipeline.RegisterIncomeSource("source.A", 10d);
            pipeline.RegisterIncomeSource("source.B", 5d);
            Assert.AreEqual(15d, pipeline.IdleCps, 1e-4);
            int countBefore = pipeline.IdleRecomputeCount;

            pipeline.UnregisterIncomeSource("source.B");
            Assert.AreEqual(10d, pipeline.IdleCps, 1e-4);
            Assert.AreEqual(countBefore + 1, pipeline.IdleRecomputeCount);
        }
    }
}
