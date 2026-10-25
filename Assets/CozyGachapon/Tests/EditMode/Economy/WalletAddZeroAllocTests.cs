using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using CozyGachapon.Tests.Pooling;
using NUnit.Framework;

namespace CozyGachapon.Tests.Economy
{
    public class WalletAddZeroAllocTests : EconomyWalletFixture
    {
        private const int Iterations = 200;
        private const string UnknownCurrencyId = "currency.unknown";

        [Test]
        public void Add_AcceptedClampedAndRejected_DoesNotAllocate()
        {
            Assert.IsTrue(Wallet.RegisterCurrency(EconomyIds.ShopStamp));
            WalletService wallet = Wallet;
            double cap = Config.WalletSafeCap;

            int allocations = GcAllocProbe.CountAllocations(() => RunAdds(wallet, cap));

            Assert.AreEqual(0, allocations);
            Assert.Greater(Recorder.Count, 0);
            Assert.AreEqual(cap, Wallet.GetBalance(EconomyIds.ShopStamp));
            Assert.Greater(Sink.CountWarnings(EconomyDiagnosticCode.RejectedAdd), 0);
        }

        private static void RunAdds(WalletService wallet, double cap)
        {
            for (int i = 0; i < Iterations; i++)
            {
                wallet.Add(EconomyIds.Coin, 1d, WalletChangeReason.IdleIncome);
                wallet.Add(EconomyIds.ShopStamp, cap, WalletChangeReason.Prestige);
                wallet.Add(EconomyIds.Coin, double.NaN, WalletChangeReason.AdReward);
                wallet.Add(UnknownCurrencyId, 1d, WalletChangeReason.AdReward);
                wallet.Add(EconomyIds.Coin, 1d, WalletChangeReason.GachaSpin);
            }
        }
    }
}
