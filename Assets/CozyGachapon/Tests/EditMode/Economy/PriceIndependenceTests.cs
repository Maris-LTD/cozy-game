using System.Collections.Generic;
using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Economy
{
    [TestFixture]
    public sealed class PriceIndependenceTests
    {
        private EconomyConfig _config;
        private CurrencyDefinition _coin;
        private CountingEconomyDiagnosticsSink _sink;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<EconomyConfig>();
            _config.SetStartingCoinsForTest(1000d);
            _config.SetSpinBasePriceForTest(100d);
            _config.SetSpinGrowthForTest(1.05d);
            _config.SetPriceNiceSigDigitsForTest(2);
            _config.SetIdleCpsUnitForTest(2d);

            _coin = ScriptableObject.CreateInstance<CurrencyDefinition>();
            _coin.SetIdForTest(EconomyIds.Coin);
            _coin.SetReleaseTierForTest(ReleaseTier.Mvp);
            _coin.SetResetsOnNewShopForTest(true);
            _config.SetCurrenciesForTest(new[] { _coin });

            _sink = new CountingEconomyDiagnosticsSink();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_coin);
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void AC32_PricesInvariant_WhenIdleCpsChanges()
        {
            EconomyStateData loadedData = new EconomyStateData(
                new[] { new CurrencyBalanceEntry(EconomyIds.Coin, 1000d) },
                30);

            EconomyCore core = new EconomyCore(_sink);
            core.Initialize(_config, ReleaseTier.Mvp, loadedData);

            SpinPriceCurve curve = new SpinPriceCurve(_config);
            DerivedPriceCalculator derived = new DerivedPriceCalculator(curve);

            double baselineSpinPrice = core.SpinPrice;
            Assert.IsTrue(derived.TryGet(10d, 30, out double baselineDerived10));
            Assert.IsTrue(derived.TryGet(0.5d, 30, out double baselineDerived05));

            Assert.AreEqual(0d, core.IncomePipeline.IdleCps, 1e-4);

            core.IncomePipeline.RegisterIncomeSource("source.A", 100d);
            Assert.AreEqual(100d, core.IncomePipeline.IdleCps, 1e-4);
            Assert.AreEqual(baselineSpinPrice, core.SpinPrice, 1e-4);
            derived.TryGet(10d, 30, out double p10A);
            derived.TryGet(0.5d, 30, out double p05A);
            Assert.AreEqual(baselineDerived10, p10A, 1e-4);
            Assert.AreEqual(baselineDerived05, p05A, 1e-4);

            core.IncomePipeline.RegisterIncomeSource("source.B", 900d);
            Assert.AreEqual(1000d, core.IncomePipeline.IdleCps, 1e-4);
            Assert.AreEqual(baselineSpinPrice, core.SpinPrice, 1e-4);
            derived.TryGet(10d, 30, out double p10B);
            derived.TryGet(0.5d, 30, out double p05B);
            Assert.AreEqual(baselineDerived10, p10B, 1e-4);
            Assert.AreEqual(baselineDerived05, p05B, 1e-4);

            core.IncomePipeline.UnregisterIncomeSource("source.A");
            core.IncomePipeline.UnregisterIncomeSource("source.B");
            Assert.AreEqual(0d, core.IncomePipeline.IdleCps, 1e-4);
            Assert.AreEqual(baselineSpinPrice, core.SpinPrice, 1e-4);
            derived.TryGet(10d, 30, out double p10C);
            derived.TryGet(0.5d, 30, out double p05C);
            Assert.AreEqual(baselineDerived10, p10C, 1e-4);
            Assert.AreEqual(baselineDerived05, p05C, 1e-4);
        }
    }
}
