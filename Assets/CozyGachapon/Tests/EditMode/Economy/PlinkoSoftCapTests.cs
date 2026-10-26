using System;
using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using NUnit.Framework;

namespace CozyGachapon.Tests.Economy
{
    [TestFixture]
    public sealed class PlinkoSoftCapTests
    {
        private EconomyConfig _config;
        private EconomyBalanceTargets _targets;
        private CountingEconomyDiagnosticsSink _sink;
        private FakeSimulationState _simulationState;
        private RecordingWalletEvents _events;
        private EconomyCore _core;

        [SetUp]
        public void SetUp()
        {
            _config = EconomyTestAssets.LoadConfig();
            _targets = EconomyTestAssets.LoadTargets();
            _sink = new CountingEconomyDiagnosticsSink();
            _simulationState = new FakeSimulationState(true, false);
            _events = new RecordingWalletEvents();
            _core = new EconomyCore(_sink, _simulationState);
            _core.Initialize(_config, ReleaseTier.Mvp, null, _simulationState);
            _core.IncomeCredited += _events.OnIncomeCredited;
            _core.WalletChanged += _events.OnWalletChanged;
        }

        [Test]
        public void AC47_PlinkoSoftCap_AboveModelCeiling_NoClampOrDrop()
        {
            double startingBalance = _core.GetBalance(EconomyIds.Coin);
            double ceiling = _targets.PlinkoRawRateCeilingAssumed[3];
            double factor = _targets.PlinkoSoftCapTestFactor;
            double rawValue = factor * ceiling / 60d;
            double dt = 1d / 60d;
            int totalSteps = (int)Math.Round(_config.PlinkoRateWindowSeconds * 60d);

            for (int i = 0; i < totalSteps; i++)
            {
                CoinResolvedEvent evt = new CoinResolvedEvent(
                    "coin.softcap." + i,
                    CoinOrigin.Manual,
                    0,
                    1d,
                    rawValue,
                    1,
                    CoinResolveReason.Bucket);
                Assert.IsTrue(_core.HandleCoinResolved(in evt));
                _core.Tick(dt);
            }

            double expectedDelta = totalSteps * rawValue;
            double actualDelta = _core.GetBalance(EconomyIds.Coin) - startingBalance;
            double expectedRate = factor * ceiling;

            Assert.That(
                actualDelta,
                Is.EqualTo(expectedDelta).Within(EconomyTestAssets.RelativeTolerance(expectedDelta, _targets.ValueRelTolerance)));
            Assert.That(_events.IncomeCredits.Count, Is.EqualTo(totalSteps));

            for (int i = 0; i < _events.IncomeCredits.Count; i++)
            {
                Assert.That(
                    _events.IncomeCredits[i].CreditedAmount,
                    Is.EqualTo(rawValue).Within(EconomyTestAssets.RelativeTolerance(rawValue, _targets.ValueRelTolerance)));
            }

            Assert.That(
                _core.PlinkoRateAvg,
                Is.EqualTo(expectedRate).Within(EconomyTestAssets.RelativeTolerance(expectedRate, _targets.ValueRelTolerance)));
        }
    }
}
