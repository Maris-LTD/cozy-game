namespace CozyGachapon.Contracts
{
    public interface IIncomePipeline
    {
        bool RegisterIncomeSource(string sourceKey, IncomeChannel channel, double baseRate);
        bool RegisterModifier(in IncomeModifier modifier);
        int Unregister(string sourceId);
        double GetSynergyMultiplier(string sourceKey);
        double GetTierMultiplier(ModifierTier tier, IncomeChannel channel);
        double GetFlatUnits(string sourceKey);
        double GetChannelIncome(string sourceKey, double flatUnitValue);
    }
}
