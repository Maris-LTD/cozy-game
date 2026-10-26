namespace CozyGachapon.Contracts
{
    public interface IIncomePipeline
    {
        double IdleCps { get; }
        double PlinkoRateAvg { get; }
        double OfflineRateSnapshot { get; }
        int IdleRecomputeCount { get; }

        bool RegisterIncomeSource(string sourceKey, double baseRate);
        bool RegisterIncomeSource(string sourceKey, IncomeChannel channel, double baseRate);
        bool UnregisterIncomeSource(string sourceKey);
        bool RegisterModifier(in IncomeModifier modifier);
        int Unregister(string sourceId);
        double GetSynergyMultiplier(string sourceKey);
        double GetTierMultiplier(ModifierTier tier, IncomeChannel channel);
        double GetFlatUnits(string sourceKey);
        double GetChannelIncome(string sourceKey, double flatUnitValue);
        bool SubmitIncome(IncomeChannel channel, string sourceKey, double baseAmount);
        double GetSourceIdleCps(string sourceKey);
        double PreviewIdleCps(IIdlePlan plan);
    }
}
