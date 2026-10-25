using CozyGachapon.Audio;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Audio
{
    public class UnityAudioClockTests
    {
        private const double Tolerance = 0.5;

        private float _timeScale;

        [SetUp]
        public void SetUp()
        {
            _timeScale = Time.timeScale;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = _timeScale;
        }

        [Test]
        public void Now_IsNonNegative_AndMonotonic()
        {
            UnityAudioClock clock = new UnityAudioClock();

            double first = clock.Now;
            double second = clock.Now;

            Assert.GreaterOrEqual(first, 0.0);
            Assert.GreaterOrEqual(second, first);
        }

        [Test]
        public void Now_TracksRealtimeSinceStartup()
        {
            UnityAudioClock clock = new UnityAudioClock();

            Assert.AreEqual(Time.realtimeSinceStartupAsDouble, clock.Now, Tolerance);
        }

        [Test]
        public void Now_IgnoresTimeScale()
        {
            UnityAudioClock clock = new UnityAudioClock();
            Time.timeScale = 0f;

            double before = clock.Now;
            double realtime = Time.realtimeSinceStartupAsDouble;

            Assert.AreEqual(realtime, before, Tolerance);
            Assert.GreaterOrEqual(clock.Now, before);
        }
    }
}
