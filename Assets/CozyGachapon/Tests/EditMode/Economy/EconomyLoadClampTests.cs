using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using NUnit.Framework;
using Object = UnityEngine.Object;

namespace CozyGachapon.Tests.Economy
{
    public class EconomyLoadClampTests
    {
        private CountingEconomyDiagnosticsSink _sink;
        private EconomyConfig _config;
        private EconomyCore _core;

        [SetUp]
        public void SetUp()
        {
            _sink = new CountingEconomyDiagnosticsSink();
            _config = EconomyStateTestData.CreateConfigWithCurrencies(0d);
            _core = new EconomyCore(_sink);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void NegativeCoin_ClampsToZeroAndReports()
        {
            LoadCoin(-5d);

            AssertClamped(0d);
        }

        [Test]
        public void NaNCoin_ClampsToZeroAndReports()
        {
            LoadCoin(double.NaN);

            AssertClamped(0d);
        }

        [Test]
        public void NegativeInfinityCoin_ClampsToZeroAndReports()
        {
            LoadCoin(double.NegativeInfinity);

            AssertClamped(0d);
        }

        [Test]
        public void HugeCoin_ClampsToWalletSafeCapAndReports()
        {
            LoadCoin(1e306d);

            AssertClamped(_config.WalletSafeCap);
        }

        [Test]
        public void PositiveInfinityCoin_ClampsToWalletSafeCapAndReports()
        {
            LoadCoin(double.PositiveInfinity);

            AssertClamped(_config.WalletSafeCap);
        }

        [Test]
        public void UnregisteredCurrencyWithBadValue_IsClampedBeforeBeingPreserved()
        {
            EconomyStateData loaded = EconomyStateTestData.Build(
                0,
                new CurrencyBalanceEntry(EconomyIds.Coin, 1d),
                new CurrencyBalanceEntry(EconomyStateTestData.FutureId, double.NaN));

            Assert.IsTrue(_core.Initialize(_config, ReleaseTier.Mvp, loaded));

            Assert.AreEqual(1, _sink.CountWarnings(EconomyDiagnosticCode.LoadedBalanceClamped));
            Assert.AreEqual(EconomyStateTestData.FutureId, _sink.LastWarningCurrencyId);
            EconomyStateTestData.AssertExactlyEqual(0d, EconomyStateTestData.Find(_core.ExportState(), EconomyStateTestData.FutureId));
        }

        [Test]
        public void NegativeSpins_ClampToZeroAndReport()
        {
            EconomyStateData loaded = EconomyStateTestData.Build(-3, new CurrencyBalanceEntry(EconomyIds.Coin, 10d));

            Assert.IsTrue(_core.Initialize(_config, ReleaseTier.Mvp, loaded));

            Assert.AreEqual(0, _core.SpinsThisShop);
            Assert.AreEqual(1, _sink.CountWarnings(EconomyDiagnosticCode.LoadedSpinsClamped));
            Assert.AreEqual(0, _sink.CountWarnings(EconomyDiagnosticCode.LoadedBalanceClamped));
            EconomyStateTestData.AssertExactlyEqual(_config.SpinBasePrice, _core.SpinPrice);
            EconomyStateTestData.AssertExactlyEqual(1d, _core.BaseCoinValue);
            Assert.AreEqual(0, _core.ExportState().SpinsThisShop);
        }

        [TestCase(0.1d)]
        [TestCase(0d)]
        [TestCase(1e300d)]
        public void ValidCoin_IsKeptExactlyWithoutWarning(double coin)
        {
            LoadCoin(coin);

            EconomyStateTestData.AssertExactlyEqual(coin, _core.GetBalance(EconomyIds.Coin));
            Assert.AreEqual(0, _sink.WarningCount);
        }

        [Test]
        public void EntriesWithNullOrEmptyId_AreDroppedWithoutException()
        {
            EconomyStateData loaded = EconomyStateTestData.Build(
                0,
                new CurrencyBalanceEntry(null, 5d),
                new CurrencyBalanceEntry(string.Empty, 6d),
                new CurrencyBalanceEntry(EconomyIds.Coin, 2d));

            Assert.DoesNotThrow(() => _core.Initialize(_config, ReleaseTier.Mvp, loaded));

            Assert.AreEqual(2d, _core.GetBalance(EconomyIds.Coin));
            Assert.AreEqual(1, _core.ExportState().Balances.Count);
        }

        [Test]
        public void NullBalancesArray_IsTreatedAsNewGameWithoutException()
        {
            EconomyStateData loaded = new EconomyStateData(null, 9);

            Assert.DoesNotThrow(() => _core.Initialize(_config, ReleaseTier.Mvp, loaded));

            Assert.AreEqual(0, _core.SpinsThisShop);
            Assert.AreEqual(0d, _core.GetBalance(EconomyIds.Coin));
        }

        [Test]
        public void DuplicateIds_KeepLastValue()
        {
            EconomyStateData loaded = EconomyStateTestData.Build(
                0,
                new CurrencyBalanceEntry(EconomyIds.Coin, 1d),
                new CurrencyBalanceEntry(EconomyIds.Coin, 4d),
                new CurrencyBalanceEntry(EconomyStateTestData.FutureId, 1d),
                new CurrencyBalanceEntry(EconomyStateTestData.FutureId, 8d));

            Assert.IsTrue(_core.Initialize(_config, ReleaseTier.Mvp, loaded));

            EconomyStateData exported = _core.ExportState();
            Assert.AreEqual(4d, _core.GetBalance(EconomyIds.Coin));
            Assert.AreEqual(8d, EconomyStateTestData.Find(exported, EconomyStateTestData.FutureId));
            Assert.AreEqual(2, exported.Balances.Count);
        }

        private void LoadCoin(double amount)
        {
            EconomyStateData loaded = EconomyStateTestData.Build(0, new CurrencyBalanceEntry(EconomyIds.Coin, amount));
            Assert.IsTrue(_core.Initialize(_config, ReleaseTier.Mvp, loaded));
        }

        private void AssertClamped(double expected)
        {
            EconomyStateTestData.AssertExactlyEqual(expected, _core.GetBalance(EconomyIds.Coin));
            Assert.AreEqual(1, _sink.CountWarnings(EconomyDiagnosticCode.LoadedBalanceClamped));
            Assert.AreEqual(EconomyIds.Coin, _sink.LastWarningCurrencyId);
        }
    }
}
