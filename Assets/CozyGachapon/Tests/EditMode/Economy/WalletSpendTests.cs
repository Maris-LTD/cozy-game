using CozyGachapon.Contracts;
using NUnit.Framework;

namespace CozyGachapon.Tests.Economy
{
    public class WalletSpendTests : EconomyWalletFixture
    {
        private const double Price = 100d;
        private const double ShortBalance = 99.7d;
        private const double DoubleTapBalance = 150d;
        private const double FractionalPrice = 0.3d;

        [Test]
        public void TrySpend_BalanceEqualsPrice_SucceedsToZeroWithOneNegativeEvent()
        {
            Seed(Price);
            int eventsBefore = Recorder.Count;
            bool affordable = Wallet.CanAfford(EconomyIds.Coin, Price);

            bool spent = Wallet.TrySpend(EconomyIds.Coin, Price, SpendReason.GachaSpin, IssueSpin());

            Assert.IsTrue(affordable);
            Assert.AreEqual(affordable, spent);
            Assert.AreEqual(0d, Wallet.GetBalance(EconomyIds.Coin));
            Assert.AreEqual(eventsBefore + 1, Recorder.Count);
            WalletChangedEvent change = Recorder.Last;
            Assert.AreEqual(EconomyIds.Coin, change.CurrencyId);
            Assert.AreEqual(Price, change.Previous);
            Assert.AreEqual(0d, change.Current);
            Assert.AreEqual(-Price, change.Delta);
            Assert.AreEqual(WalletChangeReason.GachaSpin, change.Reason);
            AssertEventIntegrity(change);
            Assert.AreEqual(0, Recorder.RejectedCount);
            Assert.AreEqual(0, Sink.CountWarnings(EconomyDiagnosticCode.RejectedSpend));
        }

        [Test]
        public void TrySpend_BalanceJustBelowPrice_FailsKeepsBalanceAndRaisesOneRejection()
        {
            Seed(ShortBalance);
            int eventsBefore = Recorder.Count;
            bool affordable = Wallet.CanAfford(EconomyIds.Coin, Price);

            bool spent = Wallet.TrySpend(EconomyIds.Coin, Price, SpendReason.GachaSpin, IssueSpin());

            Assert.IsFalse(affordable);
            Assert.AreEqual(affordable, spent);
            Assert.AreEqual(ShortBalance, Wallet.GetBalance(EconomyIds.Coin));
            Assert.AreEqual(eventsBefore, Recorder.Count);
            Assert.AreEqual(1, Recorder.RejectedCount);
            SpendRejectedEvent rejection = Recorder.LastRejected;
            Assert.AreEqual(EconomyIds.Coin, rejection.CurrencyId);
            Assert.AreEqual(Price, rejection.Amount);
            Assert.AreEqual(SpendReason.GachaSpin, rejection.Reason);
            Assert.AreEqual(0, Sink.CountWarnings(EconomyDiagnosticCode.RejectedSpend));
        }

        [Test]
        public void TrySpend_ZeroBalance_FailsWithOneRejection()
        {
            Assert.IsFalse(Wallet.TrySpend(EconomyIds.Coin, Price, SpendReason.ShopPurchase, IssueSpin()));

            Assert.AreEqual(0d, Wallet.GetBalance(EconomyIds.Coin));
            Assert.AreEqual(1, Recorder.RejectedCount);
            Assert.AreEqual(SpendReason.ShopPurchase, Recorder.LastRejected.Reason);
        }

        [Test]
        public void TrySpend_SameFrameDoubleTap_ChargesOnceAndRejectsSecond()
        {
            Seed(DoubleTapBalance);
            int eventsBefore = Recorder.Count;

            bool first = Wallet.TrySpend(EconomyIds.Coin, Price, SpendReason.GachaSpin, IssueSpin());
            bool second = Wallet.TrySpend(EconomyIds.Coin, Price, SpendReason.GachaSpin, IssueSpin());

            Assert.IsTrue(first);
            Assert.IsFalse(second);
            Assert.AreEqual(DoubleTapBalance - Price, Wallet.GetBalance(EconomyIds.Coin));
            Assert.AreEqual(eventsBefore + 1, Recorder.Count);
            Assert.AreEqual(1, Recorder.RejectedCount);
        }

        [TestCase(SpendReason.GachaSpin, WalletChangeReason.GachaSpin)]
        [TestCase(SpendReason.ShopPurchase, WalletChangeReason.ShopPurchase)]
        [TestCase(SpendReason.PrestigeUpgrade, WalletChangeReason.PrestigeUpgrade)]
        [TestCase(SpendReason.NeighborCandy, WalletChangeReason.NeighborCandy)]
        public void TrySpend_Reason_MapsToWalletChangeReason(SpendReason reason, WalletChangeReason expected)
        {
            Seed(Price);

            Assert.IsTrue(Wallet.TrySpend(EconomyIds.Coin, 1d, reason, IssueSpin()));

            Assert.AreEqual(expected, Recorder.Last.Reason);
        }

        [Test]
        public void TrySpend_FractionalAmount_KeepsFractionInBalance()
        {
            Seed(Price);

            Assert.IsTrue(Wallet.TrySpend(EconomyIds.Coin, FractionalPrice, SpendReason.ShopPurchase, IssueSpin()));

            AssertBalance(Price - FractionalPrice, EconomyIds.Coin);
            AssertEventIntegrity(Recorder.Last);
        }

        [Test]
        public void CanAfford_DoesNotChangeBalanceOrRaiseAnything()
        {
            Seed(Price);
            int eventsBefore = Recorder.Count;

            Assert.IsTrue(Wallet.CanAfford(EconomyIds.Coin, Price));
            Assert.IsFalse(Wallet.CanAfford(EconomyIds.Coin, Price + 0.1d));

            Assert.AreEqual(Price, Wallet.GetBalance(EconomyIds.Coin));
            Assert.AreEqual(eventsBefore, Recorder.Count);
            Assert.AreEqual(0, Recorder.RejectedCount);
            Assert.AreEqual(0, Sink.WarningCount);
        }
    }
}
