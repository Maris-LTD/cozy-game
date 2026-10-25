using CozyGachapon.Audio;
using CozyGachapon.Tests.Pooling;
using NUnit.Framework;

namespace CozyGachapon.Tests.Audio
{
    public class AudioFadeTests : AudioTestFixture
    {
        private const double StartTime = 10.0;
        private const double Tolerance = 1e-4;

        [Test]
        public void DefaultBusTransitionSecondsIsPositive()
        {
            Assert.Greater(Config.BusTransitionSeconds, 0f);
        }

        [Test]
        public void ZeroDurationFactorIsOneAtEverySample()
        {
            double[] samples = { StartTime, StartTime + 0.25, StartTime + 1.0, StartTime + 2.0 };

            for (int i = 0; i < samples.Length; i++)
            {
                float factor = AudioFade.Factor(samples[i], StartTime, 0f);
                Assert.AreEqual(1f, factor, Tolerance);
                Assert.IsFalse(float.IsNaN(factor) || float.IsInfinity(factor));
            }
        }

        [Test]
        public void ZeroDurationFadeReturnsTargetAtEverySample()
        {
            AudioFade fade = default;
            fade.StartFade(0f, 1f, 0f, StartTime);

            double[] samples = { StartTime, StartTime + 0.25, StartTime + 1.0, StartTime + 2.0 };
            for (int i = 0; i < samples.Length; i++)
            {
                float value = fade.Evaluate(samples[i]);
                Assert.AreEqual(1f, value, Tolerance);
                Assert.IsFalse(float.IsNaN(value) || float.IsInfinity(value));
            }
        }

        [Test]
        public void PositiveDurationFactorSamplesZeroQuarterOneOne()
        {
            float t = Config.BusTransitionSeconds;

            Assert.AreEqual(0f, AudioFade.Factor(StartTime, StartTime, t), Tolerance);
            Assert.AreEqual(0.25f, AudioFade.Factor(StartTime + t / 4.0, StartTime, t), Tolerance);
            Assert.AreEqual(1f, AudioFade.Factor(StartTime + t, StartTime, t), Tolerance);
            Assert.AreEqual(1f, AudioFade.Factor(StartTime + 2.0 * t, StartTime, t), Tolerance);
        }

        [Test]
        public void PositiveDurationFadeSamplesZeroQuarterOneOne()
        {
            float t = Config.BusTransitionSeconds;
            AudioFade fade = default;
            fade.StartFade(0f, 1f, t, StartTime);

            Assert.AreEqual(0f, fade.Evaluate(StartTime), Tolerance);
            Assert.AreEqual(0.25f, fade.Evaluate(StartTime + t / 4.0), Tolerance);
            Assert.AreEqual(1f, fade.Evaluate(StartTime + t), Tolerance);
            Assert.AreEqual(1f, fade.Evaluate(StartTime + 2.0 * t), Tolerance);
        }

        [Test]
        public void RetargetAtQuarterStartsFromQuarterAndDescendsToZero()
        {
            float t = Config.BusTransitionSeconds;
            double now = StartTime + t / 4.0;
            AudioFade fade = default;
            fade.StartFade(0f, 1f, t, StartTime);

            fade.Retarget(0f, t, now);

            Assert.AreEqual(0.25f, fade.Evaluate(now), Tolerance);
            float previous = fade.Evaluate(now);
            for (int i = 1; i < 8; i++)
            {
                float value = fade.Evaluate(now + t * i / 8.0);
                Assert.Less(value, previous);
                previous = value;
            }

            Assert.AreEqual(0f, fade.Evaluate(now + t), Tolerance);
        }

        [Test]
        public void NegativeDurationBehavesAsZero()
        {
            Assert.AreEqual(1f, AudioFade.Factor(StartTime, StartTime, -0.5f), Tolerance);

            AudioFade fade = default;
            fade.StartFade(0.2f, 0.7f, -0.5f, StartTime);

            Assert.AreEqual(0.7f, fade.Evaluate(StartTime), Tolerance);
            Assert.AreEqual(0.7f, fade.Evaluate(StartTime + 1.0), Tolerance);
        }

        [Test]
        public void RepeatedRetargetNeverJumpsAndStaysInUnitRange()
        {
            float t = Config.BusTransitionSeconds;
            double step = t / 8.0;
            double now = StartTime;
            AudioFade fade = default;
            fade.StartFade(0f, 1f, t, now);

            for (int i = 0; i < 6; i++)
            {
                now += step;
                float before = fade.Evaluate(now);
                fade.Retarget(i % 2 == 0 ? 0f : 1f, t, now);
                float after = fade.Evaluate(now);

                Assert.AreEqual(before, after, Tolerance);
                Assert.GreaterOrEqual(after, 0f);
                Assert.LessOrEqual(after, 1f);
            }
        }

        [Test]
        public void EvaluateBeforeStartReturnsFromValue()
        {
            AudioFade fade = default;
            fade.StartFade(0.3f, 1f, 0.5f, StartTime);

            Assert.AreEqual(0.3f, fade.Evaluate(StartTime - 1.0), Tolerance);
        }

        [Test]
        public void DefaultFadeEvaluatesToZero()
        {
            AudioFade fade = default;

            Assert.AreEqual(0f, fade.Evaluate(StartTime), Tolerance);
        }

        [Test]
        public void SnapHoldsValueAtAnyTime()
        {
            AudioFade fade = default;
            fade.Snap(0.6f);

            Assert.AreEqual(0.6f, fade.Evaluate(0.0), Tolerance);
            Assert.AreEqual(0.6f, fade.Evaluate(StartTime), Tolerance);
        }

        [Test]
        public void EvaluateAndRetargetDoNotAllocate()
        {
            AudioFade fade = default;
            fade.StartFade(0f, 1f, 0.2f, StartTime);

            int allocations = GcAllocProbe.CountAllocations(() =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    fade.Evaluate(StartTime + i * 0.001);
                    fade.Retarget(i % 2, 0.2f, StartTime + i * 0.001);
                }
            });

            Assert.AreEqual(0, allocations);
        }
    }
}
