using System.Collections.Generic;
using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Economy
{
    [TestFixture]
    public sealed class ResolveTimeModifierTests
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
            _config.SetSynergyTierCapForTest(4d);
            _config.SetBoostTierCapForTest(3d);
            _config.SetPrestigeTierCapForTest(10d);
            _config.SetGlobalTierCapForTest(2d);

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
        public void AC16_CoinResolved_ResolveTimeModifiers_TeaBoostAppliedWhenActive_DroppedWhenRemoved()
        {
            double rawValue = 2d;
            double baseCoinValue = _core.BaseCoinValue;
            double baseReturn = rawValue * baseCoinValue;

            CoinResolvedEvent coinA = new CoinResolvedEvent(
                "coin.A",
                CoinOrigin.Manual,
                0,
                1d,
                rawValue,
                5,
                CoinResolveReason.Bucket);

            IncomeModifier teaBoost = IncomeModifier.ForChannel(
                "boost.tea",
                IncomeChannel.Plinko,
                ModifierTier.Boost,
                1.0d);
            bool registered = _core.IncomePipeline.RegisterModifier(in teaBoost);
            Assert.IsTrue(registered);

            bool handledA = _core.HandleCoinResolved(in coinA);
            Assert.IsTrue(handledA);
            Assert.AreEqual(1, _creditedEvents.Count);
            Assert.AreEqual(2d * baseReturn, _creditedEvents[0].CreditedAmount, 1e-6d);
            Assert.AreEqual(2d * baseReturn, _core.GetBalance(EconomyIds.Coin), 1e-6d);

            int removed = _core.IncomePipeline.Unregister("boost.tea");
            Assert.AreEqual(1, removed);

            CoinResolvedEvent coinB = new CoinResolvedEvent(
                "coin.B",
                CoinOrigin.Manual,
                0,
                1d,
                rawValue,
                5,
                CoinResolveReason.Bucket);

            bool handledB = _core.HandleCoinResolved(in coinB);
            Assert.IsTrue(handledB);
            Assert.AreEqual(2, _creditedEvents.Count);
            Assert.AreEqual(baseReturn, _creditedEvents[1].CreditedAmount, 1e-6d);
            Assert.AreEqual(3d * baseReturn, _core.GetBalance(EconomyIds.Coin), 1e-6d);
        }

        [Test]
        public void ResolveTimeModifiers_MultipleTiers_CalculatedAtMomentOfResolution()
        {
            double rawValue = 1d;
            double baseCoinValue = _core.BaseCoinValue;

            IncomeModifier synergy = IncomeModifier.ForSource(
                "shelf.slot1.synergy",
                IncomeChannel.Plinko,
                ModifierTier.Synergy,
                0.5d,
                EconomyIds.PlinkoCoinSource);
            IncomeModifier prestige = IncomeModifier.ForChannel(
                "prestige.upgrade",
                IncomeChannel.Plinko,
                ModifierTier.Prestige,
                1.0d);

            _core.IncomePipeline.RegisterModifier(in synergy);
            _core.IncomePipeline.RegisterModifier(in prestige);

            CoinResolvedEvent evt = new CoinResolvedEvent(
                "coin.multi",
                CoinOrigin.Auto,
                1,
                1d,
                rawValue,
                3,
                CoinResolveReason.Bucket);

            bool handled = _core.HandleCoinResolved(in evt);
            Assert.IsTrue(handled);

            double expected = rawValue * baseCoinValue * 1.5d * 2.0d;
            Assert.AreEqual(expected, _creditedEvents[0].CreditedAmount, 1e-6d);
        }
    }
}
