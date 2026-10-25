using CozyGachapon.Contracts;
using NUnit.Framework;

namespace CozyGachapon.Tests.Economy
{
    public class WalletSpendRejectionTests : EconomyWalletFixture
    {
        private const double StartingBalance = 500d;
        private const double Price = 100d;
        private const int UndefinedReasonValue = 999;
        private const string UnknownCurrencyId = "currency.unknown";

        [SetUp]
        public void SeedStartingBalance()
        {
            Seed(StartingBalance);
        }

        [Test]
        public void TrySpend_DefaultContext_FailsWithoutEventsAndWarnsSink()
        {
            int eventsBefore = Recorder.Count;

            bool spent = Wallet.TrySpend(EconomyIds.Coin, Price, SpendReason.GachaSpin, default);

            Assert.IsFalse(spent);
            Assert.AreEqual(StartingBalance, Wallet.GetBalance(EconomyIds.Coin));
            Assert.AreEqual(eventsBefore, Recorder.Count);
            Assert.AreEqual(0, Recorder.RejectedCount);
            Assert.AreEqual(1, Sink.CountWarnings(EconomyDiagnosticCode.RejectedSpend));
        }

        [Test]
        public void TrySpend_ContextFromRejectedIssue_Fails()
        {
            PlayerActionContext context = Issuer.Issue(PlayerActionKind.None);

            Assert.IsFalse(Wallet.TrySpend(EconomyIds.Coin, Price, SpendReason.GachaSpin, context));

            Assert.AreEqual(StartingBalance, Wallet.GetBalance(EconomyIds.Coin));
        }

        [TestCase(-1d)]
        [TestCase(0d)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void TrySpend_InvalidAmount_FailsWithoutEvents(double amount)
        {
            int eventsBefore = Recorder.Count;

            bool spent = Wallet.TrySpend(EconomyIds.Coin, amount, SpendReason.GachaSpin, IssueSpin());

            Assert.IsFalse(spent);
            Assert.AreEqual(StartingBalance, Wallet.GetBalance(EconomyIds.Coin));
            Assert.AreEqual(eventsBefore, Recorder.Count);
            Assert.AreEqual(0, Recorder.RejectedCount);
            Assert.AreEqual(1, Sink.CountWarnings(EconomyDiagnosticCode.RejectedSpend));
            Assert.IsFalse(Wallet.CanAfford(EconomyIds.Coin, amount));
        }

        [Test]
        public void TrySpend_UnknownCurrency_FailsWithoutEvents()
        {
            int eventsBefore = Recorder.Count;

            Assert.IsFalse(Wallet.TrySpend(UnknownCurrencyId, Price, SpendReason.GachaSpin, IssueSpin()));

            Assert.AreEqual(StartingBalance, Wallet.GetBalance(EconomyIds.Coin));
            Assert.AreEqual(eventsBefore, Recorder.Count);
            Assert.AreEqual(0, Recorder.RejectedCount);
            Assert.AreEqual(1, Sink.CountWarnings(EconomyDiagnosticCode.RejectedSpend));
        }

        [Test]
        public void TrySpend_NullCurrency_FailsWithoutEvents()
        {
            int eventsBefore = Recorder.Count;

            Assert.IsFalse(Wallet.TrySpend(null, Price, SpendReason.GachaSpin, IssueSpin()));

            Assert.AreEqual(StartingBalance, Wallet.GetBalance(EconomyIds.Coin));
            Assert.AreEqual(eventsBefore, Recorder.Count);
            Assert.AreEqual(0, Recorder.RejectedCount);
        }

        [Test]
        public void TrySpend_UndefinedReason_FailsWithoutEvents()
        {
            int eventsBefore = Recorder.Count;

            Assert.IsFalse(Wallet.TrySpend(EconomyIds.Coin, Price, (SpendReason)UndefinedReasonValue, IssueSpin()));

            Assert.AreEqual(StartingBalance, Wallet.GetBalance(EconomyIds.Coin));
            Assert.AreEqual(eventsBefore, Recorder.Count);
            Assert.AreEqual(0, Recorder.RejectedCount);
        }

        [Test]
        public void CanAfford_UnknownOrNullCurrency_IsFalse()
        {
            Assert.IsFalse(Wallet.CanAfford(UnknownCurrencyId, Price));
            Assert.IsFalse(Wallet.CanAfford(null, Price));
        }

        [Test]
        public void CanAfford_NeverRaisesEventsOrWarnings()
        {
            int eventsBefore = Recorder.Count;

            Wallet.CanAfford(EconomyIds.Coin, Price);
            Wallet.CanAfford(EconomyIds.Coin, double.NaN);
            Wallet.CanAfford(UnknownCurrencyId, Price);
            Wallet.CanAfford(EconomyIds.Coin, StartingBalance * 2d);

            Assert.AreEqual(eventsBefore, Recorder.Count);
            Assert.AreEqual(0, Recorder.RejectedCount);
            Assert.AreEqual(0, Sink.CountWarnings(EconomyDiagnosticCode.RejectedSpend));
        }
    }
}
