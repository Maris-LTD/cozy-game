using CozyGachapon.Contracts;

namespace CozyGachapon.Pooling
{
    public class PoolRecord
    {
        public string Id { get; }
        public PoolDefinition Definition { get; }
        public int Capacity { get; }
        public int Created { get; private set; }

        public PoolRecord(string id, PoolDefinition definition, int capacity)
        {
            Id = id;
            Definition = definition;
            Capacity = capacity;
            Created = 0;
        }

        public PoolStats ToStats()
        {
            return new PoolStats(Capacity, Created, 0, 0, 0, 0, 0, 0, 0, 0);
        }
    }
}
