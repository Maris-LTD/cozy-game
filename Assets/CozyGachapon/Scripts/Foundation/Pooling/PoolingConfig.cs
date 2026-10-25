using System;
using System.Collections.Generic;
using UnityEngine;

namespace CozyGachapon.Pooling
{
    [CreateAssetMenu(fileName = "PoolingConfig", menuName = "CozyGachapon/Pooling/PoolingConfig")]
    public class PoolingConfig : ScriptableObject
    {
        [SerializeField] private List<PoolDefinition> _pools = new List<PoolDefinition>();
        [SerializeField] private int _maxCapacityPerPool = 64;
        [SerializeField] private int _maxTotalPooledObjects = 128;
        [SerializeField] private int _prewarmPerFrame = 10;
        [SerializeField] private float _prewarmMaxSeconds = 0.25f;

        public IReadOnlyList<PoolDefinition> Pools => _pools;
        public int MaxCapacityPerPool => _maxCapacityPerPool;
        public int MaxTotalPooledObjects => _maxTotalPooledObjects;
        public int PrewarmPerFrame => _prewarmPerFrame;
        public float PrewarmMaxSeconds => _prewarmMaxSeconds;

        public bool TryGetDefinition(string id, out PoolDefinition definition)
        {
            for (int i = 0; i < _pools.Count; i++)
            {
                PoolDefinition candidate = _pools[i];
                if (candidate != null && string.Equals(candidate.Id, id, StringComparison.Ordinal))
                {
                    definition = candidate;
                    return true;
                }
            }

            definition = null;
            return false;
        }

        public void SetPoolsForTest(IEnumerable<PoolDefinition> pools)
        {
            _pools = new List<PoolDefinition>(pools);
        }

        public void SetPrewarmForTest(int perFrame, float maxSeconds)
        {
            _prewarmPerFrame = perFrame;
            _prewarmMaxSeconds = maxSeconds;
        }

        public void SetLimitsForTest(int maxCapacityPerPool, int maxTotalPooledObjects)
        {
            _maxCapacityPerPool = maxCapacityPerPool;
            _maxTotalPooledObjects = maxTotalPooledObjects;
        }
    }
}
