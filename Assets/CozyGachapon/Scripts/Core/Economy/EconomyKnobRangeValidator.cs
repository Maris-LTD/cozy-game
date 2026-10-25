using System.Collections.Generic;

namespace CozyGachapon.Economy
{
    public static class EconomyKnobRangeValidator
    {
        public const string StartingCoins = "startingCoins";
        public const string WalletSafeCap = "walletSafeCap";
        public const string SpinBasePrice = "spinBasePrice";
        public const string SpinGrowth = "spinGrowth";
        public const string CoinValueExponent = "coinValueExponent";
        public const string PriceNiceSigDigits = "priceNiceSigDigits";
        public const string IdleCpsUnit = "idleCpsUnit";
        public const string SynergyTierCap = "synergyTierCap";
        public const string BoostTierCap = "boostTierCap";
        public const string PrestigeTierCap = "prestigeTierCap";
        public const string GlobalTierCap = "globalTierCap";
        public const string PlinkoFlatPerCoinCap = "plinkoFlatPerCoinCap";
        public const string MaxFrameDelta = "maxFrameDelta";
        public const string AdCompensationMaxSeconds = "adCompensationMaxSeconds";
        public const string PlinkoRateWindowSeconds = "plinkoRateWindowSeconds";
        public const string RatesPublishInterval = "ratesPublishInterval";
        public const string DisplaySigDigits = "displaySigDigits";
        public const string RateDecimals = "rateDecimals";

        public static void Validate(EconomyConfig config, EconomySafeRanges ranges, List<EconomyConfigIssue> results)
        {
            Check(StartingCoins, config.StartingCoins, ranges.StartingCoins, results);
            Check(WalletSafeCap, config.WalletSafeCap, ranges.WalletSafeCap, results);
            Check(SpinBasePrice, config.SpinBasePrice, ranges.SpinBasePrice, results);
            Check(SpinGrowth, config.SpinGrowth, ranges.SpinGrowth, results);
            Check(CoinValueExponent, config.CoinValueExponent, ranges.CoinValueExponent, results);
            Check(PriceNiceSigDigits, config.PriceNiceSigDigits, ranges.PriceNiceSigDigits, results);
            Check(IdleCpsUnit, config.IdleCpsUnit, ranges.IdleCpsUnit, results);
            Check(SynergyTierCap, config.SynergyTierCap, ranges.SynergyTierCap, results);
            Check(BoostTierCap, config.BoostTierCap, ranges.BoostTierCap, results);
            Check(PrestigeTierCap, config.PrestigeTierCap, ranges.PrestigeTierCap, results);
            Check(GlobalTierCap, config.GlobalTierCap, ranges.GlobalTierCap, results);
            Check(PlinkoFlatPerCoinCap, config.PlinkoFlatPerCoinCap, ranges.PlinkoFlatPerCoinCap, results);
            Check(MaxFrameDelta, config.MaxFrameDelta, ranges.MaxFrameDelta, results);
            Check(AdCompensationMaxSeconds, config.AdCompensationMaxSeconds, ranges.AdCompensationMaxSeconds, results);
            Check(PlinkoRateWindowSeconds, config.PlinkoRateWindowSeconds, ranges.PlinkoRateWindowSeconds, results);
            Check(RatesPublishInterval, config.RatesPublishInterval, ranges.RatesPublishInterval, results);
            Check(DisplaySigDigits, config.DisplaySigDigits, ranges.DisplaySigDigits, results);
            Check(RateDecimals, config.RateDecimals, ranges.RateDecimals, results);
        }

        private static void Check(string subject, double value, DoubleRange range, List<EconomyConfigIssue> results)
        {
            if (!range.Contains(value))
            {
                results.Add(EconomyConfigIssue.ForKnob(EconomyConfigIssueCode.KnobOutOfRange, subject, value, range));
            }
        }
    }
}
