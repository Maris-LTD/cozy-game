using CozyGachapon.Contracts;
using UnityEngine;

namespace CozyGachapon.Pooling
{
    public class PoolRecord
    {
        private readonly GameObject[] _free;

        public string Id { get; }
        public PoolDefinition Definition { get; }
        public int Capacity { get; }
        public int Created { get; private set; }
        public int FreeCount { get; private set; }

        public PoolRecord(string id, PoolDefinition definition, int capacity)
        {
            Id = id;
            Definition = definition;
            Capacity = capacity;
            Created = 0;
            _free = new GameObject[capacity];
        }

        public bool AddPrewarmed(GameObject instance)
        {
            if (Created >= Capacity)
            {
                return false;
            }

            _free[FreeCount] = instance;
            FreeCount++;
            Created++;
            return true;
        }

        public GameObject GetFreeAt(int index)
        {
            return _free[index];
        }

        public PoolStats ToStats()
        {
            return new PoolStats(Capacity, Created, 0, 0, 0, 0, 0, 0, 0, 0);
        }
    }
}
