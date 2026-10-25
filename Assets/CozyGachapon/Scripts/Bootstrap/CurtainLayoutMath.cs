using UnityEngine;

namespace CozyGachapon
{
    public static class CurtainLayoutMath
    {
        public static Rect ToCanvasRect(float frameX, float frameY, float frameWidth, float frameHeight, float scaleFactor)
        {
            float scale = scaleFactor > 0f ? scaleFactor : 1f;
            return new Rect(frameX / scale, frameY / scale, frameWidth / scale, frameHeight / scale);
        }

        public static float RevealOffset(float progress, float canvasHeight)
        {
            float clamped = Mathf.Clamp01(progress);
            float inverse = 1f - clamped;
            float eased = 1f - inverse * inverse;
            return eased * Mathf.Max(0f, canvasHeight);
        }

        public static float FadeAlpha(float elapsedSeconds, float durationSeconds)
        {
            if (durationSeconds <= 0f)
            {
                return 1f;
            }

            return Mathf.Clamp01(elapsedSeconds / durationSeconds);
        }
    }
}
