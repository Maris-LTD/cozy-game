using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Economy
{
    [TestFixture]
    public sealed class BaseCoinValueSourceTests
    {
        private EconomyConfig _config;
        private CurrencyDefinition _coin;
        private CountingEconomyDiagnosticsSink _sink;
        private PlayerActionIssuer _issuer;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<EconomyConfig>();
            _config.SetStartingCoinsForTest(50000d);
            _config.SetSpinBasePriceForTest(100d);
            _config.SetSpinGrowthForTest(1.05d);
            _config.SetPriceNiceSigDigitsForTest(2);
            _config.SetCoinValueExponentForTest(0.6d);

            _coin = ScriptableObject.CreateInstance<CurrencyDefinition>();
            _coin.SetIdForTest(EconomyIds.Coin);
            _coin.SetReleaseTierForTest(ReleaseTier.Mvp);
            _coin.SetResetsOnNewShopForTest(true);
            _config.SetCurrenciesForTest(new[] { _coin });

            _sink = new CountingEconomyDiagnosticsSink();
            _issuer = new PlayerActionIssuer();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_coin);
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void AC33_BaseCoinValueFormula_MatchesTargetValues()
        {
            SpinPriceCurve curve = new SpinPriceCurve(_config);
            BaseCoinValueCalculator calculator = new BaseCoinValueCalculator(_config, curve);

            Assert.AreEqual(1d, calculator.Compute(0), 1e-4);
            Assert.AreEqual(1.0297d, calculator.Compute(1), 1e-3);
            Assert.AreEqual(18.679d, calculator.Compute(100), 18.679d * 1e-3);

            _config.SetCoinValueExponentForTest(0.5d);
            Assert.AreEqual(11.467d, calculator.Compute(100), 11.467d * 1e-3);

            _config.SetCoinValueExponentForTest(0.7d);
            Assert.AreEqual(30.43d, calculator.Compute(100), 30.43d * 1e-3);
        }

        [Test]
        public void AC33_FakePlinkoReader_LifecycleScenarios_MatchesExpectedValues()
        {
            EconomyCore core = new EconomyCore(_sink);
            FakePlinkoReader plinko = new FakePlinkoReader(core);

            core.Initialize(_config, ReleaseTier.Mvp, null);
            Assert.AreEqual(1d, plinko.BaseCoinValue, 1e-4);

            PlayerActionContext spin = _issuer.Issue(PlayerActionKind.GachaSpin);
            Assert.IsTrue(core.TryPaySpin(in spin));
            Assert.AreEqual(1.0297d, plinko.BaseCoinValue, 1e-3);

            PlayerActionContext invalidContext = default;
            Assert.IsFalse(core.TryPaySpin(in invalidContext));
            Assert.AreEqual(1.0297d, plinko.BaseCoinValue, 1e-3);

            EconomyCore loadedCore = new EconomyCore(_sink);
            FakePlinkoReader loadedPlinko = new FakePlinkoReader(loadedCore);
            EconomyStateData loadedData = new EconomyStateData(
                new[]
                {
                    new CurrencyBalanceEntry(EconomyIds.Coin, 1000d)
                },
                100);
            loadedCore.Initialize(_config, ReleaseTier.Mvp, loadedData);
            Assert.AreEqual(18.679d, loadedPlinko.BaseCoinValue, 18.679d * 1e-3);
        }
    }
}
