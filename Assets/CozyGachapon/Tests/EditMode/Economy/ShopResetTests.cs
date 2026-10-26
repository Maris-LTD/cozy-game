using System.Collections.Generic;
using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Economy
{
    [TestFixture]
    public sealed class ShopResetTests
    {
        private const string StampId = "currency.stamp";
        private EconomyConfig _config;
        private CurrencyDefinition _coin;
        private CurrencyDefinition _stamp;
        private CountingEconomyDiagnosticsSink _sink;
        private EconomyCore _core;
        private PlayerActionIssuer _issuer;
        private List<string> _eventOrder;
        private List<WalletChangedEvent> _walletEvents;
        private List<SpinPriceChangedEvent> _priceEvents;
        private List<ShopEconomyResetEvent> _resetEvents;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<EconomyConfig>();
            _config.SetStartingCoinsForTest(100d);
            _config.SetSpinBasePriceForTest(100d);
            _config.SetSpinGrowthForTest(1.05d);
            _config.SetPriceNiceSigDigitsForTest(2);
            _config.SetPrestigeTierCapForTest(10d);

            _coin = ScriptableObject.CreateInstance<CurrencyDefinition>();
            _coin.SetIdForTest(EconomyIds.Coin);
            _coin.SetReleaseTierForTest(ReleaseTier.Mvp);
            _coin.SetResetsOnNewShopForTest(true);

            _stamp = ScriptableObject.CreateInstance<CurrencyDefinition>();
            _stamp.SetIdForTest(StampId);
            _stamp.SetReleaseTierForTest(ReleaseTier.Mvp);
            _stamp.SetResetsOnNewShopForTest(false);

            _config.SetCurrenciesForTest(new[] { _coin, _stamp });

            _sink = new CountingEconomyDiagnosticsSink();
            _issuer = new PlayerActionIssuer();
            _eventOrder = new List<string>();
            _walletEvents = new List<WalletChangedEvent>();
            _priceEvents = new List<SpinPriceChangedEvent>();
            _resetEvents = new List<ShopEconomyResetEvent>();

            _core = new EconomyCore(_sink);
            _core.WalletChanged += OnWalletChanged;
            _core.SpinPriceChanged += OnPriceChanged;
            _core.ShopEconomyReset += OnShopReset;
        }

        [TearDown]
        public void TearDown()
        {
            _core.WalletChanged -= OnWalletChanged;
            _core.SpinPriceChanged -= OnPriceChanged;
            _core.ShopEconomyReset -= OnShopReset;
            Object.DestroyImmediate(_coin);
            Object.DestroyImmediate(_stamp);
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void AC36_ResetForNewShop_OrderAndStateResetCorrectly()
        {
            EconomyStateData loadedData = new EconomyStateData(
                new[]
                {
                    new CurrencyBalanceEntry(EconomyIds.Coin, 5000d),
                    new CurrencyBalanceEntry(StampId, 7d)
                },
                40);

            _core.Initialize(_config, ReleaseTier.Mvp, loadedData);
            FakePlinkoReader plinko = new FakePlinkoReader(_core);

            _core.IncomePipeline.RegisterIncomeSource("source.check", IncomeChannel.Idle, 1d);

            IncomeModifier prestigeMod = new IncomeModifier(
                "prestige.x",
                IncomeChannel.Both,
                ModifierTier.Prestige,
                1.0d,
                ModifierTarget.Channel,
                string.Empty,
                false);
            _core.IncomePipeline.RegisterModifier(in prestigeMod);

            Assert.AreEqual(5000d, _core.GetBalance(EconomyIds.Coin), 1e-4);
            Assert.AreEqual(7d, _core.GetBalance(StampId), 1e-4);
            Assert.AreEqual(40, _core.SpinsThisShop);
            Assert.Greater(plinko.BaseCoinValue, 1d);

            _eventOrder.Clear();
            _walletEvents.Clear();
            _priceEvents.Clear();
            _resetEvents.Clear();

            PlayerActionContext resetContext = _issuer.Issue(PlayerActionKind.StartNewShop);
            bool result = _core.ResetForNewShop(in resetContext);

            Assert.IsTrue(result);
            Assert.AreEqual(100d, _core.GetBalance(EconomyIds.Coin), 1e-4);
            Assert.AreEqual(7d, _core.GetBalance(StampId), 1e-4);
            Assert.AreEqual(0, _core.SpinsThisShop);
            Assert.AreEqual(100d, _core.SpinPrice, 1e-4);
            Assert.AreEqual(1d, plinko.BaseCoinValue, 1e-4);

            Assert.AreEqual(3, _eventOrder.Count);
            Assert.AreEqual("WalletChanged", _eventOrder[0]);
            Assert.AreEqual("SpinPriceChanged", _eventOrder[1]);
            Assert.AreEqual("ShopEconomyReset", _eventOrder[2]);

            Assert.AreEqual(1, _walletEvents.Count);
            Assert.AreEqual(EconomyIds.Coin, _walletEvents[0].CurrencyId);
            Assert.AreEqual(5000d, _walletEvents[0].Previous, 1e-4);
            Assert.AreEqual(100d, _walletEvents[0].Current, 1e-4);
            Assert.AreEqual(-4900d, _walletEvents[0].Delta, 1e-4);
            Assert.AreEqual(WalletChangeReason.ShopReset, _walletEvents[0].Reason);

            Assert.AreEqual(1, _priceEvents.Count);
            Assert.AreEqual(0, _priceEvents[0].SpinsThisShop);
            Assert.AreEqual(100d, _priceEvents[0].SpinPrice, 1e-4);
            Assert.AreEqual(1d, _priceEvents[0].BaseCoinValue, 1e-4);

            Assert.AreEqual(1, _resetEvents.Count);
            Assert.AreEqual(0, _resetEvents[0].SpinsThisShop);
            Assert.AreEqual(1, _resetEvents[0].ResetCurrencyIds.Count);
            Assert.AreEqual(EconomyIds.Coin, _resetEvents[0].ResetCurrencyIds[0]);

            double income = _core.IncomePipeline.GetChannelIncome("source.check", 0d);
            Assert.AreEqual(2d, income, 1e-4);
        }

        [Test]
        public void AC6_ResetForNewShop_InvalidContext_DoesNothing()
        {
            _core.Initialize(_config, ReleaseTier.Mvp, null);
            _core.Add(EconomyIds.Coin, 400d, WalletChangeReason.AdReward);
            Assert.AreEqual(500d, _core.GetBalance(EconomyIds.Coin), 1e-4);

            _eventOrder.Clear();
            _walletEvents.Clear();
            _priceEvents.Clear();
            _resetEvents.Clear();

            PlayerActionContext invalid = default;
            bool result = _core.ResetForNewShop(in invalid);

            Assert.IsFalse(result);
            Assert.AreEqual(500d, _core.GetBalance(EconomyIds.Coin), 1e-4);
            Assert.AreEqual(0, _walletEvents.Count);
            Assert.AreEqual(0, _priceEvents.Count);
            Assert.AreEqual(0, _resetEvents.Count);
        }

        private void OnWalletChanged(WalletChangedEvent change)
        {
            _eventOrder.Add("WalletChanged");
            _walletEvents.Add(change);
        }

        private void OnPriceChanged(SpinPriceChangedEvent change)
        {
            _eventOrder.Add("SpinPriceChanged");
            _priceEvents.Add(change);
        }

        private void OnShopReset(ShopEconomyResetEvent reset)
        {
            _eventOrder.Add("ShopEconomyReset");
            _resetEvents.Add(reset);
        }
    }
}
