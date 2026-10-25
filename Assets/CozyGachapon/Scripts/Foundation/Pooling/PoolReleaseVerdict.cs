namespace CozyGachapon.Pooling
{
    public enum PoolReleaseVerdict
    {
        Valid = 0,
        Foreign = 1,
        Double = 2,
        Stale = 3
    }
}
