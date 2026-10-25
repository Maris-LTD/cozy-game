using UnityEngine;

namespace CozyGachapon.Contracts
{
    public interface IPoolHooks
    {
        void OnAcquire(GameObject instance);
        void OnReturn(GameObject instance, PoolReturnReason reason);
    }
}
