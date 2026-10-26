using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Economy
{
    [TestFixture]
    public sealed class AdIdleCompensationTests
    {
        private EconomyConfig _config;
        private CurrencyDefinition _coinDef;
        private CountingEconomyDiagnosticsSink _sink;
        private FakeSimulationState _simulationState;
        private EconomyCore _core;
        private RecordingWalletEvents _recorder;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<EconomyConfig>();
            _coinDef = ScriptableObject.CreateInstance<CurrencyDefinition>();
            _coinDef.SetIdForTest(EconomyIds.Coin);
            _config.SetCurrenciesForTest(new[] { _coinDef });
            _config.SetStartingCoinsForTest(0d);
            _config.SetWalletSafeCapForTest(1e300d);
            _config.SetIdleCpsUnitForTest(2d);
            _config.SetMaxFrameDeltaForTest(0.25d);
            _config.SetAdCompensationMaxSecondsForTest(90d);
            _config.SetBoostTierCapForTest(3d);

            _sink = new CountingEconomyDiagnosticsSink();
            _simulationState = new FakeSimulationState(true, false);
            _core = new EconomyCore(_sink, _simulationState);
            _core.Initialize(_config, ReleaseTier.Mvp, null, _simulationState);
            _core.IncomePipeline.RegisterIncomeSource("shelf.slot1.idle", IncomeChannel.Idle, 58d);

            IncomeModifier plinkoBoost = IncomeModifier.ForChannel("boost.plinko", IncomeChannel.Plinko, ModifierTier.Boost, 0.5d);
            _core.IncomePipeline.RegisterModifier(in plinkoBoost);

            _recorder = new RecordingWalletEvents();
            _core.WalletChanged += _recorder.OnWalletChanged;
            _core.IncomeCredited += _recorder.OnIncomeCredited;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_coinDef);
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void AC48a_AdClosed30_WithZeroFramesRun_Compensates1740WithSingleWalletChanged()
        {
            Assert.AreEqual(58d, _core.IncomePipeline.IdleCps, 1e-6d);
            double plinkoMultBefore = _core.IncomePipeline.GetTierMultiplier(ModifierTier.Boost, IncomeChannel.Plinko);

            _simulationState.IsAdOpen = true;
            _simulationState.IsAdOpen = false;
            double comp = _core.HandleAdClosed(new AdClosedEvent(30d));

            Assert.AreEqual(1740d, comp, 1e-6d);
            Assert.AreEqual(1740d, _core.GetBalance(EconomyIds.Coin), 1e-6d);
            Assert.AreEqual(1, _recorder.WalletChanges.Count);
            Assert.AreEqual(WalletChangeReason.AdIdleCompensation, _recorder.WalletChanges[0].Reason);
            Assert.AreEqual(1740d, _recorder.WalletChanges[0].Delta, 1e-6d);
            Assert.AreEqual(plinkoMultBefore, _core.IncomePipeline.GetTierMultiplier(ModifierTier.Boost, IncomeChannel.Plinko), 1e-6d);
        }

        [Test]
        public void AC48b_AdClosed30_After1800StepsDuringAd_CompensatesZeroAndAvoidsDoubleCounting()
        {
            _simulationState.IsAdOpen = true;
            for (int i = 0; i < 1800; i++)
            {
                _core.Tick(1d / 60d);
            }

            double accruedDuringAd = _core.GetBalance(EconomyIds.Coin);
            Assert.AreEqual(1740d, accruedDuringAd, 1e-3d);
            int changesBeforeClose = _recorder.WalletChanges.Count;

            _simulationState.IsAdOpen = false;
            double comp = _core.HandleAdClosed(new AdClosedEvent(30d));

            Assert.AreEqual(0d, comp, 1e-6d);
            Assert.AreEqual(1740d, _core.GetBalance(EconomyIds.Coin), 1e-3d);
            Assert.AreEqual(changesBeforeClose, _recorder.WalletChanges.Count);
        }

        [Test]
        public void AC48c_AdClosed200_ClampsToMaxSeconds90_Compensates5220()
        {
            _simulationState.IsAdOpen = true;
            _simulationState.IsAdOpen = false;
            double comp = _core.HandleAdClosed(new AdClosedEvent(200d));

            Assert.AreEqual(5220d, comp, 1e-6d);
            Assert.AreEqual(5220d, _core.GetBalance(EconomyIds.Coin), 1e-6d);
            Assert.AreEqual(1, _recorder.WalletChanges.Count);
            Assert.AreEqual(WalletChangeReason.AdIdleCompensation, _recorder.WalletChanges[0].Reason);
        }

        [Test]
        public void AC48d_AdClosed30_WhenSimulationNotRunning_CompensatesZero()
        {
            _simulationState.IsSimulationRunning = false;
            double comp = _core.HandleAdClosed(new AdClosedEvent(30d));

            Assert.AreEqual(0d, comp);
            Assert.AreEqual(0d, _core.GetBalance(EconomyIds.Coin));
            Assert.AreEqual(0, _recorder.WalletChanges.Count);
        }

        [Test]
        public void AC48e_AdClosedNaNAndNegative_CompensatesZero()
        {
            Assert.AreEqual(0d, _core.HandleAdClosed(new AdClosedEvent(double.NaN)));
            Assert.AreEqual(0d, _core.HandleAdClosed(new AdClosedEvent(-5d)));
            Assert.AreEqual(0d, _core.HandleAdClosed(new AdClosedEvent(double.PositiveInfinity)));
            Assert.AreEqual(0d, _core.GetBalance(EconomyIds.Coin));
            Assert.AreEqual(0, _recorder.WalletChanges.Count);
        }
    }
}
