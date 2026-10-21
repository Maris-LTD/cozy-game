using System;
using System.Collections.Generic;
using CozyGachapon.Contracts;

namespace CozyGachapon.Foundation
{
    public static class BootStepOrderResolver
    {
        public static bool TryBuildExecutionPlan(
            IReadOnlyList<IBootStep> registeredSteps,
            out List<IBootStep> executionPlan,
            out string errorStepId,
            out BootPhase errorPhase,
            out string errorMessage)
        {
            executionPlan = new List<IBootStep>();
            errorStepId = string.Empty;
            errorPhase = BootPhase.ServiceInit;
            errorMessage = string.Empty;

            if (registeredSteps == null)
            {
                errorMessage = "Registered steps collection cannot be null";
                return false;
            }

            Array phases = Enum.GetValues(typeof(BootPhase));
            for (int i = 0; i < phases.Length; i++)
            {
                BootPhase phase = (BootPhase)phases.GetValue(i);
                List<IBootStep> phaseSteps = new List<IBootStep>();

                for (int j = 0; j < registeredSteps.Count; j++)
                {
                    if (registeredSteps[j].Phase == phase)
                    {
                        phaseSteps.Add(registeredSteps[j]);
                    }
                }

                if (phaseSteps.Count == 0)
                {
                    continue;
                }

                if (!SortPhaseSteps(phaseSteps, out List<IBootStep> sortedSteps, out errorStepId))
                {
                    errorPhase = phase;
                    errorMessage = "Invalid or cyclic dependency in phase " + phase;
                    return false;
                }

                executionPlan.AddRange(sortedSteps);
            }

            return true;
        }

        private static bool SortPhaseSteps(List<IBootStep> phaseSteps, out List<IBootStep> sorted, out string errorStepId)
        {
            sorted = new List<IBootStep>(phaseSteps.Count);
            errorStepId = string.Empty;
            Dictionary<string, IBootStep> stepMap = new Dictionary<string, IBootStep>(StringComparer.Ordinal);
            Dictionary<string, int> inDegrees = new Dictionary<string, int>(StringComparer.Ordinal);
            Dictionary<string, List<string>> dependents = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            for (int i = 0; i < phaseSteps.Count; i++)
            {
                IBootStep step = phaseSteps[i];
                stepMap[step.Id] = step;
                inDegrees[step.Id] = 0;
                dependents[step.Id] = new List<string>();
            }

            for (int i = 0; i < phaseSteps.Count; i++)
            {
                IBootStep step = phaseSteps[i];
                IReadOnlyList<string> runsAfter = step.RunsAfter;
                if (runsAfter == null)
                {
                    continue;
                }

                HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
                for (int j = 0; j < runsAfter.Count; j++)
                {
                    string depId = runsAfter[j];
                    if (string.IsNullOrEmpty(depId) || !stepMap.ContainsKey(depId))
                    {
                        errorStepId = step.Id;
                        return false;
                    }

                    if (seen.Add(depId))
                    {
                        inDegrees[step.Id]++;
                        dependents[depId].Add(step.Id);
                    }
                }
            }

            List<string> ready = new List<string>();
            foreach (KeyValuePair<string, int> pair in inDegrees)
            {
                if (pair.Value == 0)
                {
                    ready.Add(pair.Key);
                }
            }

            ready.Sort(StringComparer.Ordinal);

            while (ready.Count > 0)
            {
                string nextId = ready[0];
                ready.RemoveAt(0);
                sorted.Add(stepMap[nextId]);

                List<string> deps = dependents[nextId];
                for (int i = 0; i < deps.Count; i++)
                {
                    string dep = deps[i];
                    inDegrees[dep]--;
                    if (inDegrees[dep] == 0)
                    {
                        ready.Add(dep);
                        ready.Sort(StringComparer.Ordinal);
                    }
                }
            }

            if (sorted.Count != phaseSteps.Count)
            {
                errorStepId = string.Empty;
                return false;
            }

            return true;
        }
    }
}
