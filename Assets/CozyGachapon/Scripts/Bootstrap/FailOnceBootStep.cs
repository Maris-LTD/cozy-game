#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using CozyGachapon.Contracts;
using UnityEngine;

namespace CozyGachapon
{
    public sealed class FailOnceBootStep : IBootStep
    {
        public const string StepId = "debug.failOnce";

        private static bool _hasFailedOnce;

        public string Id => StepId;
        public BootPhase Phase => BootPhase.DataLoad;
        public IReadOnlyList<string> RunsAfter => Array.Empty<string>();
        public bool IsCritical => true;
        public bool IsDone { get; private set; }
        public bool IsFailed { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            _hasFailedOnce = false;
        }

        public void Run()
        {
            if (_hasFailedOnce)
            {
                IsDone = true;
                return;
            }

            _hasFailedOnce = true;
            IsFailed = true;
        }
    }
}
#endif
