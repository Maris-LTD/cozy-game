using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Bootstrap
{
    public class CurtainLayoutMathTests
    {
        private const float Tight = 0.01f;

        [Test]
        public void ToCanvasRect_PortraitScaleOne_ReturnsFramePixels()
        {
            Rect rect = CurtainLayoutMath.ToCanvasRect(0f, 0f, 1080f, 1920f, 1f);

            Assert.AreEqual(0f, rect.x, Tight);
            Assert.AreEqual(0f, rect.y, Tight);
            Assert.AreEqual(1080f, rect.width, Tight);
            Assert.AreEqual(1920f, rect.height, Tight);
        }

        [Test]
        public void ToCanvasRect_LandscapeScaleHeightMatch_ReturnsReferenceSizedFrame()
        {
            Rect rect = CurtainLayoutMath.ToCanvasRect(656.25f, 0f, 607.5f, 1080f, 0.5625f);

            Assert.AreEqual(1166.6667f, rect.x, Tight);
            Assert.AreEqual(0f, rect.y, Tight);
            Assert.AreEqual(1080f, rect.width, Tight);
            Assert.AreEqual(1920f, rect.height, Tight);
        }

        [Test]
        public void ToCanvasRect_NonPositiveScale_TreatsAsOne()
        {
            Rect zero = CurtainLayoutMath.ToCanvasRect(10f, 20f, 300f, 400f, 0f);
            Rect negative = CurtainLayoutMath.ToCanvasRect(10f, 20f, 300f, 400f, -2f);

            Assert.AreEqual(300f, zero.width, Tight);
            Assert.AreEqual(400f, negative.height, Tight);
            Assert.AreEqual(10f, negative.x, Tight);
        }

        [Test]
        public void RevealOffset_ProgressZeroAndOne_ReturnsZeroAndHeight()
        {
            Assert.AreEqual(0f, CurtainLayoutMath.RevealOffset(0f, 1920f), Tight);
            Assert.AreEqual(1920f, CurtainLayoutMath.RevealOffset(1f, 1920f), Tight);
        }

        [Test]
        public void RevealOffset_ProgressHalf_IsEasedAboveLinear()
        {
            Assert.AreEqual(1440f, CurtainLayoutMath.RevealOffset(0.5f, 1920f), Tight);
        }

        [Test]
        public void RevealOffset_ProgressOutOfRange_IsClamped()
        {
            Assert.AreEqual(0f, CurtainLayoutMath.RevealOffset(-1f, 1920f), Tight);
            Assert.AreEqual(1920f, CurtainLayoutMath.RevealOffset(3f, 1920f), Tight);
        }

        [Test]
        public void FadeAlpha_HalfDuration_ReturnsHalf()
        {
            Assert.AreEqual(0.5f, CurtainLayoutMath.FadeAlpha(0.1f, 0.2f), Tight);
        }

        [Test]
        public void FadeAlpha_NegativeOrBeyondDuration_IsClamped()
        {
            Assert.AreEqual(0f, CurtainLayoutMath.FadeAlpha(-1f, 0.2f), Tight);
            Assert.AreEqual(1f, CurtainLayoutMath.FadeAlpha(5f, 0.2f), Tight);
        }

        [Test]
        public void FadeAlpha_ZeroDuration_ReturnsOne()
        {
            Assert.AreEqual(1f, CurtainLayoutMath.FadeAlpha(0f, 0f), Tight);
        }
    }
}
