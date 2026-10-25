using CozyGachapon.Contracts;

namespace CozyGachapon.Economy
{
    public static class SpendReasonRules
    {
        public static bool TryToWalletChangeReason(SpendReason reason, out WalletChangeReason walletReason)
        {
            switch (reason)
            {
                case SpendReason.GachaSpin:
                    walletReason = WalletChangeReason.GachaSpin;
                    return true;
                case SpendReason.ShopPurchase:
                    walletReason = WalletChangeReason.ShopPurchase;
                    return true;
                case SpendReason.PrestigeUpgrade:
                    walletReason = WalletChangeReason.PrestigeUpgrade;
                    return true;
                case SpendReason.NeighborCandy:
                    walletReason = WalletChangeReason.NeighborCandy;
                    return true;
                default:
                    walletReason = default;
                    return false;
            }
        }
    }
}
