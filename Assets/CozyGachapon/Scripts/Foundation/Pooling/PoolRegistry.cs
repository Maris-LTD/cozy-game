using System;
using System.Collections.Generic;

namespace CozyGachapon.Pooling
{
    public class PoolRegistry
    {
        private readonly Dictionary<string, PoolRecord> _records = new Dictionary<string, PoolRecord>(StringComparer.Ordinal);
        private readonly List<PoolRecord> _ordered = new List<PoolRecord>();

        public int TotalCapacity { get; private set; }
        public int Count => _ordered.Count;

        public bool Contains(string id)
        {
            return id != null && _records.ContainsKey(id);
        }

        public void Add(PoolRecord record)
        {
            _records.Add(record.Id, record);
            _ordered.Add(record);
            TotalCapacity += record.Capacity;
        }

        public PoolRecord GetAt(int index)
        {
            return _ordered[index];
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
