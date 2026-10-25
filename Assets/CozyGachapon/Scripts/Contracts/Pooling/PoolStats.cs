namespace CozyGachapon.Contracts
{
    public readonly struct PoolStats
    {
        public int Capacity { get; }
        public int Created { get; }
        public int InUse { get; }
        public int PeakInUse { get; }
        public int Exhausted { get; }
        public int Recycled { get; }
        public int DoubleRelease { get; }
        public int StaleRelease { get; }
        public int ForeignRelease { get; }
        public int Lost { get; }

        public PoolStats(
            int capacity,
            int created,
            int inUse,
            int peakInUse,
            int exhausted,
            int recycled,
            int doubleRelease,
            int staleRelease,
            int foreignRelease,
            int lost)
        {
            Capacity = capacity;
            Created = created;
            InUse = inUse;
            PeakInUse = peakInUse;
            Exhausted = exhausted;
            Recycled = recycled;
            DoubleRelease = doubleRelease;
            StaleRelease = staleRelease;
            ForeignRelease = foreignRelease;
            Lost = lost;
        }
    }
}
