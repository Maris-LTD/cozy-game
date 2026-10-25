using System.Collections.Generic;

namespace CozyGachapon.Pooling
{
    public sealed class PoolInstanceTable
    {
        private readonly Dictionary<int, PoolInstanceEntry> _entries;

        public PoolInstanceTable(int capacity)
        {
            _entries = new Dictionary<int, PoolInstanceEntry>(capacity);
        }

        public void Add(int instanceId, PoolRecord record, int slot)
        {
            _entries[instanceId] = new PoolInstanceEntry(record, slot);
        }

        public bool TryResolve(int instanceId, out PoolInstanceEntry entry)
        {
            return _entries.TryGetValue(instanceId, out entry);
        }
    }
}
