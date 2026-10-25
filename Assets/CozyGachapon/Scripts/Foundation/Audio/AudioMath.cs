using UnityEngine;

namespace CozyGachapon.Audio
{
    public static class AudioMath
    {
        private const float DecibelsPerAmplitudeDecade = 20f;

        public static float DbToLinear(float db, float silenceFloorDb)
        {
            if (db <= silenceFloorDb)
            {
                return 0f;
            }

            return Mathf.Pow(10f, db / DecibelsPerAmplitudeDecade);
        }
    }
}
