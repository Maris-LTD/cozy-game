using System.Collections.Generic;
using CozyGachapon.Contracts;
using UnityEngine;

namespace CozyGachapon.Pooling
{
    public static class PoolingConfigValidator
    {
        public static void Validate(PoolingConfig config, List<PoolConfigIssue> results)
        {
            if (config.PrewarmPerFrame < 1)
            {
                results.Add(new PoolConfigIssue(PoolDiagnosticCode.PrewarmPerFrameInvalid, null, true));
            }

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

        public static void ValidatePrewarmBudget(PoolingConfig config, int totalCapacity, int targetFps, List<PoolConfigIssue> results)
        {
            if (PrewarmBudget.ExceedsBudget(totalCapacity, config.PrewarmPerFrame, targetFps, config.PrewarmMaxSeconds))
            {
                results.Add(new PoolConfigIssue(PoolDiagnosticCode.PrewarmBudgetExceeded, null, false));
            }
        }
    }
}
