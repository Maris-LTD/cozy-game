using System;
using System.Collections.Generic;

namespace CozyGachapon.Pooling
{
    public class PoolRegistry
    {
        private readonly Dictionary<string, PoolRecord> _records = new Dictionary<string, PoolRecord>(StringComparer.Ordinal);

        public int TotalCapacity { get; private set; }

        public bool Contains(string id)
        {
            return id != null && _records.ContainsKey(id);
        }

        public void Add(PoolRecord record)
        {
            _records.Add(record.Id, record);
            TotalCapacity += record.Capacity;
        }

        public bool TryGet(string id, out PoolRecord record)
        {
            if (id == null)
            {
                record = null;
                return false;
            }

            return _records.TryGetValue(id, out record);
        }
    }
}
