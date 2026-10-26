using System;
using CozyGachapon.Contracts;

namespace CozyGachapon.Economy
{
    public static class OfflineRateCalculator
    {
        public static double Compute(
            IncomeSourceTable sources,
            ModifierBook book,
            TierMultiplierCalculator calculator,
            EconomyConfig config,
            double currentIdleCps)
        {
            if (sources == null || sources.Count == 0 || book == null || calculator == null)
            {
                return 0d;
            }

            double idleCpsUnit = config != null ? config.IdleCpsUnit : 2d;
            double prestigeTier = calculator.ChannelTierPermanent(book, ModifierTier.Prestige, IncomeChannel.Idle);
            double globalTier = calculator.ChannelTierPermanent(book, ModifierTier.Global, IncomeChannel.Idle);

            double sum = 0d;
            for (int i = 0; i < sources.Count; i++)
            {
                if (sources.ChannelAt(i) != IncomeChannel.Idle)
                {
                    continue;
                }

                string key = sources.KeyAt(i);
                double baseRate = sources.BaseRateAt(i);
                double flatUnits = calculator.FlatUnitsPermanent(book, key);
                double flat = flatUnits * idleCpsUnit;
                double synergy = calculator.SynergyPermanent(book, key, IncomeChannel.Idle);
                sum += (baseRate + flat) * synergy;
            }

            double offline = sum * prestigeTier * globalTier;
            return Math.Min(offline, currentIdleCps);
        }
    }
}
