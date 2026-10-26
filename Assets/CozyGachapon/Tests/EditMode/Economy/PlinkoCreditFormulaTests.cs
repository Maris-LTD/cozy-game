using System.Collections.Generic;
using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Economy
{
    [TestFixture]
    public sealed class PlinkoCreditFormulaTests
    {
        private EconomyConfig _config;
        private CurrencyDefinition _coinDef;
        private CountingEconomyDiagnosticsSink _sink;
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
            _config.SetSpinBasePriceForTest(100d);
            _config.SetSpinGrowthForTest(1.05d);
            _config.SetCoinValueExponentForTest(0.6d);
            _config.SetPlinkoFlatPerCoinCapForTest(2.0d);
            _config.SetSynergyTierCapForTest(4d);
            _config.SetBoostTierCapForTest(3d);
            _config.SetPrestigeTierCapForTest(10d);
            _config.SetGlobalTierCapForTest(2d);

            _sink = new CountingEconomyDiagnosticsSink();
            _creditedEvents = new List<IncomeCreditedEvent>();
            _walletEvents = new List<WalletChangedEvent>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_coinDef);
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void AC18_CreditFormula_MatchesGddExamples()
        {
            EconomyStateData stateWith100Spins = new EconomyStateData(new[]
            {
                new CurrencyBalanceEntry(EconomyIds.Coin, 0d)
            }, 100);

            _core = new EconomyCore(_sink);
            _core.Initialize(_config, ReleaseTier.Mvp, stateWith100Spins);
            _core.IncomeCredited += _creditedEvents.Add;

            double baseCoinValue = _core.BaseCoinValue;
            Assert.AreEqual(18.679d, baseCoinValue, 0.05d);

            IncomeModifier synergyMod = IncomeModifier.ForChannel(
                "shelf.combo",
                IncomeChannel.Plinko,
                ModifierTier.Synergy,
                0.5d);
            _core.IncomePipeline.RegisterModifier(in synergyMod);

            IncomeModifier boostMod = IncomeModifier.ForChannel(
                "boost.tea",
                IncomeChannel.Plinko,
                ModifierTier.Boost,
                1.0d);
            _core.IncomePipeline.RegisterModifier(in boostMod);

            double rawValue = 5d * baseCoinValue;

            CoinResolvedEvent evt1 = new CoinResolvedEvent(
                "coin.1",
                CoinOrigin.Manual,
                4,
                5d,
                rawValue,
                10,
                CoinResolveReason.Bucket);
            Assert.IsTrue(_core.HandleCoinResolved(in evt1));
            Assert.AreEqual(280.2d, _creditedEvents[0].CreditedAmount, 0.2d);

            IncomeModifier flatMod04 = IncomeModifier.ForSource(
                "shelf.flat04",
                IncomeChannel.Plinko,
                ModifierTier.Base,
                0.4d,
                EconomyIds.PlinkoCoinSource);
            _core.IncomePipeline.RegisterModifier(in flatMod04);

            CoinResolvedEvent evt2 = new CoinResolvedEvent(
                "coin.2",
                CoinOrigin.Manual,
                4,
                5d,
                rawValue,
                10,
                CoinResolveReason.Bucket);
            Assert.IsTrue(_core.HandleCoinResolved(in evt2));
            Assert.AreEqual(302.6d, _creditedEvents[1].CreditedAmount, 0.2d);

            IncomeModifier flatMod10 = IncomeModifier.ForSource(
                "shelf.flat04",
                IncomeChannel.Plinko,
                ModifierTier.Base,
                10.0d,
                EconomyIds.PlinkoCoinSource);
            _core.IncomePipeline.RegisterModifier(in flatMod10);

            CoinResolvedEvent evt3 = new CoinResolvedEvent(
                "coin.3",
                CoinOrigin.Manual,
                4,
                5d,
                rawValue,
                10,
                CoinResolveReason.Bucket);
            Assert.IsTrue(_core.HandleCoinResolved(in evt3));
            double expectedCapped = (rawValue + 2.0d * baseCoinValue) * 1.5d * 2.0d;
            Assert.AreEqual(expectedCapped, _creditedEvents[2].CreditedAmount, 0.2d);
        }

        [TestCase(-1d)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void InvalidRawValue_IsRejected_NoEventsAndNoBalanceChange(double invalidRaw)
        {
            _core = new EconomyCore(_sink);
            _core.Initialize(_config, ReleaseTier.Mvp, null);
            _core.IncomeCredited += _creditedEvents.Add;
            _core.WalletChanged += _walletEvents.Add;

            CoinResolvedEvent evt = new CoinResolvedEvent(
                "coin.bad",
                CoinOrigin.Manual,
                0,
                1d,
                invalidRaw,
                0,
                CoinResolveReason.Bucket);

            Assert.IsFalse(_core.HandleCoinResolved(in evt));
            Assert.AreEqual(0, _creditedEvents.Count);
            Assert.AreEqual(0, _walletEvents.Count);
            Assert.AreEqual(0d, _core.GetBalance(EconomyIds.Coin));
        }

        [Test]
        public void ZeroRawValue_RaisesCreditedWithZero_DoesNotRaiseWalletChanged()
        {
            _core = new EconomyCore(_sink);
            _core.Initialize(_config, ReleaseTier.Mvp, null);
            _core.IncomeCredited += _creditedEvents.Add;
            _core.WalletChanged += _walletEvents.Add;

            CoinResolvedEvent evt = new CoinResolvedEvent(
                "coin.zero",
                CoinOrigin.Manual,
                0,
                0d,
                0d,
                0,
                CoinResolveReason.Bucket);

            Assert.IsTrue(_core.HandleCoinResolved(in evt));
            Assert.AreEqual(1, _creditedEvents.Count);
            Assert.AreEqual(0d, _creditedEvents[0].CreditedAmount);
            Assert.AreEqual(0, _walletEvents.Count);
            Assert.AreEqual(0d, _core.GetBalance(EconomyIds.Coin));
        }

        [Test]
        public void SafeCapReached_CreditedAmountMatchesClampedDelta()
        {
            _config.SetWalletSafeCapForTest(100d);
            _core = new EconomyCore(_sink);
            _core.Initialize(_config, ReleaseTier.Mvp, null);
            _core.IncomeCredited += _creditedEvents.Add;
            _core.WalletChanged += _walletEvents.Add;

            _core.Add(EconomyIds.Coin, 90d, WalletChangeReason.Offline);
            Assert.AreEqual(90d, _core.GetBalance(EconomyIds.Coin));

            CoinResolvedEvent evt = new CoinResolvedEvent(
                "coin.cap",
                CoinOrigin.Manual,
                0,
                1d,
                50d,
                0,
                CoinResolveReason.Bucket);

            Assert.IsTrue(_core.HandleCoinResolved(in evt));
            Assert.AreEqual(100d, _core.GetBalance(EconomyIds.Coin));
            Assert.AreEqual(10d, _creditedEvents[0].CreditedAmount);
        }
    }
}
