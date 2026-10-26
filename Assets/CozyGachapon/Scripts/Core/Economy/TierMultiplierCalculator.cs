using System;
using CozyGachapon.Contracts;

namespace CozyGachapon.Economy
{
    public sealed class TierMultiplierCalculator
    {
        private readonly EconomyConfig _config;

        public TierMultiplierCalculator(EconomyConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public double Synergy(ModifierBook book, string sourceKey, IncomeChannel sourceChannel)
        {
            double sum = 0d;
            for (int i = 0; i < book.Count; i++)
            {
                ref readonly IncomeModifier modifier = ref book.At(i);
                if (modifier.Tier == ModifierTier.Synergy && TargetsSource(in modifier, sourceKey, sourceChannel))
                {
                    sum += ValueSanitizer.Sanitize(modifier.Value);
                }
            }

            return Clamp(sum, CapFor(ModifierTier.Synergy));
        }

        public double SynergyTargeted(ModifierBook book, string sourceKey)
        {
            double sum = 0d;
            for (int i = 0; i < book.Count; i++)
            {
                ref readonly IncomeModifier modifier = ref book.At(i);
                if (modifier.Tier == ModifierTier.Synergy
                    && modifier.Target == ModifierTarget.Source
                    && string.Equals(modifier.TargetKey, sourceKey, StringComparison.Ordinal))
                {
                    sum += ValueSanitizer.Sanitize(modifier.Value);
                }
            }

            return Clamp(sum, CapFor(ModifierTier.Synergy));
        }

        public double ChannelTier(ModifierBook book, ModifierTier tier, IncomeChannel channel)
        {
            double sum = 0d;
            for (int i = 0; i < book.Count; i++)
            {
                ref readonly IncomeModifier modifier = ref book.At(i);
                if (modifier.Tier == tier && CoversChannel(modifier.Channel, channel))
                {
                    sum += ValueSanitizer.Sanitize(modifier.Value);
                }
            }

            return Clamp(sum, CapFor(tier));
        }

        public double FlatUnits(ModifierBook book, string sourceKey)
        {
            double sum = 0d;
            for (int i = 0; i < book.Count; i++)
            {
                ref readonly IncomeModifier modifier = ref book.At(i);
                if (modifier.Tier == ModifierTier.Base
                    && modifier.Target == ModifierTarget.Source
                    && string.Equals(modifier.TargetKey, sourceKey, StringComparison.Ordinal))
                {
                    sum += ValueSanitizer.Sanitize(modifier.Value);
                }
            }

            return sum;
        }

        public double SynergyPermanent(ModifierBook book, string sourceKey, IncomeChannel sourceChannel)
        {
            double sum = 0d;
            for (int i = 0; i < book.Count; i++)
            {
                ref readonly IncomeModifier modifier = ref book.At(i);
                if (modifier.Tier == ModifierTier.Synergy
                    && !modifier.IsTemporary
                    && TargetsSource(in modifier, sourceKey, sourceChannel))
                {
                    sum += ValueSanitizer.Sanitize(modifier.Value);
                }
            }

            return Clamp(sum, CapFor(ModifierTier.Synergy));
        }

        public double ChannelTierPermanent(ModifierBook book, ModifierTier tier, IncomeChannel channel)
        {
            if (tier == ModifierTier.Boost)
            {
                return 1d;
            }

            double sum = 0d;
            for (int i = 0; i < book.Count; i++)
            {
                ref readonly IncomeModifier modifier = ref book.At(i);
                if (modifier.Tier == tier && !modifier.IsTemporary && CoversChannel(modifier.Channel, channel))
                {
                    sum += ValueSanitizer.Sanitize(modifier.Value);
                }
            }

            return Clamp(sum, CapFor(tier));
        }

        public double FlatUnitsPermanent(ModifierBook book, string sourceKey)
        {
            double sum = 0d;
            for (int i = 0; i < book.Count; i++)
            {
                ref readonly IncomeModifier modifier = ref book.At(i);
                if (modifier.Tier == ModifierTier.Base
                    && !modifier.IsTemporary
                    && modifier.Target == ModifierTarget.Source
                    && string.Equals(modifier.TargetKey, sourceKey, StringComparison.Ordinal))
                {
                    sum += ValueSanitizer.Sanitize(modifier.Value);
                }
            }

            return sum;
        }

        private static bool TargetsSource(in IncomeModifier modifier, string sourceKey, IncomeChannel sourceChannel)
        {
            if (modifier.Target == ModifierTarget.Source)
            {
                return string.Equals(modifier.TargetKey, sourceKey, StringComparison.Ordinal);
            }

            return CoversChannel(modifier.Channel, sourceChannel);
        }

        private static bool CoversChannel(IncomeChannel modifierChannel, IncomeChannel channel)
        {
            return modifierChannel == IncomeChannel.Both || modifierChannel == channel;
        }

        private static double Clamp(double sum, double cap)
        {
            double multiplier = 1d + sum;
            return multiplier > cap ? cap : multiplier;
        }

        private double CapFor(ModifierTier tier)
        {
            switch (tier)
            {
                case ModifierTier.Synergy:
                    return _config.SynergyTierCap;
                case ModifierTier.Boost:
                    return _config.BoostTierCap;
                case ModifierTier.Prestige:
                    return _config.PrestigeTierCap;
                case ModifierTier.Global:
                    return _config.GlobalTierCap;
                default:
                    return double.MaxValue;
            }
        }
    }
}
