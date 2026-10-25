using System;
using UnityEngine.Profiling;

namespace CozyGachapon.Tests.Pooling
{
    public static class GcAllocProbe
    {
        private const string MarkerName = "GC.Alloc";

        public static int CountAllocations(Action body)
        {
            Recorder recorder = Recorder.Get(MarkerName);
            recorder.enabled = false;
            recorder.FilterToCurrentThread();
            recorder.enabled = true;
            try
            {
                body();
            }
            finally
            {
                recorder.enabled = false;
                recorder.CollectFromAllThreads();
            }

            return recorder.sampleBlockCount;
        }
    }
}
