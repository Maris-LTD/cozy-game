using System.Collections.Generic;
using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Economy
{
    [TestFixture]
    public sealed class InvalidIncomeInputTests
    {
        private EconomyConfig _config;
        private CurrencyDefinition _coinDef;
        private CountingEconomyDiagnosticsSink _sink;
        private EconomyCore _core;
        private List<IncomeCreditedEvent> _creditedEvents;

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

            _sink = new CountingEconomyDiagnosticsSink();
            _core = new EconomyCore(_sink);
            _core.Initialize(_config, ReleaseTier.Mvp, null);

            _creditedEvents = new List<IncomeCreditedEvent>();
            _core.IncomeCredited += _creditedEvents.Add;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_coinDef);
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void SubmitIncome_InvalidBaseAmounts_RejectedAndReportedToSink()
        {
            double[] invalidAmounts =
            {
                0d,
                -0.0001d,
                -10d,
                double.NaN,
                double.PositiveInfinity,
                double.NegativeInfinity
            };

            for (int i = 0; i < invalidAmounts.Length; i++)
            {
                double amount = invalidAmounts[i];
                bool result = _core.SubmitIncome(IncomeChannel.Plinko, "shelf.slot1", amount);
                Assert.IsFalse(result);
                Assert.AreEqual(0d, _core.GetBalance(EconomyIds.Coin));
                Assert.AreEqual(0, _creditedEvents.Count);
                Assert.AreEqual(i + 1, _sink.CountWarnings(EconomyDiagnosticCode.InvalidIncomeInput));
            }
        }

        [Test]
        public void CoinResolved_InvalidRawValues_RejectedAndReportedToSink()
        {
            double[] invalidRawValues =
            {
                -0.0001d,
                -5d,
                double.NaN,
                double.PositiveInfinity,
                double.NegativeInfinity
            };

            for (int i = 0; i < invalidRawValues.Length; i++)
            {
                double rawValue = invalidRawValues[i];
                CoinResolvedEvent evt = new CoinResolvedEvent(
                    "coin.invalid",
                    CoinOrigin.Manual,
                    0,
                    1d,
                    rawValue,
                    2,
                    CoinResolveReason.Bucket);

                bool result = _core.HandleCoinResolved(in evt);
                Assert.IsFalse(result);
                Assert.AreEqual(0d, _core.GetBalance(EconomyIds.Coin));
                Assert.AreEqual(0, _creditedEvents.Count);
                Assert.AreEqual(i + 1, _sink.CountWarnings(EconomyDiagnosticCode.InvalidIncomeInput));
            }
        }

        [Test]
        public void ZeroRawValue_CoinResolved_IsValidAndAccepted()
        {
            CoinResolvedEvent evt = new CoinResolvedEvent(
                "coin.zero",
                CoinOrigin.Manual,
                0,
                1d,
                0d,
                0,
                CoinResolveReason.Bucket);

            bool result = _core.HandleCoinResolved(in evt);
            Assert.IsTrue(result);
            Assert.AreEqual(0, _sink.CountWarnings(EconomyDiagnosticCode.InvalidIncomeInput));
            Assert.AreEqual(1, _creditedEvents.Count);
            Assert.AreEqual(0d, _creditedEvents[0].CreditedAmount);
        }

        [Test]
        public void ValidSubmitIncome_DoesNotReportInvalidIncomeInput()
        {
            bool result = _core.SubmitIncome(IncomeChannel.Plinko, "shelf.slot1", 10d);
            Assert.IsTrue(result);
            Assert.AreEqual(0, _sink.CountWarnings(EconomyDiagnosticCode.InvalidIncomeInput));
            Assert.AreEqual(1, _creditedEvents.Count);
        }
    }
}
