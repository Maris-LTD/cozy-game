using System.Collections.Generic;
using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Economy
{
    [TestFixture]
    public sealed class SubmitIncomeTests
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
            _config.SetSynergyTierCapForTest(4d);
            _config.SetBoostTierCapForTest(3d);
            _config.SetPrestigeTierCapForTest(10d);
            _config.SetGlobalTierCapForTest(2d);

            _sink = new CountingEconomyDiagnosticsSink();
            _core = new EconomyCore(_sink);
            _core.Initialize(_config, ReleaseTier.Mvp, null);

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
        public void AC19_SubmitIncome_WithSynergyAndTeaBoost_CreditsExactExpectedAmountAndRaisesEvent()
        {
            const string sourceKey = "shelf.slot1";
            const double baseAmount = 10d;

            IncomeModifier synergy = IncomeModifier.ForSource(
                "shelf.slot1.synergy",
                IncomeChannel.Plinko,
                ModifierTier.Synergy,
                0.5d,
                sourceKey);
            IncomeModifier teaBoost = IncomeModifier.ForChannel(
                "boost.tea",
                IncomeChannel.Plinko,
                ModifierTier.Boost,
                1.0d);

            _core.IncomePipeline.RegisterModifier(in synergy);
            _core.IncomePipeline.RegisterModifier(in teaBoost);

            bool success = _core.SubmitIncome(IncomeChannel.Plinko, sourceKey, baseAmount);
            Assert.IsTrue(success);

            Assert.AreEqual(30d, _core.GetBalance(EconomyIds.Coin), 1e-6d);
            Assert.AreEqual(1, _creditedEvents.Count);

            IncomeCreditedEvent evt = _creditedEvents[0];
            Assert.AreEqual(IncomeChannel.Plinko, evt.Channel);
            Assert.AreEqual(sourceKey, evt.SourceKey);
            Assert.AreEqual(baseAmount, evt.BaseAmount, 1e-6d);
            Assert.AreEqual(30d, evt.CreditedAmount, 1e-6d);
            Assert.AreEqual(IncomeCreditedEvent.NoCoinId, evt.CoinId);
            Assert.AreEqual("none", evt.CoinId);
            Assert.AreEqual(IncomeCreditedEvent.NoBucket, evt.BucketIndex);
            Assert.AreEqual(-1, evt.BucketIndex);
            Assert.AreEqual(CoinOrigin.None, evt.Origin);
            Assert.AreEqual(CoinResolveReason.None, evt.Reason);

            Assert.AreEqual(1, _walletEvents.Count);
            Assert.AreEqual(EconomyIds.Coin, _walletEvents[0].CurrencyId);
            Assert.AreEqual(30d, _walletEvents[0].Delta, 1e-6d);
            Assert.AreEqual(WalletChangeReason.PlinkoIncome, _walletEvents[0].Reason);
        }

        [Test]
        public void SubmitIncome_NonPlinkoChannel_ReturnsFalse_AndDoesNotChangeBalance()
        {
            const string sourceKey = "shelf.slot1";
            double initial = _core.GetBalance(EconomyIds.Coin);

            bool idleResult = _core.SubmitIncome(IncomeChannel.Idle, sourceKey, 10d);
            Assert.IsFalse(idleResult);
            Assert.AreEqual(initial, _core.GetBalance(EconomyIds.Coin));
            Assert.AreEqual(0, _creditedEvents.Count);
            Assert.AreEqual(1, _sink.CountWarnings(EconomyDiagnosticCode.InvalidIncomeInput));

            bool bothResult = _core.SubmitIncome(IncomeChannel.Both, sourceKey, 10d);
            Assert.IsFalse(bothResult);
            Assert.AreEqual(initial, _core.GetBalance(EconomyIds.Coin));
            Assert.AreEqual(0, _creditedEvents.Count);
            Assert.AreEqual(2, _sink.CountWarnings(EconomyDiagnosticCode.InvalidIncomeInput));
        }

        [Test]
        public void SubmitIncome_NullOrEmptySourceKey_ReturnsFalse_AndReportsSink()
        {
            double initial = _core.GetBalance(EconomyIds.Coin);

            bool nullResult = _core.SubmitIncome(IncomeChannel.Plinko, null, 10d);
            Assert.IsFalse(nullResult);
            Assert.AreEqual(initial, _core.GetBalance(EconomyIds.Coin));
            Assert.AreEqual(1, _sink.CountWarnings(EconomyDiagnosticCode.InvalidIncomeInput));

            bool emptyResult = _core.SubmitIncome(IncomeChannel.Plinko, string.Empty, 10d);
            Assert.IsFalse(emptyResult);
            Assert.AreEqual(initial, _core.GetBalance(EconomyIds.Coin));
            Assert.AreEqual(2, _sink.CountWarnings(EconomyDiagnosticCode.InvalidIncomeInput));
        }

        [Test]
        public void SubmitIncome_ViaPipelineDirect_ExecutesCorrectly()
        {
            const string sourceKey = "shelf.slot2";
            bool success = _core.IncomePipeline.SubmitIncome(IncomeChannel.Plinko, sourceKey, 5d);
            Assert.IsTrue(success);
            Assert.AreEqual(5d, _core.GetBalance(EconomyIds.Coin), 1e-6d);
            Assert.AreEqual(1, _creditedEvents.Count);
            Assert.AreEqual(sourceKey, _creditedEvents[0].SourceKey);
        }
    }
}
