using UnityEngine;

namespace CozyGachapon.Contracts
{
    public readonly struct PoolLease
    {
        public int InstanceId { get; }
        public string PoolId { get; }
        public long Sequence { get; }
        public GameObject Instance { get; }
        public bool IsValid => Sequence > 0 && PoolId != null;

        public PoolLease(int instanceId, string poolId, long sequence, GameObject instance)
        {
            InstanceId = instanceId;
            PoolId = poolId;
            Sequence = sequence;
            Instance = instance;
        }
    }
}
