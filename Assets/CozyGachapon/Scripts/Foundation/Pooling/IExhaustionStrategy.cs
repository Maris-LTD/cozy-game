namespace CozyGachapon.Pooling
{
    public interface IExhaustionStrategy
    {
        bool TryReclaim(PoolRecord record, out int slot);
    }
}
