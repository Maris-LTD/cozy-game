namespace CozyGachapon.Pooling
{
    public sealed class PoolCounters
    {
        public int InUse { get; private set; }
        public int PeakInUse { get; private set; }
        public int DoubleRelease { get; private set; }
        public int StaleRelease { get; private set; }
        public int ForeignRelease { get; private set; }

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
    }
}
