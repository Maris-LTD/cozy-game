using System;
using UnityEngine;

namespace CozyGachapon.Pooling
{
    public sealed class PoolPrewarmer
    {
        private readonly PoolRegistry _registry;
        private readonly Transform[] _containers;
        private readonly PoolInstanceTable _table;
        private readonly int _perFrame;
        private int _poolIndex;

        public bool IsComplete { get; private set; }
        public bool HasFailed { get; private set; }
        public int LastStepCreated { get; private set; }

        public PoolPrewarmer(PoolRegistry registry, PoolInstanceTable table, PoolContainer factory, Transform root, int perFrame)
        {
            _registry = registry;
            _table = table;
            _perFrame = PrewarmBudget.EffectivePerFrame(perFrame);
            _containers = new Transform[registry.Count];

            for (int i = 0; i < _containers.Length; i++)
            {
                _containers[i] = factory.Create(root, registry.GetAt(i).Id);
            }

            SkipFullPools();
        }

        public void Step()
        {
            LastStepCreated = 0;
            if (IsComplete || HasFailed)
            {
                return;
            }

            try
            {
                int budget = _perFrame;
                while (budget > 0 && !IsComplete)
                {
                    PoolRecord record = _registry.GetAt(_poolIndex);
                    GameObject instance = UnityEngine.Object.Instantiate(record.Definition.Prefab, _containers[_poolIndex], false);
                    instance.SetActive(false);
                    int instanceId = instance.GetInstanceID();
                    int slot = record.AddPrewarmed(instance, instanceId);
                    _table.Add(instanceId, record, slot);
                    LastStepCreated++;
                    budget--;
                    SkipFullPools();
                }
            }
            catch (Exception)
            {
                HasFailed = true;
            }
        }

        private void SkipFullPools()
        {
            while (_poolIndex < _registry.Count && _registry.GetAt(_poolIndex).Created >= _registry.GetAt(_poolIndex).Capacity)
            {
                _poolIndex++;
            }

            IsComplete = _poolIndex >= _registry.Count;
        }
    }
}
