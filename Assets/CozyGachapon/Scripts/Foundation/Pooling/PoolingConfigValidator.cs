using System.Collections.Generic;
using CozyGachapon.Contracts;
using UnityEngine;

namespace CozyGachapon.Pooling
{
    public static class PoolingConfigValidator
    {
        public static void Validate(PoolingConfig config, List<PoolConfigIssue> results)
        {
            IReadOnlyList<PoolDefinition> pools = config.Pools;
            for (int i = 0; i < pools.Count; i++)
            {
                PoolDefinition definition = pools[i];
                if (definition == null || definition.Prefab == null)
                {
                    continue;
                }

                if (definition.Prefab.GetComponentInChildren<AudioSource>(true) != null)
                {
                    results.Add(new PoolConfigIssue(PoolDiagnosticCode.PrefabHasAudioSource, definition.Id, false));
                }
            }
        }
    }
}
