using System.Collections.Generic;
using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Economy
{
    [TestFixture]
    public sealed class PlinkoCreditTests
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
        public void AC15_AllOriginAndReasonCombinations_CreditAndRaiseCorrectEvents()
        {
            CoinOrigin[] origins = { CoinOrigin.Manual, CoinOrigin.Auto, CoinOrigin.Rain, CoinOrigin.JackpotBurst };
            CoinResolveReason[] reasons = { CoinResolveReason.Bucket, CoinResolveReason.Stuck, CoinResolveReason.Overflow };

            int eventIndex = 0;
            for (int o = 0; o < origins.Length; o++)
            {
                for (int r = 0; r < reasons.Length; r++)
                {
                    CoinOrigin origin = origins[o];
                    CoinResolveReason reason = reasons[r];
                    int bucketIndex = reason == CoinResolveReason.Bucket ? o : CoinResolvedEvent.NoBucket;
                    string coinId = "coin." + o + "." + r;
                    double rawValue = 10d + o + r;
                    double prevBalance = _core.GetBalance(EconomyIds.Coin);

                    CoinResolvedEvent evt = new CoinResolvedEvent(
                        coinId,
                        origin,
                        bucketIndex,
                        1d,
                        rawValue,
                        5,
                        reason);

                    Assert.IsTrue(_core.HandleCoinResolved(in evt));
                    Assert.AreEqual(eventIndex + 1, _creditedEvents.Count);

                    IncomeCreditedEvent credited = _creditedEvents[eventIndex];
                    Assert.AreEqual(IncomeChannel.Plinko, credited.Channel);
                    Assert.AreEqual(coinId, credited.CoinId);
                    Assert.AreEqual(bucketIndex, credited.BucketIndex);
                    Assert.AreEqual(origin, credited.Origin);
                    Assert.AreEqual(reason, credited.Reason);
                    Assert.AreEqual(EconomyIds.PlinkoCoinSource, credited.SourceKey);
                    Assert.AreEqual(rawValue, credited.BaseAmount, 1e-6);

                    double expectedDelta = rawValue;
                    Assert.AreEqual(expectedDelta, credited.CreditedAmount, 1e-6);
                    Assert.AreEqual(prevBalance + expectedDelta, _core.GetBalance(EconomyIds.Coin), 1e-6);

                    eventIndex++;
                }
            }
        }

        [Test]
        public void AC17_ZeroFrameDelay_BalanceAndEventsUpdatedSynchronously()
        {
            Assert.AreEqual(0d, _core.GetBalance(EconomyIds.Coin));
            CoinResolvedEvent evt = new CoinResolvedEvent(
                "coin.sync",
                CoinOrigin.Manual,
                2,
                1d,
                25d,
                3,
                CoinResolveReason.Bucket);

            Assert.IsTrue(_core.HandleCoinResolved(in evt));

            Assert.AreEqual(25d, _core.GetBalance(EconomyIds.Coin));
            Assert.AreEqual(1, _creditedEvents.Count);
            Assert.AreEqual(1, _walletEvents.Count);
            Assert.AreEqual(25d, _creditedEvents[0].CreditedAmount);
            Assert.AreEqual(25d, _walletEvents[0].Delta);
        }

        [Test]
        public void AC17_ConsecutiveCoins_TotalBalanceMatchesSumOfCreditedAmounts()
        {
            const int coinCount = 24;
            double totalExpected = 0d;

            for (int i = 0; i < coinCount; i++)
            {
                double raw = 2.5d * (i + 1);
                totalExpected += raw;
                CoinResolvedEvent evt = new CoinResolvedEvent(
                    "coin.burst." + i,
                    CoinOrigin.Auto,
                    i % 5,
                    1d,
                    raw,
                    1,
                    CoinResolveReason.Bucket);

                Assert.IsTrue(_core.HandleCoinResolved(in evt));
            }

            Assert.AreEqual(coinCount, _creditedEvents.Count);
            Assert.AreEqual(coinCount, _walletEvents.Count);
            Assert.AreEqual(totalExpected, _core.GetBalance(EconomyIds.Coin), 1e-6);
        }
    }
}
