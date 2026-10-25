using CozyGachapon.Contracts;
using UnityEngine;

namespace CozyGachapon.Tests.Pooling
{
    public readonly struct RecordedHookCall
    {
        public bool IsReturn { get; }
        public GameObject Instance { get; }
        public PoolReturnReason Reason { get; }

        public RecordedHookCall(bool isReturn, GameObject instance, PoolReturnReason reason)
        {
            IsReturn = isReturn;
            Instance = instance;
            Reason = reason;
        }
    }
}
