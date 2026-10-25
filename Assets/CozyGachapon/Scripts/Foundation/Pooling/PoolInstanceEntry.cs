namespace CozyGachapon.Pooling
{
    public readonly struct PoolInstanceEntry
    {
        public PoolRecord Record { get; }
        public int Slot { get; }

        public PoolInstanceEntry(PoolRecord record, int slot)
        {
            Record = record;
            Slot = slot;
        }
    }
}
