using System;
using System.Collections.Generic;

namespace CozyGachapon.Audio
{
    public static class SamplePitchSelector
    {
        public static bool TrySelectSample(int semitone, IReadOnlyList<PitchedSample> samples, out int index, out int shift)
        {
            index = -1;
            shift = 0;
            int bestDistance = int.MaxValue;
            int bestNative = 0;
            for (int i = 0; i < samples.Count; i++)
            {
                PitchedSample sample = samples[i];
                if (sample == null || sample.Clip == null)
                {
                    continue;
                }

                int distance = Math.Abs(semitone - sample.NativeSemitone);
                bool closer = distance < bestDistance;
                bool tieWithLowerNative = distance == bestDistance && sample.NativeSemitone < bestNative;
                if (closer || tieWithLowerNative)
                {
                    bestDistance = distance;
                    bestNative = sample.NativeSemitone;
                    index = i;
                    shift = semitone - sample.NativeSemitone;
                }
            }

            return index >= 0;
        }
    }
}
