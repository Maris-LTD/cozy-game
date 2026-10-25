using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using CozyGachapon.Tests.Pooling;
using NUnit.Framework;

namespace CozyGachapon.Tests.Economy
{
    public class WalletSpendZeroAllocTests : EconomyWalletFixture
    {
        private const int Iterations = 100;
        private const double Unaffordable = 1e9d;
        private const string UnknownCurrencyId = "currency.unknown";

        [Test]
        public void SpendAndCanAfford_AllPaths_DoNotAllocate()
        {
            WalletService wallet = Wallet;
            PlayerActionContext context = IssueSpin();

            int allocations = GcAllocProbe.CountAllocations(() => RunSpends(wallet, context));

            Assert.AreEqual(0, allocations);
            Assert.Greater(Recorder.Count, 0);
            Assert.Greater(Recorder.RejectedCount, 0);
            Assert.Greater(Sink.CountWarnings(EconomyDiagnosticCode.RejectedSpend), 0);
        }

        private static void RunSpends(WalletService wallet, PlayerActionContext context)
        {
            for (int i = 0; i < Iterations; i++)
            {
                wallet.Add(EconomyIds.Coin, 1d, WalletChangeReason.IdleIncome);
                wallet.CanAfford(EconomyIds.Coin, 1d);
                wallet.TrySpend(EconomyIds.Coin, 1d, SpendReason.GachaSpin, context);
                wallet.TrySpend(EconomyIds.Coin, Unaffordable, SpendReason.ShopPurchase, context);
                wallet.TrySpend(EconomyIds.Coin, 1d, SpendReason.GachaSpin, default);
                wallet.TrySpend(EconomyIds.Coin, double.NaN, SpendReason.GachaSpin, context);
                wallet.CanAfford(UnknownCurrencyId, 1d);
            }
        }
    }
}
