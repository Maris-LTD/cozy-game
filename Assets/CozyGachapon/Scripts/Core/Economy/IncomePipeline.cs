using System;
using CozyGachapon.Contracts;

namespace CozyGachapon.Economy
{
    public sealed class IncomePipeline : IIncomePipeline
    {
        private const int ChannelCount = 2;
        private const int TierCount = 5;

        private readonly EconomyConfig _config;
        private readonly IEconomyDiagnosticsSink _sink;
        private readonly ModifierBook _book = new ModifierBook();
        private readonly IncomeSourceTable _sources = new IncomeSourceTable();
        private readonly TierMultiplierCalculator _calculator;
        private readonly TierOwnershipPolicy _policy;
        private readonly PipelineDirtyTracker _tracker = new PipelineDirtyTracker();
        private readonly PlinkoRateWindow _plinkoRateWindow;
        private readonly double[] _channelTierCache = new double[TierCount * ChannelCount];
        private double _cachedIdleCps;
        private SubmitIncomeHandler _submitHandler;

        public event Action StateMutated;

        public IncomePipeline(EconomyConfig config, IEconomyDiagnosticsSink sink = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _sink = sink;
            _calculator = new TierMultiplierCalculator(_config);
            _policy = new TierOwnershipPolicy(_config.TierOwnerPrefixes);
            _plinkoRateWindow = new PlinkoRateWindow(_config.PlinkoRateWindowSeconds);
        }

        public bool IsDirty => _tracker.IsDirty;
        public int IdleRecomputeCount => _tracker.IdleRecomputeCount;
        public PlinkoRateWindow PlinkoRateWindow => _plinkoRateWindow;
        public double PlinkoRateAvg => _plinkoRateWindow.RateAvg;

        public double IdleCps
        {
            get
            {
                EnsureFresh();
                return _cachedIdleCps;
            }
        }

        public double OfflineRateSnapshot
        {
            get
            {
                EnsureFresh();
                return OfflineRateCalculator.Compute(_sources, _book, _calculator, _config, _cachedIdleCps);
            }
        }

        public bool RegisterIncomeSource(string sourceKey, double baseRate) =>
            RegisterIncomeSource(sourceKey, IncomeChannel.Idle, baseRate);

        public bool RegisterIncomeSource(string sourceKey, IncomeChannel channel, double baseRate)
        {
            if (!_sources.Upsert(sourceKey, channel, baseRate))
            {
                return false;
            }

            MarkDirtyAndNotify();
            return true;
        }

        public bool UnregisterIncomeSource(string sourceKey)
        {
            if (!_sources.Remove(sourceKey))
            {
                return false;
            }

            MarkDirtyAndNotify();
            return true;
        }

        public bool RegisterModifier(in IncomeModifier modifier)
        {
            if (!IsWellFormed(in modifier))
            {
                return false;
            }

            if (!_policy.IsAllowed(modifier.SourceId, modifier.Tier))
            {
                _sink?.ReportWarning(EconomyDiagnosticCode.ModifierTierRejected, modifier.SourceId, modifier.Value);
                return false;
            }

            _book.Add(in modifier);
            MarkDirtyAndNotify();
            return true;
        }

        public int Unregister(string sourceId)
        {
            int removed = _book.RemoveBySourceId(sourceId);
            if (removed > 0)
            {
                MarkDirtyAndNotify();
            }

            return removed;
        }

        public double GetSynergyMultiplier(string sourceKey)
        {
            int index = _sources.IndexOf(sourceKey);
            if (index >= 0)
            {
                EnsureFresh();
                return _sources.SynergyAt(index);
            }

            return _calculator.SynergyTargeted(_book, sourceKey);
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

        public void SetSubmitHandler(SubmitIncomeHandler handler) => _submitHandler = handler;

        public bool SubmitIncome(IncomeChannel channel, string sourceKey, double baseAmount) =>
            _submitHandler != null && _submitHandler.Submit(channel, sourceKey, baseAmount);

        public double GetSourceIdleCps(string sourceKey) =>
            SourceIdleCpsCalculator.Compute(sourceKey, _sources, _book, _calculator, _config);

        public double PreviewIdleCps(IIdlePlan plan) =>
            IdlePreviewCalculator.Compute(plan, _book, _calculator, _config);

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

        private static bool IsCachedTier(ModifierTier tier) =>
            tier == ModifierTier.Boost || tier == ModifierTier.Prestige || tier == ModifierTier.Global;

        private static bool IsConcrete(IncomeChannel channel) =>
            channel == IncomeChannel.Idle || channel == IncomeChannel.Plinko;

        private static int CacheIndex(ModifierTier tier, IncomeChannel channel) =>
            (int)tier * ChannelCount + (int)channel;

        private void MarkDirtyAndNotify()
        {
            _tracker.MarkDirty();
            StateMutated?.Invoke();
        }

        private void EnsureFresh()
        {
            if (!_tracker.IsDirty)
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

            double idleCpsUnit = _config != null ? _config.IdleCpsUnit : 2d;
            _cachedIdleCps = IdleCpsCalculator.Compute(
                _sources,
                idleCpsUnit,
                _channelTierCache[CacheIndex(ModifierTier.Boost, IncomeChannel.Idle)],
                _channelTierCache[CacheIndex(ModifierTier.Prestige, IncomeChannel.Idle)],
                _channelTierCache[CacheIndex(ModifierTier.Global, IncomeChannel.Idle)]);

            _tracker.OnRecomputed();
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
