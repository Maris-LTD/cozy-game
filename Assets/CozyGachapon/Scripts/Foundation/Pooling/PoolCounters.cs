namespace CozyGachapon.Pooling
{
    public sealed class PoolCounters
    {
        public int InUse { get; private set; }
        public int PeakInUse { get; private set; }
        public int DoubleRelease { get; private set; }
        public int StaleRelease { get; private set; }
        public int ForeignRelease { get; private set; }
        public int Exhausted { get; private set; }
        public int Recycled { get; private set; }
        public int Lost { get; private set; }
        public bool HasWarnedExhausted { get; private set; }

        public void OnAcquired()
        {
            InUse++;
            if (InUse > PeakInUse)
            {
                PeakInUse = InUse;
            }
        }

        public void OnReleased()
        {
            InUse--;
        }

        public void AddDoubleRelease()
        {
            DoubleRelease++;
        }

        public void AddStaleRelease()
        {
            StaleRelease++;
        }

        public void AddForeignRelease()
        {
            ForeignRelease++;
        }

        public bool AddExhausted()
        {
            Exhausted++;
            if (HasWarnedExhausted)
            {
                return false;
            }

            HasWarnedExhausted = true;
            return true;
        }

        public void AddRecycled()
        {
            Recycled++;
        }

        public void AddLost()
        {
            Lost++;
        }
    }
}
