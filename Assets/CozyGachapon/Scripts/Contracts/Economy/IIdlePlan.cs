namespace CozyGachapon.Contracts
{
    public interface IIdlePlan
    {
        int SourceCount { get; }
        IdlePlanSource GetSource(int index);
        int ModifierCount { get; }
        IncomeModifier GetModifier(int index);
    }
}
