using System.Collections.Generic;
using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Economy
{
    [TestFixture]
    public sealed class SpinPaymentTests
    {
        private EconomyConfig _config;
        private CurrencyDefinition _coin;
        private CountingEconomyDiagnosticsSink _sink;
        private EconomyCore _core;
        private PlayerActionIssuer _issuer;
        private List<SpinPriceChangedEvent> _priceEvents;
        private List<SpendRejectedEvent> _rejections;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<EconomyConfig>();
            _config.SetStartingCoinsForTest(150d);
            _config.SetSpinBasePriceForTest(100d);
            _config.SetSpinGrowthForTest(1.05d);
            _config.SetPriceNiceSigDigitsForTest(2);

            _coin = ScriptableObject.CreateInstance<CurrencyDefinition>();
            _coin.SetIdForTest(EconomyIds.Coin);
            _coin.SetReleaseTierForTest(ReleaseTier.Mvp);
            _coin.SetResetsOnNewShopForTest(true);
            _config.SetCurrenciesForTest(new[] { _coin });

            _sink = new CountingEconomyDiagnosticsSink();
            _issuer = new PlayerActionIssuer();
            _priceEvents = new List<SpinPriceChangedEvent>();
            _rejections = new List<SpendRejectedEvent>();

            _core = new EconomyCore(_sink);
            _core.SpinPriceChanged += _priceEvents.Add;
            _core.SpendRejected += _rejections.Add;
            _core.Initialize(_config, ReleaseTier.Mvp, null);
        }

        [TearDown]
        public void TearDown()
        {
            _core.SpinPriceChanged -= _priceEvents.Add;
            _core.SpendRejected -= _rejections.Add;
            Object.DestroyImmediate(_coin);
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void AC30_TryPaySpin_TwiceSameFrame_FirstSucceedsSecondFailsWithRejection()
        {
            Assert.AreEqual(150d, _core.GetBalance(EconomyIds.Coin), 1e-4);
            Assert.AreEqual(0, _core.SpinsThisShop);
            Assert.AreEqual(100d, _core.SpinPrice, 1e-4);

            PlayerActionContext context1 = _issuer.Issue(PlayerActionKind.GachaSpin);
            bool firstResult = _core.TryPaySpin(in context1);

            Assert.IsTrue(firstResult);
            Assert.AreEqual(50d, _core.GetBalance(EconomyIds.Coin), 1e-4);
            Assert.AreEqual(1, _core.SpinsThisShop);
            Assert.AreEqual(110d, _core.SpinPrice, 1e-4);
            Assert.AreEqual(1, _priceEvents.Count);
            Assert.AreEqual(1, _priceEvents[0].SpinsThisShop);
            Assert.AreEqual(110d, _priceEvents[0].SpinPrice, 1e-4);
            Assert.AreEqual(1.0297d, _priceEvents[0].BaseCoinValue, 1e-3);

            PlayerActionContext context2 = _issuer.Issue(PlayerActionKind.GachaSpin);
            bool secondResult = _core.TryPaySpin(in context2);

            Assert.IsFalse(secondResult);
            Assert.AreEqual(50d, _core.GetBalance(EconomyIds.Coin), 1e-4);
            Assert.AreEqual(1, _core.SpinsThisShop);
            Assert.AreEqual(110d, _core.SpinPrice, 1e-4);
            Assert.AreEqual(1, _rejections.Count);
            Assert.AreEqual(EconomyIds.Coin, _rejections[0].CurrencyId);
            Assert.AreEqual(110d, _rejections[0].Amount, 1e-4);
            Assert.AreEqual(SpendReason.GachaSpin, _rejections[0].Reason);
        }

        [Test]
        public void AC35_SpinsThisShop_UnchangedExceptBySuccessfulPaySpinAndReset()
        {
            PlayerActionContext spin = _issuer.Issue(PlayerActionKind.GachaSpin);
            _core.Add(EconomyIds.Coin, 10000d, WalletChangeReason.AdReward);
            for (int i = 0; i < 5; i++)
            {
                Assert.IsTrue(_core.TryPaySpin(in spin));
            }
            Assert.AreEqual(5, _core.SpinsThisShop);

            _core.Add(EconomyIds.Coin, 500d, WalletChangeReason.AdReward);
            _core.GetBalance(EconomyIds.Coin);
            _core.CanAfford(EconomyIds.Coin, 100d);
            _core.CanAffordSpin();
            _core.GetSpinPrice();
            _core.IsCurrencyRegistered(EconomyIds.Coin);
            _core.ExportState();

            PlayerActionContext shopContext = _issuer.Issue(PlayerActionKind.ShopPurchase);
            _core.TrySpend(EconomyIds.Coin, 50d, SpendReason.ShopPurchase, in shopContext);

            PlayerActionContext invalidContext = default;
            _core.TryPaySpin(in invalidContext);

            Assert.AreEqual(5, _core.SpinsThisShop);
        }

        [Test]
        public void TryPaySpin_InvalidContext_ReturnsFalseWithoutSpending()
        {
            PlayerActionContext invalid = default;
            bool result = _core.TryPaySpin(in invalid);

            Assert.IsFalse(result);
            Assert.AreEqual(150d, _core.GetBalance(EconomyIds.Coin), 1e-4);
            Assert.AreEqual(0, _core.SpinsThisShop);
        }

        [Test]
        public void CanAffordSpin_ReflectsCoinBalanceVsCurrentPrice()
        {
            Assert.IsTrue(_core.CanAffordSpin());
            PlayerActionContext spin = _issuer.Issue(PlayerActionKind.GachaSpin);
            _core.TryPaySpin(in spin);
            Assert.AreEqual(50d, _core.GetBalance(EconomyIds.Coin), 1e-4);
            Assert.AreEqual(110d, _core.SpinPrice, 1e-4);
            Assert.IsFalse(_core.CanAffordSpin());
        }
    }
}
