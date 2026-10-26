using System;
using CozyGachapon.Contracts;

namespace CozyGachapon.Economy
{
    public static class IdlePreviewCalculator
    {
        private const string ShelfPrefix = "shelf.";

        public static double Compute(
            IIdlePlan plan,
            ModifierBook currentBook,
            TierMultiplierCalculator calculator,
            EconomyConfig config)
        {
            if (plan == null || plan.SourceCount <= 0 || currentBook == null || calculator == null)
            {
                return 0d;
            }

            double idleCpsUnit = config != null ? ValueSanitizer.Sanitize(config.IdleCpsUnit) : 2d;
            double synergyCap = config != null ? config.SynergyTierCap : 4d;
            double boost = calculator.ChannelTier(currentBook, ModifierTier.Boost, IncomeChannel.Idle);
            double prestige = calculator.ChannelTier(currentBook, ModifierTier.Prestige, IncomeChannel.Idle);
            double global = calculator.ChannelTier(currentBook, ModifierTier.Global, IncomeChannel.Idle);

            double sum = 0d;
            int sourceCount = plan.SourceCount;
            for (int i = 0; i < sourceCount; i++)
            {
                IdlePlanSource planSource = plan.GetSource(i);
                if (string.IsNullOrEmpty(planSource.SourceKey))
                {
                    continue;
                }

                double baseRate = ValueSanitizer.Sanitize(planSource.BaseRate);
                double flatUnits = ComputeFlatUnits(in planSource, plan, currentBook);
                double synergy = ComputeSynergy(planSource.SourceKey, plan, currentBook, synergyCap);
                sum += (baseRate + flatUnits * idleCpsUnit) * synergy;
            }

            return sum * boost * prestige * global;
        }

        private static double ComputeFlatUnits(
            in IdlePlanSource planSource,
            IIdlePlan plan,
            ModifierBook currentBook)
        {
            double flat = ValueSanitizer.Sanitize(planSource.FlatUnits);
            for (int i = 0; i < currentBook.Count; i++)
            {
                ref readonly IncomeModifier mod = ref currentBook.At(i);
                if (!IsShelfOwned(mod.SourceId)
                    && mod.Tier == ModifierTier.Base
                    && mod.Target == ModifierTarget.Source
                    && string.Equals(mod.TargetKey, planSource.SourceKey, StringComparison.Ordinal))
                {
                    flat += ValueSanitizer.Sanitize(mod.Value);
                }
            }

            if (flat <= 0d)
            {
                int modCount = plan.ModifierCount;
                for (int i = 0; i < modCount; i++)
                {
                    IncomeModifier mod = plan.GetModifier(i);
                    if (mod.Tier == ModifierTier.Base
                        && mod.Target == ModifierTarget.Source
                        && string.Equals(mod.TargetKey, planSource.SourceKey, StringComparison.Ordinal))
                    {
                        flat += ValueSanitizer.Sanitize(mod.Value);
                    }
                }
            }

            return flat;
        }

        private static double ComputeSynergy(
            string sourceKey,
            IIdlePlan plan,
            ModifierBook currentBook,
            double synergyCap)
        {
            double sum = 0d;
            for (int i = 0; i < currentBook.Count; i++)
            {
                ref readonly IncomeModifier mod = ref currentBook.At(i);
                if (!IsShelfOwned(mod.SourceId)
                    && mod.Tier == ModifierTier.Synergy
                    && TargetsIdleSource(in mod, sourceKey))
                {
                    sum += ValueSanitizer.Sanitize(mod.Value);
                }
            }

            int modCount = plan.ModifierCount;
            for (int i = 0; i < modCount; i++)
            {
                IncomeModifier mod = plan.GetModifier(i);
                if (mod.Tier == ModifierTier.Synergy && TargetsIdleSource(in mod, sourceKey))
                {
                    sum += ValueSanitizer.Sanitize(mod.Value);
                }
            }

            double multiplier = 1d + sum;
            return multiplier > synergyCap ? synergyCap : multiplier;
        }

        private static bool TargetsIdleSource(in IncomeModifier mod, string sourceKey)
        {
            if (mod.Target == ModifierTarget.Source)
            {
                return string.Equals(mod.TargetKey, sourceKey, StringComparison.Ordinal);
            }

            return mod.Channel == IncomeChannel.Idle || mod.Channel == IncomeChannel.Both;
        }

        private static bool IsShelfOwned(string sourceId)
        {
            return !string.IsNullOrEmpty(sourceId)
                && sourceId.StartsWith(ShelfPrefix, StringComparison.Ordinal);
        }
    }
}
