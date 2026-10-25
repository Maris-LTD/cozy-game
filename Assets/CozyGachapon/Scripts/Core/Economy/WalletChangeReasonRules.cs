using CozyGachapon.Contracts;

namespace CozyGachapon.Economy
{
    public static class WalletChangeReasonRules
    {
        public static bool IsFaucet(WalletChangeReason reason)
        {
            switch (reason)
            {
                case WalletChangeReason.Offline:
                case WalletChangeReason.AdIdleCompensation:
                case WalletChangeReason.AdReward:
                case WalletChangeReason.Prestige:
                case WalletChangeReason.IdleIncome:
                case WalletChangeReason.PlinkoIncome:
                    return true;
                default:
                    return false;
            }
        }
    }
}
