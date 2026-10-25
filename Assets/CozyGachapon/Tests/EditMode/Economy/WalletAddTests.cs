using System;
using System.Globalization;
using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using NUnit.Framework;

namespace CozyGachapon.Tests.Economy
{
    public class WalletAddTests : EconomyWalletFixture
    {
        private const string UnknownCurrencyId = "currency.unknown";
        private const double StartingBalance = 10d;
        private const double SmallCap = 1000d;

        private static string FloorDisplay(double balance)
        {
            return Math.Floor(balance).ToString(CultureInfo.InvariantCulture);
        }

        [Test]
        public void Add_TwoAndAHalfTwice_BalanceFiveAndOneEventPerAdd()
        {
            Assert.IsTrue(Wallet.Add(EconomyIds.Coin, 2.5d, WalletChangeReason.AdReward));
            Assert.IsTrue(Wallet.Add(EconomyIds.Coin, 2.5d, WalletChangeReason.AdReward));

            AssertBalance(5d, EconomyIds.Coin);
            Assert.AreEqual("5", FloorDisplay(Wallet.GetBalance(EconomyIds.Coin)));
            Assert.AreEqual(2, Recorder.Count);
            for (int i = 0; i < Recorder.Count; i++)
            {
                WalletChangedEvent change = Recorder[i];
                Assert.AreEqual(EconomyIds.Coin, change.CurrencyId);
                Assert.AreEqual(2.5d, change.Delta, EconomyTestAssets.RelativeTolerance(2.5d, RelTolerance));
                Assert.AreEqual(WalletChangeReason.AdReward, change.Reason);
                AssertEventIntegrity(change);
            }

            Assert.AreEqual(0, Sink.WarningCount);
        }

        [Test]
        public void Add_PointFourThreeTimes_BalanceKeepsFractionAndDisplayFloorsToOne()
        {
            for (int i = 0; i < 3; i++)
            {
                Assert.IsTrue(Wallet.Add(EconomyIds.Coin, 0.4d, WalletChangeReason.Offline));
            }

            AssertBalance(1.2d, EconomyIds.Coin);
            Assert.AreEqual("1", FloorDisplay(Wallet.GetBalance(EconomyIds.Coin)));
            Assert.AreEqual(3, Recorder.Count);
            for (int i = 0; i < Recorder.Count; i++)
            {
                Assert.AreEqual(0.4d, Recorder[i].Delta, EconomyTestAssets.RelativeTolerance(0.4d, RelTolerance));
                Assert.AreEqual(WalletChangeReason.Offline, Recorder[i].Reason);
                AssertEventIntegrity(Recorder[i]);
            }
        }

        [TestCase(-1d)]
        [TestCase(0d)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void Add_InvalidAmount_RejectedWithoutEvent(double amount)
        {
            GiveStartingBalance();

            Assert.IsFalse(Wallet.Add(EconomyIds.Coin, amount, WalletChangeReason.AdReward));

            AssertRejectedAndUnchanged();
        }

        [TestCase(UnknownCurrencyId)]
        [TestCase((string)null)]
        [TestCase("")]
        public void Add_UnregisteredCurrency_RejectedWithoutEvent(string currencyId)
        {
            GiveStartingBalance();

            Assert.IsFalse(Wallet.Add(currencyId, 5d, WalletChangeReason.AdReward));

            AssertRejectedAndUnchanged();
        }

        [TestCase(WalletChangeReason.GachaSpin)]
        [TestCase(WalletChangeReason.ShopReset)]
        [TestCase(WalletChangeReason.ShopPurchase)]
        [TestCase(WalletChangeReason.PrestigeUpgrade)]
        [TestCase(WalletChangeReason.NeighborCandy)]
        [TestCase((WalletChangeReason)999)]
        public void Add_SinkOrUndefinedReason_RejectedWithoutEvent(WalletChangeReason reason)
        {
            GiveStartingBalance();

            Assert.IsFalse(Wallet.Add(EconomyIds.Coin, 5d, reason));

            AssertRejectedAndUnchanged();
        }

        [TestCase(WalletChangeReason.Offline)]
        [TestCase(WalletChangeReason.AdIdleCompensation)]
        [TestCase(WalletChangeReason.AdReward)]
        [TestCase(WalletChangeReason.Prestige)]
        [TestCase(WalletChangeReason.IdleIncome)]
        [TestCase(WalletChangeReason.PlinkoIncome)]
        public void Add_FaucetReason_AcceptedWithReasonOnEvent(WalletChangeReason reason)
        {
            Assert.IsTrue(Wallet.Add(EconomyIds.Coin, 1d, reason));

            Assert.AreEqual(1, Recorder.Count);
            Assert.AreEqual(reason, Recorder.Last.Reason);
        }

        [TestCase(UnknownCurrencyId)]
        [TestCase((string)null)]
        [TestCase("")]
        public void GetBalance_UnregisteredCurrency_ReturnsZero(string currencyId)
        {
            Assert.AreEqual(0d, Wallet.GetBalance(currencyId));
        }

        [Test]
        public void Add_NearDefaultCap_ClampsAtCap()
        {
            double cap = Config.WalletSafeCap;
            Assert.IsTrue(Wallet.Add(EconomyIds.Coin, cap - 1d, WalletChangeReason.AdReward));

            Assert.IsTrue(Wallet.Add(EconomyIds.Coin, cap / 10d, WalletChangeReason.AdReward));

            Assert.AreEqual(cap, Wallet.GetBalance(EconomyIds.Coin));
            Assert.AreEqual(0, Sink.WarningCount);
        }

        [Test]
        public void Add_OneBelowSmallCap_ClampsAndReportsActualDelta()
        {
            Config.SetWalletSafeCapForTest(SmallCap);
            Assert.IsTrue(Wallet.Add(EconomyIds.Coin, SmallCap - 1d, WalletChangeReason.AdReward));

            Assert.IsTrue(Wallet.Add(EconomyIds.Coin, SmallCap / 10d, WalletChangeReason.AdReward));

            Assert.AreEqual(SmallCap, Wallet.GetBalance(EconomyIds.Coin));
            Assert.AreEqual(2, Recorder.Count);
            Assert.AreEqual(SmallCap - 1d, Recorder.Last.Previous);
            Assert.AreEqual(SmallCap, Recorder.Last.Current);
            Assert.AreEqual(1d, Recorder.Last.Delta);
            AssertEventIntegrity(Recorder.Last);
        }

        [Test]
        public void Add_AtCap_ReturnsTrueWithoutEventOrWarning()
        {
            Config.SetWalletSafeCapForTest(SmallCap);
            Assert.IsTrue(Wallet.Add(EconomyIds.Coin, SmallCap, WalletChangeReason.AdReward));
            int eventsBefore = Recorder.Count;

            Assert.IsTrue(Wallet.Add(EconomyIds.Coin, 5d, WalletChangeReason.AdReward));

            Assert.AreEqual(SmallCap, Wallet.GetBalance(EconomyIds.Coin));
            Assert.AreEqual(eventsBefore, Recorder.Count);
            Assert.AreEqual(0, Sink.CountWarnings(EconomyDiagnosticCode.RejectedAdd));
        }

        [Test]
        public void RegisterCurrency_DuplicateNullOrEmpty_ReturnsFalse()
        {
            Assert.IsFalse(Wallet.RegisterCurrency(EconomyIds.Coin));
            Assert.IsFalse(Wallet.RegisterCurrency(null));
            Assert.IsFalse(Wallet.RegisterCurrency(string.Empty));
            Assert.IsTrue(Wallet.RegisterCurrency(EconomyIds.ShopStamp));
            Assert.AreEqual(0d, Wallet.GetBalance(EconomyIds.ShopStamp));
        }

        [Test]
        public void Constructor_NullArguments_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => new WalletService(null, Sink));
            Assert.Throws<ArgumentNullException>(() => new WalletService(Config, null));
        }

        private void GiveStartingBalance()
        {
            Assert.IsTrue(Wallet.Add(EconomyIds.Coin, StartingBalance, WalletChangeReason.AdReward));
            Assert.AreEqual(1, Recorder.Count);
            Assert.AreEqual(0, Sink.WarningCount);
        }

        private void AssertRejectedAndUnchanged()
        {
            Assert.AreEqual(StartingBalance, Wallet.GetBalance(EconomyIds.Coin));
            Assert.AreEqual(1, Recorder.Count);
            Assert.AreEqual(1, Sink.CountWarnings(EconomyDiagnosticCode.RejectedAdd));
            Assert.AreEqual(1, Sink.WarningCount);
        }
    }
}
