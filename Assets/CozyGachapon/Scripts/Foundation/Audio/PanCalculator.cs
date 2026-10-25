using UnityEngine;

namespace CozyGachapon.Audio
{
    public static class PanCalculator
    {
        private const float PanLimit = 1f;

        public static float Resolve(bool panFromPosition, float panPosition, float maxPan, bool supportsPan)
        {
            if (!panFromPosition || !supportsPan || !AudioMath.IsFinite(panPosition) || !AudioMath.IsFinite(maxPan))
            {
                return 0f;
            }

            float pan = maxPan * Mathf.Clamp(panPosition, -PanLimit, PanLimit);
            return Mathf.Clamp(pan, -PanLimit, PanLimit);
        }
    }
}
