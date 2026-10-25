using UnityEngine;

namespace CozyGachapon.Audio
{
    public static class AudioMath
    {
        private const float DecibelsPerAmplitudeDecade = 20f;
        private const float SemitonesPerOctave = 12f;
        private const float OctaveRatio = 2f;

        public static float DbToLinear(float db, float silenceFloorDb)
        {
            if (db <= silenceFloorDb)
            {
                return 0f;
            }

            return Mathf.Pow(10f, db / DecibelsPerAmplitudeDecade);
        }

        public static float SemitoneRatio(int shift)
        {
            return Mathf.Pow(OctaveRatio, shift / SemitonesPerOctave);
        }

        public static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
