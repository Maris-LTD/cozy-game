using CozyGachapon.Audio;
using NUnit.Framework;

namespace CozyGachapon.Tests.Audio
{
    public class SliderMathTests
    {
        private const float MinDb = -40f;
        private const float DefaultSlider = 1f;
        private const float Tolerance = 0.0001f;

        [TestCase(1f, 0f)]
        [TestCase(0.75f, -10f)]
        [TestCase(0.5f, -20f)]
        [TestCase(1.5f, 0f)]
        [TestCase(float.PositiveInfinity, 0f)]
        public void AudibleSliderMapsLinearlyToDb(float slider, float expectedDb)
        {
            bool audible = SliderMath.TryToDb(slider, MinDb, DefaultSlider, out float userDb);

            Assert.IsTrue(audible);
            Assert.AreEqual(expectedDb, userDb, Tolerance);
        }

        [TestCase(0f)]
        [TestCase(-0.5f)]
        [TestCase(-1f)]
        [TestCase(float.NegativeInfinity)]
        public void ZeroAndNegativeSliderSilencesTheBus(float slider)
        {
            Assert.IsFalse(SliderMath.TryToDb(slider, MinDb, DefaultSlider, out _));
        }

        [Test]
        public void NaNUsesTheDefaultSlider()
        {
            Assert.IsTrue(SliderMath.TryToDb(float.NaN, MinDb, 0.75f, out float userDb));
            Assert.AreEqual(-10f, userDb, Tolerance);

            Assert.IsTrue(SliderMath.TryToDb(float.NaN, MinDb, DefaultSlider, out userDb));
            Assert.AreEqual(0f, userDb, Tolerance);
        }

        [Test]
        public void NaNWithZeroDefaultSilencesTheBus()
        {
            Assert.IsFalse(SliderMath.TryToDb(float.NaN, MinDb, 0f, out _));
        }
    }
}
