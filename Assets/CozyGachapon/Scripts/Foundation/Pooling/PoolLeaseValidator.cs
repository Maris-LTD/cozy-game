using CozyGachapon.Contracts;

namespace CozyGachapon.Pooling
{
    public static class PoolLeaseValidator
    {
        public static PoolReleaseVerdict Classify(PoolRecord target, PoolInstanceTable table, in PoolLease lease, out int slot)
        {
            slot = -1;
            if (!lease.IsValid || ReferenceEquals(lease.Instance, null) || table == null)
            {
                return PoolReleaseVerdict.Foreign;
            }

            if (!table.TryResolve(lease.InstanceId, out PoolInstanceEntry entry) || !ReferenceEquals(entry.Record, target))
            {
                return PoolReleaseVerdict.Foreign;
            }

            if (!ReferenceEquals(target.GetInstance(entry.Slot), lease.Instance))
            {
                return PoolReleaseVerdict.Foreign;
            }

            slot = entry.Slot;
            if (!target.IsInUse(slot))
            {
                return PoolReleaseVerdict.Double;
            }

            if (target.GetSequence(slot) != lease.Sequence)
            {
                return PoolReleaseVerdict.Stale;
            }

            return PoolReleaseVerdict.Valid;
        }
    }
}
