using CozyGachapon.Contracts;

namespace CozyGachapon.Economy
{
    public sealed class IncomePipeline : IIncomePipeline
    {
        private const int ChannelCount = 2;
        private const int TierCount = 5;

        private readonly ModifierBook _book = new ModifierBook();
        private readonly IncomeSourceTable _sources = new IncomeSourceTable();
        private readonly TierMultiplierCalculator _calculator;
        private readonly double[] _channelTierCache = new double[TierCount * ChannelCount];

        public IncomePipeline(EconomyConfig config)
        {
            _calculator = new TierMultiplierCalculator(config);
            IsDirty = true;
        }

        public bool IsDirty { get; private set; }

        public bool RegisterIncomeSource(string sourceKey, IncomeChannel channel, double baseRate)
        {
            if (!_sources.Upsert(sourceKey, channel, baseRate))
            {
                return false;
            }

            IsDirty = true;
            return true;
        }

        public bool RegisterModifier(in IncomeModifier modifier)
        {
            if (!IsWellFormed(in modifier))
            {
                return false;
            }

            _book.Add(in modifier);
            IsDirty = true;
            return true;
        }

        public int Unregister(string sourceId)
        {
            int removed = _book.RemoveBySourceId(sourceId);
            if (removed > 0)
            {
                IsDirty = true;
            }

            return removed;
        }

        public double GetSynergyMultiplier(string sourceKey)
        {
            int index = _sources.IndexOf(sourceKey);
            if (index < 0)
            {
                return 1d;
            }

            EnsureFresh();
            return _sources.SynergyAt(index);
        }

        public double GetTierMultiplier(ModifierTier tier, IncomeChannel channel)
        {
            if (!IsCachedTier(tier) || !IsConcrete(channel))
            {
                return 1d;
            }

            EnsureFresh();
            return _channelTierCache[CacheIndex(tier, channel)];
        }

        public double GetFlatUnits(string sourceKey)
        {
            int index = _sources.IndexOf(sourceKey);
            if (index < 0)
            {
                return 0d;
            }

            EnsureFresh();
            return _sources.FlatUnitsAt(index);
        }

        public double GetChannelIncome(string sourceKey, double flatUnitValue)
        {
            int index = _sources.IndexOf(sourceKey);
            if (index < 0)
            {
                return 0d;
            }

            EnsureFresh();
            IncomeChannel channel = _sources.ChannelAt(index);
            double flat = _sources.FlatUnitsAt(index) * ValueSanitizer.Sanitize(flatUnitValue);
            return ChannelIncomeCalculator.Compute(
                _sources.BaseRateAt(index),
                flat,
                _sources.SynergyAt(index),
                _channelTierCache[CacheIndex(ModifierTier.Boost, channel)],
                _channelTierCache[CacheIndex(ModifierTier.Prestige, channel)],
                _channelTierCache[CacheIndex(ModifierTier.Global, channel)]);
        }

        private static bool IsWellFormed(in IncomeModifier modifier)
        {
            if (string.IsNullOrEmpty(modifier.SourceId))
            {
                return false;
            }

            if (modifier.Tier < ModifierTier.Base || modifier.Tier > ModifierTier.Global
                || modifier.Channel < IncomeChannel.Idle || modifier.Channel > IncomeChannel.Both
                || modifier.Target < ModifierTarget.Channel || modifier.Target > ModifierTarget.Source)
            {
                return false;
            }

            return modifier.Target != ModifierTarget.Source || !string.IsNullOrEmpty(modifier.TargetKey);
        }

        private static bool IsCachedTier(ModifierTier tier)
        {
            return tier == ModifierTier.Boost || tier == ModifierTier.Prestige || tier == ModifierTier.Global;
        }

        private static bool IsConcrete(IncomeChannel channel)
        {
            return channel == IncomeChannel.Idle || channel == IncomeChannel.Plinko;
        }

        private static int CacheIndex(ModifierTier tier, IncomeChannel channel)
        {
            return (int)tier * ChannelCount + (int)channel;
        }

        private void EnsureFresh()
        {
            if (!IsDirty)
            {
                return;
            }

            RecomputeChannelTiers(IncomeChannel.Idle);
            RecomputeChannelTiers(IncomeChannel.Plinko);
            for (int i = 0; i < _sources.Count; i++)
            {
                string key = _sources.KeyAt(i);
                double synergy = _calculator.Synergy(_book, key, _sources.ChannelAt(i));
                _sources.SetDerived(i, _calculator.FlatUnits(_book, key), synergy);
            }

            IsDirty = false;
        }

        private void RecomputeChannelTiers(IncomeChannel channel)
        {
            _channelTierCache[CacheIndex(ModifierTier.Boost, channel)] =
                _calculator.ChannelTier(_book, ModifierTier.Boost, channel);
            _channelTierCache[CacheIndex(ModifierTier.Prestige, channel)] =
                _calculator.ChannelTier(_book, ModifierTier.Prestige, channel);
            _channelTierCache[CacheIndex(ModifierTier.Global, channel)] =
                _calculator.ChannelTier(_book, ModifierTier.Global, channel);
        }
    }
}
