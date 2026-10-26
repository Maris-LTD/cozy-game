using System;
using System.Collections.Generic;
using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Economy
{
    [TestFixture]
    public sealed class IncomeRatesPublishTests
    {
        private EconomyConfig _config;
        private CurrencyDefinition _coinDef;
        private CountingEconomyDiagnosticsSink _sink;
        private FakeSimulationState _simulationState;
        private EconomyCore _core;
        private List<IncomeRatesChangedEvent> _rateEvents;

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
            _config.SetSynergyTierCapForTest(10d);

            _sink = new CountingEconomyDiagnosticsSink();
            _simulationState = new FakeSimulationState(true, false);
            _core = new EconomyCore(_sink, _simulationState);
            _core.Initialize(_config, ReleaseTier.Mvp, null, _simulationState);
            _core.IncomePipeline.RegisterIncomeSource("shelf.slot1.idle", IncomeChannel.Idle, 10d);

            _rateEvents = new List<IncomeRatesChangedEvent>();
            _core.IncomeRatesChanged += _rateEvents.Add;
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_coinDef);
            UnityEngine.Object.DestroyImmediate(_config);
        }

        [Test]
        public void AC26_ModifierChangedEveryFrameFor10s_PublishesAtMostCeilOverIntervalPlusOne_AndWithinInterval()
        {
            const double dt = 1d / 60d;
            const int totalFrames = 600;
            double interval = _config.RatesPublishInterval;
            int maxAllowedEvents = (int)Math.Ceiling(10d / interval) + 1;

            double timeSinceLastChange = 0d;
            double latestExpectedIdleCps = _core.IncomePipeline.IdleCps;

            for (int frame = 0; frame < totalFrames; frame++)
            {
                double modValue = (frame + 1) * 0.005d;
                IncomeModifier mod = IncomeModifier.ForSource(
                    "shelf.dynamic",
                    IncomeChannel.Idle,
                    ModifierTier.Synergy,
                    modValue,
                    "shelf.slot1.idle");
                _core.IncomePipeline.RegisterModifier(in mod);
                latestExpectedIdleCps = _core.IncomePipeline.IdleCps;

                int countBefore = _rateEvents.Count;
                _core.Tick(dt);
                timeSinceLastChange += dt;

                if (_rateEvents.Count > countBefore)
                {
                    IncomeRatesChangedEvent lastEvt = _rateEvents[_rateEvents.Count - 1];
                    Assert.AreEqual(latestExpectedIdleCps, lastEvt.IdleCps, 1e-6d);
                    Assert.LessOrEqual(timeSinceLastChange, interval + 1e-6d);
                    timeSinceLastChange = 0d;
                }
            }

            Assert.Greater(_rateEvents.Count, 0);
            Assert.LessOrEqual(_rateEvents.Count, maxAllowedEvents);
        }

        [Test]
        public void IncomeRatesChanged_WhenNoChangesOccur_DoesNotPublishRepeatedly()
        {
            _core.Tick(0.5d);
            int initialCount = _rateEvents.Count;

            for (int i = 0; i < 120; i++)
            {
                _core.Tick(1d / 60d);
            }

            Assert.AreEqual(initialCount, _rateEvents.Count);
        }
    }
}
