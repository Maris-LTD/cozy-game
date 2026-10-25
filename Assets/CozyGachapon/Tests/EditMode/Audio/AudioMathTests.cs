using CozyGachapon.Audio;
using NUnit.Framework;

namespace CozyGachapon.Tests.Audio
{
    public class AudioMathTests : AudioTestFixture
    {
        private const float Tolerance = 1e-4f;

        [Test]
        public void DefaultSilenceFloorIsMinusEighty()
        {
            Assert.AreEqual(-80f, Config.SilenceFloorDb, Tolerance);
        }

        [TestCase(0f, 1f)]
        [TestCase(-6f, 0.5012f)]
        [TestCase(-12f, 0.2512f)]
        public void DbToLinearAboveFloorMatchesPowerLaw(float db, float expected)
        {
            Assert.AreEqual(expected, AudioMath.DbToLinear(db, Config.SilenceFloorDb), Tolerance);
        }

        [Test]
        public void DbToLinearAtSilenceFloorIsZero()
        {
            Assert.AreEqual(0f, AudioMath.DbToLinear(Config.SilenceFloorDb, Config.SilenceFloorDb), Tolerance);
        }

        [Test]
        public void DbToLinearTwentyBelowSilenceFloorIsZero()
        {
            Assert.AreEqual(0f, AudioMath.DbToLinear(Config.SilenceFloorDb - 20f, Config.SilenceFloorDb), Tolerance);
        }

        [Test]
        public void DbToLinearPositiveDbIsNotClamped()
        {
            float linear = AudioMath.DbToLinear(6f, Config.SilenceFloorDb);

            Assert.AreEqual(1.9953f, linear, Tolerance);
            Assert.Greater(linear, 1f);
        }

        [TestCase(-100f)]
        [TestCase(-80f)]
        [TestCase(-60f)]
        public void DbToLinearThresholdFollowsConfiguredFloor(float floorDb)
        {
            Config.SetSilenceFloorDbForTest(floorDb);

            Assert.AreEqual(0f, AudioMath.DbToLinear(floorDb, Config.SilenceFloorDb));
            Assert.AreEqual(0f, AudioMath.DbToLinear(floorDb - 1f, Config.SilenceFloorDb));
            Assert.Greater(AudioMath.DbToLinear(floorDb + 0.01f, Config.SilenceFloorDb), 0f);
        }
    }
}
