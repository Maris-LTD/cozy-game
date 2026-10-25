using CozyGachapon.Contracts;
using UnityEngine;

namespace CozyGachapon.Pooling
{
    public class PoolRecord
    {
        private readonly GameObject[] _instances;
        private readonly int[] _instanceIds;
        private readonly long[] _sequences;
        private readonly bool[] _inUse;
        private readonly int[] _freeSlots;

        public string Id { get; }
        public PoolDefinition Definition { get; }
        public int Capacity { get; }
        public int Created { get; private set; }
        public int FreeCount { get; private set; }
        public long AcquireSeq { get; private set; }
        public IPoolHooks Hooks { get; }
        public PoolCounters Counters { get; }

        public PoolRecord(string id, PoolDefinition definition, int capacity, IPoolHooks hooks)
        {
            Id = id;
            Definition = definition;
            Capacity = capacity;
            Hooks = hooks;
            Created = 0;
            Counters = new PoolCounters();
            _instances = new GameObject[capacity];
            _instanceIds = new int[capacity];
            _sequences = new long[capacity];
            _inUse = new bool[capacity];
            _freeSlots = new int[capacity];
        }

        public int AddPrewarmed(GameObject instance, int instanceId)
        {
            if (Created >= Capacity)
            {
                return -1;
            }

            int slot = Created;
            _instances[slot] = instance;
            _instanceIds[slot] = instanceId;
            Created++;
            PushFree(slot);
            return slot;
        }

        public bool TryPopFree(out int slot)
        {
            if (FreeCount == 0)
            {
                slot = -1;
                return false;
            }

            FreeCount--;
            slot = _freeSlots[FreeCount];
            return true;
        }

        public void PushFree(int slot)
        {
            _freeSlots[FreeCount] = slot;
            FreeCount++;
        }

        public GameObject GetFreeAt(int index)
        {
            return _instances[_freeSlots[index]];
        }

        public GameObject GetInstance(int slot)
        {
            return _instances[slot];
        }

        public int GetInstanceId(int slot)
        {
            return _instanceIds[slot];
        }

        public long GetSequence(int slot)
        {
            return _sequences[slot];
        }

        public bool IsInUse(int slot)
        {
            return _inUse[slot];
        }

        public long MarkAcquired(int slot)
        {
            AcquireSeq++;
            _sequences[slot] = AcquireSeq;
            _inUse[slot] = true;
            return AcquireSeq;
        }

        public void MarkReleased(int slot)
        {
            _inUse[slot] = false;
        }

        public PoolStats ToStats()
        {
            return new PoolStats(
                Capacity,
                Created,
                Counters.InUse,
                Counters.PeakInUse,
                0,
                0,
                Counters.DoubleRelease,
                Counters.StaleRelease,
                Counters.ForeignRelease,
                0);
        }
    }
}
