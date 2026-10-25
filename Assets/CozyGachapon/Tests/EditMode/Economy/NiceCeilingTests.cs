using CozyGachapon.Economy;
using NUnit.Framework;

namespace CozyGachapon.Tests.Economy
{
    public class NiceCeilingTests
    {
        private const double EpsilonTolerance = 1e-8d;
        private const int RandomSamples = 5000;
        private const int RandomSeed = 20261025;

        [TestCase(105d, 2, 110d)]
        [TestCase(1146.74d, 2, 1200d)]
        [TestCase(7.2d, 2, 8d)]
        [TestCase(100d, 2, 100d)]
        [TestCase(0d, 2, 0d)]
        [TestCase(-5d, 2, 0d)]
        [TestCase(13150.1d, 3, 13200d)]
        [TestCase(9.5d, 2, 10d)]
        [TestCase(1000d, 2, 1000d)]
        [TestCase(10000d, 2, 10000d)]
        [TestCase(1e6d, 2, 1e6d)]
        public void Apply_TableCases_ReturnNiceCeiling(double x, int digits, double expected)
        {
            Assert.AreEqual(expected, NiceCeiling.Apply(x, digits));
        }

        [Test]
        public void Apply_TinyExcessOverPowerOfTen_DoesNotJumpToNextStep()
        {
            Assert.AreEqual(100d, NiceCeiling.Apply(100d * (1d + 1e-12d), 2));
        }

        [Test]
        public void Apply_NaNAndNegativeInfinity_ReturnZero()
        {
            Assert.AreEqual(0d, NiceCeiling.Apply(double.NaN, 2));
            Assert.AreEqual(0d, NiceCeiling.Apply(double.NegativeInfinity, 2));
        }

        [Test]
        public void Apply_PositiveInfinity_ReturnsInfinity()
        {
            Assert.IsTrue(double.IsPositiveInfinity(NiceCeiling.Apply(double.PositiveInfinity, 2)));
        }

        [Test]
        public void Apply_RandomValues_NeverBelowInputAndWithinStepBound()
        {
            System.Random random = new System.Random(RandomSeed);
            for (int i = 0; i < RandomSamples; i++)
            {
                double x = System.Math.Pow(10d, random.NextDouble() * 12d);
                double result = NiceCeiling.Apply(x, 2);
                Assert.GreaterOrEqual(result, x * (1d - EpsilonTolerance));
                Assert.Less(result, x * 1.11d + 1d);
            }
        }
    }
}
