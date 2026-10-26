using CozyGachapon.Contracts;

namespace CozyGachapon.Economy
{
    public static class WalletChangeReasonValidator
    {
        public static bool IsValid(WalletChangeReason reason)
        {
            return reason >= WalletChangeReason.Offline
                && reason <= WalletChangeReason.PlinkoIncome;
        }
    }
}
