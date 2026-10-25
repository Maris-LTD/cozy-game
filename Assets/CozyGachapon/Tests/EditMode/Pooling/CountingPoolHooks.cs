using CozyGachapon.Contracts;
using UnityEngine;

namespace CozyGachapon.Tests.Pooling
{
    public sealed class CountingPoolHooks : IPoolHooks
    {
        public int AcquireCount { get; private set; }
        public int ReturnCount { get; private set; }

        public void OnAcquire(GameObject instance)
        {
            AcquireCount++;
        }

        public void OnReturn(GameObject instance, PoolReturnReason reason)
        {
            ReturnCount++;
        }
    }
}
