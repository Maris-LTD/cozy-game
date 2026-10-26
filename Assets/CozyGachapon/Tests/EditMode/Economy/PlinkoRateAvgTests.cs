using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Economy
{
    [TestFixture]
    public sealed class PlinkoRateAvgTests
    {
        private EconomyConfig _config;
        private CurrencyDefinition _coinDef;
        private CountingEconomyDiagnosticsSink _sink;
        private FakeSimulationState _simulationState;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<EconomyConfig>();
            _coinDef = ScriptableObject.CreateInstance<CurrencyDefinition>();
            _coinDef.SetIdForTest(EconomyIds.Coin);
            _config.SetCurrenciesForTest(new[] { _coinDef });
            _config.SetStartingCoinsForTest(0d);
            _config.SetWalletSafeCapForTest(1e300d);
            _config.SetMaxFrameDeltaForTest(0.25d);
            _config.SetPlinkoRateWindowSecondsForTest(10d);
            _config.SetRatesPublishIntervalForTest(0.5d);

            _sink = new CountingEconomyDiagnosticsSink();
            _simulationState = new FakeSimulationState(true, false);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_coinDef);
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void AC25_PlinkoRateAvg_100PerSecFor10s_300In3s_50In04s_ZeroAfter10sIdle()
        {
            EconomyCore core1 = CreateInitializedCore();
            for (int sec = 0; sec < 10; sec++)
            {
                core1.SubmitIncome(IncomeChannel.Plinko, "shelf.slot1", 100d);
                StepSeconds(core1, 1.0d);
            }

            Assert.AreEqual(100d, core1.IncomePipeline.PlinkoRateAvg, 1e-4d);

            for (int sec = 0; sec < 10; sec++)
            {
                StepSeconds(core1, 1.0d);
            }

            Assert.AreEqual(0d, core1.IncomePipeline.PlinkoRateAvg, 1e-6d);

            EconomyCore core2 = CreateInitializedCore();
            core2.SubmitIncome(IncomeChannel.Plinko, "shelf.slot1", 300d);
            StepSeconds(core2, 3.0d);
            Assert.AreEqual(100d, core2.IncomePipeline.PlinkoRateAvg, 1e-4d);

            EconomyCore core3 = CreateInitializedCore();
            core3.SubmitIncome(IncomeChannel.Plinko, "shelf.slot1", 50d);
            StepSeconds(core3, 0.4d);
            Assert.AreEqual(50d, core3.IncomePipeline.PlinkoRateAvg, 1e-4d);
        }

        [Test]
        public void PlinkoRateAvg_IdleIncomeDoesNotAffectPlinkoRateAvg()
        {
            EconomyCore core = CreateInitializedCore();
            core.IncomePipeline.RegisterIncomeSource("shelf.slot1.idle", IncomeChannel.Idle, 50d);
            StepSeconds(core, 5.0d);

            Assert.AreEqual(50d, core.IncomePipeline.IdleCps, 1e-4d);
            Assert.AreEqual(0d, core.IncomePipeline.PlinkoRateAvg, 1e-6d);
        }

        private EconomyCore CreateInitializedCore()
        {
            EconomyCore core = new EconomyCore(_sink, _simulationState);
            core.Initialize(_config, ReleaseTier.Mvp, null, _simulationState);
            return core;
        }

        private static void StepSeconds(EconomyCore core, double seconds)
        {
            int steps = (int)System.Math.Round(seconds * 60d);
            for (int i = 0; i < steps; i++)
            {
                core.Tick(1d / 60d);
            }
        }
    }
}
