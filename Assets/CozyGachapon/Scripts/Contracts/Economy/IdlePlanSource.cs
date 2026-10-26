namespace CozyGachapon.Contracts
{
    public readonly struct IdlePlanSource
    {
        public string SourceKey { get; }
        public double BaseRate { get; }
        public double FlatUnits { get; }

        public IdlePlanSource(string sourceKey, double baseRate, double flatUnits = 0d)
        {
            SourceKey = sourceKey;
            BaseRate = baseRate;
            FlatUnits = flatUnits;
        }
    }
}
