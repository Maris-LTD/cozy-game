using System.Collections.Generic;
using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Economy
{
    [TestFixture]
    public sealed class PausedResolveTests
    {
        private EconomyConfig _config;
        private CurrencyDefinition _coinDef;
        private CountingEconomyDiagnosticsSink _sink;
        private FakeSimulationState _simulationState;
        private EconomyCore _core;
        private List<IncomeCreditedEvent> _creditedEvents;
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
            _config.SetBoostTierCapForTest(3d);

            _sink = new CountingEconomyDiagnosticsSink();
            _simulationState = new FakeSimulationState(false);
            _core = new EconomyCore(_sink, _simulationState);
            _core.Initialize(_config, ReleaseTier.Mvp, null, _simulationState);
            _core.IncomePipeline.RegisterIncomeSource("shelf.slot1.idle", IncomeChannel.Idle, 25d);

            IncomeModifier boost = IncomeModifier.ForChannel(
                "boost.tea",
                IncomeChannel.Plinko,
                ModifierTier.Boost,
                1.0d);
            _core.IncomePipeline.RegisterModifier(in boost);

            _creditedEvents = new List<IncomeCreditedEvent>();
            _walletEvents = new List<WalletChangedEvent>();
            _core.IncomeCredited += _creditedEvents.Add;
            _core.WalletChanged += _walletEvents.Add;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_coinDef);
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void AC49a_CoinResolvedOverflow_WhenReady_CreditsPlinko_KeepsReady_AndNoIdleAccrual()
        {
            Assert.AreEqual(EconomyRunState.Ready, _core.RunState);

            CoinResolvedEvent overflowEvt = new CoinResolvedEvent(
                "coin.overflow.ready",
                CoinOrigin.Auto,
                CoinResolvedEvent.NoBucket,
                1d,
                5d,
                0,
                CoinResolveReason.Overflow);

            bool handled = _core.HandleCoinResolved(in overflowEvt);
            Assert.IsTrue(handled);
            _core.Tick(1d / 60d);

            double expectedCredit = 5d * _core.BaseCoinValue * 2d;
            Assert.AreEqual(expectedCredit, _core.GetBalance(EconomyIds.Coin), 1e-6d);
            Assert.AreEqual(1, _creditedEvents.Count);
            Assert.AreEqual(CoinResolvedEvent.NoBucket, _creditedEvents[0].BucketIndex);
            Assert.AreEqual(CoinResolveReason.Overflow, _creditedEvents[0].Reason);
            Assert.AreEqual(expectedCredit, _creditedEvents[0].CreditedAmount, 1e-6d);
            Assert.AreEqual(EconomyRunState.Ready, _core.RunState);
            Assert.AreEqual(1, _walletEvents.Count);
            Assert.AreEqual(WalletChangeReason.PlinkoIncome, _walletEvents[0].Reason);
        }

        [Test]
        public void AC49b_CoinResolvedOverflow_WhenPaused_CreditsPlinko_KeepsPaused_AndNoIdleAccrual()
        {
            _core.HandlePaused(new PausedEvent(PauseReason.Hidden));
            Assert.AreEqual(EconomyRunState.Paused, _core.RunState);

            CoinResolvedEvent overflowEvt = new CoinResolvedEvent(
                "coin.overflow.paused",
                CoinOrigin.Manual,
                CoinResolvedEvent.NoBucket,
                1d,
                7d,
                0,
                CoinResolveReason.Overflow);

            bool handled = _core.HandleCoinResolved(in overflowEvt);
            Assert.IsTrue(handled);
            _core.Tick(1d / 60d);

            double expectedCredit = 7d * _core.BaseCoinValue * 2d;
            Assert.AreEqual(expectedCredit, _core.GetBalance(EconomyIds.Coin), 1e-6d);
            Assert.AreEqual(1, _creditedEvents.Count);
            Assert.AreEqual(CoinResolvedEvent.NoBucket, _creditedEvents[0].BucketIndex);
            Assert.AreEqual(CoinResolveReason.Overflow, _creditedEvents[0].Reason);
            Assert.AreEqual(expectedCredit, _creditedEvents[0].CreditedAmount, 1e-6d);
            Assert.AreEqual(EconomyRunState.Paused, _core.RunState);
            Assert.AreEqual(1, _walletEvents.Count);
            Assert.AreEqual(WalletChangeReason.PlinkoIncome, _walletEvents[0].Reason);
        }

        [Test]
        public void CoinResolved_WhenUninitialized_IsRejected()
        {
            EconomyCore uninitialized = new EconomyCore(_sink);
            Assert.AreEqual(EconomyRunState.Uninitialized, uninitialized.RunState);

            CoinResolvedEvent evt = new CoinResolvedEvent(
                "coin.uninit",
                CoinOrigin.Manual,
                CoinResolvedEvent.NoBucket,
                1d,
                5d,
                0,
                CoinResolveReason.Overflow);

            Assert.IsFalse(uninitialized.HandleCoinResolved(in evt));
            Assert.AreEqual(0d, uninitialized.GetBalance(EconomyIds.Coin));
        }
    }
}
