using CozyGachapon.Contracts;
using UnityEngine;

namespace CozyGachapon.Tests.Pooling
{
    public sealed class AllocatingPoolHooks : IPoolHooks
    {
        private const int BufferLength = 16;

        public object[] Last { get; private set; }

        public void OnAcquire(GameObject instance)
        {
            Last = new object[BufferLength];
        }

        public void OnReturn(GameObject instance, PoolReturnReason reason)
        {
        }
    }
}
