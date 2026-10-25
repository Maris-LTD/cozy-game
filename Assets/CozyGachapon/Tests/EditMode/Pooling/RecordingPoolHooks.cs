using CozyGachapon.Contracts;
using UnityEngine;

namespace CozyGachapon.Tests.Pooling
{
    public class RecordingPoolHooks : IPoolHooks
    {
        public int AcquireCount { get; private set; }
        public int ReturnCount { get; private set; }
        public PoolReturnReason LastReason { get; private set; }
        public GameObject LastAcquired { get; private set; }
        public GameObject LastReturned { get; private set; }
        public bool WasActiveOnAcquire { get; private set; }

        public void OnAcquire(GameObject instance)
        {
            AcquireCount++;
            LastAcquired = instance;
            WasActiveOnAcquire = instance.activeInHierarchy;
        }

        public void OnReturn(GameObject instance, PoolReturnReason reason)
        {
            ReturnCount++;
            LastReturned = instance;
            LastReason = reason;
        }
    }
}
