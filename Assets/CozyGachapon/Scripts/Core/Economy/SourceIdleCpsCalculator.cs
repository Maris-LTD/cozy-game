using CozyGachapon.Contracts;

namespace CozyGachapon.Economy
{
    public static class SourceIdleCpsCalculator
    {
        public static double Compute(
            string sourceKey,
            IncomeSourceTable sources,
            ModifierBook book,
            TierMultiplierCalculator calculator,
            EconomyConfig config)
        {
            if (string.IsNullOrEmpty(sourceKey) || sources == null || book == null || calculator == null)
            {
                return 0d;
            }

            int index = sources.IndexOf(sourceKey);
            if (index < 0 || sources.ChannelAt(index) != IncomeChannel.Idle)
            {
                return 0d;
            }

            double idleCpsUnit = config != null ? ValueSanitizer.Sanitize(config.IdleCpsUnit) : 2d;
            double baseRate = ValueSanitizer.Sanitize(sources.BaseRateAt(index));
            double flatUnits = calculator.FlatUnits(book, sourceKey);
            double flat = flatUnits * idleCpsUnit;
            double synergy = calculator.Synergy(book, sourceKey, IncomeChannel.Idle);
            double boost = calculator.ChannelTier(book, ModifierTier.Boost, IncomeChannel.Idle);
            double prestige = calculator.ChannelTier(book, ModifierTier.Prestige, IncomeChannel.Idle);
            double global = calculator.ChannelTier(book, ModifierTier.Global, IncomeChannel.Idle);

            return ChannelIncomeCalculator.Compute(baseRate, flat, synergy, boost, prestige, global);
        }
    }
}
