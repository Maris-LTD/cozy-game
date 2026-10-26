using System.Collections.Generic;
using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Economy
{
    [TestFixture]
    public sealed class EconomyLifecycleTests
    {
        private const double StepDt = 1d / 60d;
        private EconomyConfig _config;
        private CurrencyDefinition _coinDef;
        private EconomyBalanceTargets _targets;
        private CountingEconomyDiagnosticsSink _sink;
        private FakeSimulationState _simulationState;
        private EconomyCore _core;
        private List<WalletChangedEvent> _walletEvents;
        private double _expectedOneSecondIncome;
        private double _relTol;

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

            _targets = EconomyTestAssets.LoadTargets();
            _relTol = _targets != null ? _targets.ValueRelTolerance : 1e-4d;

            _sink = new CountingEconomyDiagnosticsSink();
            _simulationState = new FakeSimulationState(false);
            _core = new EconomyCore(_sink, _simulationState);
            _core.Initialize(_config, ReleaseTier.Mvp, null, _simulationState);
            _core.IncomePipeline.RegisterIncomeSource("shelf.slot1.idle", IncomeChannel.Idle, 15d);
            _expectedOneSecondIncome = _core.IncomePipeline.IdleCps;

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
        public void AC22a_ReadyBeforeInteractive_NoAccrualFor600Steps_ThenAccruesAfterInteractive()
        {
            Assert.AreEqual(EconomyRunState.Ready, _core.RunState);

            for (int i = 0; i < 600; i++)
            {
                _core.Tick(StepDt);
            }

            Assert.AreEqual(EconomyRunState.Ready, _core.RunState);
            Assert.AreEqual(0d, _core.GetBalance(EconomyIds.Coin));
            Assert.AreEqual(0, _walletEvents.Count);

            _simulationState.IsSimulationRunning = true;
            _core.HandleInteractive(new InteractiveEvent(1.0f));
            Assert.AreEqual(EconomyRunState.Running, _core.RunState);

            for (int i = 0; i < 60; i++)
            {
                _core.Tick(StepDt);
            }

            double tol = EconomyTestAssets.RelativeTolerance(_expectedOneSecondIncome, _relTol);
            Assert.AreEqual(_expectedOneSecondIncome, _core.GetBalance(EconomyIds.Coin), tol);
            Assert.AreEqual(60, _walletEvents.Count);
        }

        [Test]
        public void AC22b_RunningToPaused_NoAccrualDuringPause_ResumedAccruesWithoutAwaySeconds()
        {
            _simulationState.IsSimulationRunning = true;
            _core.HandleInteractive(new InteractiveEvent(0.5f));
            Assert.AreEqual(EconomyRunState.Running, _core.RunState);

            _simulationState.IsSimulationRunning = false;
            _core.HandlePaused(new PausedEvent(PauseReason.Hidden));
            Assert.AreEqual(EconomyRunState.Paused, _core.RunState);

            for (int i = 0; i < 600; i++)
            {
                _core.Tick(StepDt);
            }

            Assert.AreEqual(0d, _core.GetBalance(EconomyIds.Coin));
            Assert.AreEqual(0, _walletEvents.Count);

            _simulationState.IsSimulationRunning = true;
            _core.HandleResumed(new ResumedEvent(PauseReason.Hidden, 10d));
            Assert.AreEqual(EconomyRunState.Running, _core.RunState);
            Assert.AreEqual(0d, _core.GetBalance(EconomyIds.Coin));
            Assert.AreEqual(0, _walletEvents.Count);

            for (int i = 0; i < 60; i++)
            {
                _core.Tick(StepDt);
            }

            double tol = EconomyTestAssets.RelativeTolerance(_expectedOneSecondIncome, _relTol);
            Assert.AreEqual(_expectedOneSecondIncome, _core.GetBalance(EconomyIds.Coin), tol);
            Assert.AreEqual(60, _walletEvents.Count);
        }

        [Test]
        public void AC22c_PausedBeforeInteractive_ResumedReturnsToReady_AccruesOnlyAfterInteractive()
        {
            Assert.AreEqual(EconomyRunState.Ready, _core.RunState);

            _core.HandlePaused(new PausedEvent(PauseReason.Hidden));
            Assert.AreEqual(EconomyRunState.Paused, _core.RunState);

            for (int i = 0; i < 600; i++)
            {
                _core.Tick(StepDt);
            }

            _simulationState.IsSimulationRunning = false;
            _core.HandleResumed(new ResumedEvent(PauseReason.Hidden, 10d));
            Assert.AreEqual(EconomyRunState.Ready, _core.RunState);

            for (int i = 0; i < 600; i++)
            {
                _core.Tick(StepDt);
            }

            Assert.AreEqual(0d, _core.GetBalance(EconomyIds.Coin));
            Assert.AreEqual(0, _walletEvents.Count);

            _simulationState.IsSimulationRunning = true;
            _core.HandleInteractive(new InteractiveEvent(2.0f));
            Assert.AreEqual(EconomyRunState.Running, _core.RunState);

            for (int i = 0; i < 60; i++)
            {
                _core.Tick(StepDt);
            }

            double tol = EconomyTestAssets.RelativeTolerance(_expectedOneSecondIncome, _relTol);
            Assert.AreEqual(_expectedOneSecondIncome, _core.GetBalance(EconomyIds.Coin), tol);
            Assert.AreEqual(60, _walletEvents.Count);
        }

        [Test]
        public void AC22d_RunningWithIsSimulationRunningFalse_DoesNotAccrue()
        {
            _simulationState.IsSimulationRunning = true;
            _core.HandleInteractive(new InteractiveEvent(0.5f));
            Assert.AreEqual(EconomyRunState.Running, _core.RunState);

            _simulationState.IsSimulationRunning = false;

            for (int i = 0; i < 60; i++)
            {
                _core.Tick(StepDt);
            }

            Assert.AreEqual(0d, _core.GetBalance(EconomyIds.Coin));
            Assert.AreEqual(0, _walletEvents.Count);
        }
    }
}
