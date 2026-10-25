using UnityEngine;

namespace CozyGachapon.Tests.Pooling
{
    public sealed class PoolingTestTargets : ScriptableObject
    {
        [SerializeField, Range(500, 10000)] private int gcTestCycles = 1000;

        public int GcTestCycles => gcTestCycles;
    }
}
