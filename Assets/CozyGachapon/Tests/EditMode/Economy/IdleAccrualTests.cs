using System.Collections.Generic;
using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Economy
{
    [TestFixture]
    public sealed class IdleAccrualTests
    {
        private EconomyConfig _config;
        private CurrencyDefinition _coinDef;
        private CountingEconomyDiagnosticsSink _sink;
        private FakeSimulationState _simulationState;
        private EconomyCore _core;
        private List<WalletChangedEvent> _walletEvents;

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

            _sink = new CountingEconomyDiagnosticsSink();
            _simulationState = new FakeSimulationState(true);
            _core = new EconomyCore(_sink, _simulationState);
            _core.Initialize(_config, ReleaseTier.Mvp, null, _simulationState);

            _walletEvents = new List<WalletChangedEvent>();
            _core.WalletChanged += _walletEvents.Add;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_coinDef);
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void AC21_IdleAccrual_60FramesNormal_1FrameSpike_NegativeAndNaN_MatchesExpectedDeltas()
        {
            _core.IncomePipeline.RegisterIncomeSource("shelf.slot1.idle", IncomeChannel.Idle, 58d);
            Assert.AreEqual(58d, _core.IncomePipeline.IdleCps, 1e-6d);
            Assert.AreEqual(EconomyRunState.Running, _core.RunState);

            for (int i = 0; i < 60; i++)
            {
                _core.Tick(1d / 60d);
            }

            Assert.AreEqual(58d, _core.GetBalance(EconomyIds.Coin), 1e-6d);
            Assert.AreEqual(60, _walletEvents.Count);
            Assert.AreEqual(WalletChangeReason.IdleIncome, _walletEvents[0].Reason);

            double beforeSpike = _core.GetBalance(EconomyIds.Coin);
            _core.Tick(1.0d);
            double expectedSpikeDelta = 58d * _config.MaxFrameDelta;
            Assert.AreEqual(14.5d, expectedSpikeDelta, 1e-6d);
            Assert.AreEqual(beforeSpike + expectedSpikeDelta, _core.GetBalance(EconomyIds.Coin), 1e-6d);
            Assert.AreEqual(61, _walletEvents.Count);

            double beforeInvalid = _core.GetBalance(EconomyIds.Coin);
            _core.Tick(-0.5d);
            _core.Tick(0d);
            _core.Tick(double.NaN);
            _core.Tick(double.PositiveInfinity);
            _core.Tick(double.NegativeInfinity);

            Assert.AreEqual(beforeInvalid, _core.GetBalance(EconomyIds.Coin), 1e-6d);
            Assert.AreEqual(61, _walletEvents.Count);
        }

        [Test]
        public void IdleAccrual_WhenIdleCpsIsZero_DoesNotRaiseWalletChanged()
        {
            Assert.AreEqual(0d, _core.IncomePipeline.IdleCps, 1e-6d);

            double dt = _core.Tick(1d / 60d);
            Assert.AreEqual(1d / 60d, dt, 1e-6d);
            Assert.AreEqual(0d, _core.GetBalance(EconomyIds.Coin));
            Assert.AreEqual(0, _walletEvents.Count);
        }
    }
}
