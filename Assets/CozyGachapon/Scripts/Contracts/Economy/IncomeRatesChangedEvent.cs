namespace CozyGachapon.Contracts
{
    public readonly struct IncomeRatesChangedEvent
    {
        public double IdleCps { get; }
        public double PlinkoRateAvg { get; }

        public IncomeRatesChangedEvent(double idleCps, double plinkoRateAvg)
        {
            IdleCps = idleCps;
            PlinkoRateAvg = plinkoRateAvg;
        }
    }
}
