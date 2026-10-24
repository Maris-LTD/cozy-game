using CozyGachapon.Contracts;
using UnityEngine;

namespace CozyGachapon.Foundation
{
    internal static class FrameRectCalculator
    {
        public static FrameChangedEvent Compute(
            int screenWidth,
            int screenHeight,
            float minAspect,
            float maxAspect,
            float referenceWidth,
            float referenceHeight)
        {
            float width = screenWidth;
            float height = screenHeight;
            float aspect = width / height;
            float frameWidth = width;
            float frameHeight = height;

            if (aspect > maxAspect)
            {
                frameWidth = height * maxAspect;
            }
            else if (aspect < minAspect)
            {
                frameHeight = width / minAspect;
            }

            float frameX = (width - frameWidth) * 0.5f;
            float frameY = (height - frameHeight) * 0.5f;
            float frameScale = Mathf.Min(frameWidth / referenceWidth, frameHeight / referenceHeight);
            float extraHeightRef = frameHeight / frameScale - referenceHeight;

            return new FrameChangedEvent(frameX, frameY, frameWidth, frameHeight, frameScale, extraHeightRef);
        }
    }
}
