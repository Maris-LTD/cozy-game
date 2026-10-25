using System;
using CozyGachapon.Economy;
using NUnit.Framework;
using Object = UnityEngine.Object;

namespace CozyGachapon.Tests.Economy
{
    public class BaseCoinValueTests
    {
        private const int MonotonicRange = 2000;
        private const int HugeN = 100000;
        private const double LooseTolerance = 1e-3d;

        private EconomyConfig _config;
        private BaseCoinValueCalculator _calculator;

        [SetUp]
        public void SetUp()
        {
            _config = UnityEngine.ScriptableObject.CreateInstance<EconomyConfig>();
            _calculator = new BaseCoinValueCalculator(_config, new SpinPriceCurve(_config));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void Compute_ZeroSpins_IsExactlyOne()
        {
            EconomyStateTestData.AssertExactlyEqual(1d, _calculator.Compute(0));
        }

        [TestCase(1, 1.0297d)]
        [TestCase(100, 18.679d)]
        public void Compute_DefaultExponent_MatchesGddValues(int n, double expected)
        {
            Assert.AreEqual(expected, _calculator.Compute(n), expected * LooseTolerance);
        }

        [Test]
        public void Compute_NegativeSpins_IsOne()
        {
            Assert.AreEqual(1d, _calculator.Compute(-7));
        }

        [Test]
        public void Compute_HugeSpins_IsFiniteAndWithinCap()
        {
            double value = _calculator.Compute(HugeN);
            Assert.IsFalse(double.IsInfinity(value));
            Assert.IsFalse(double.IsNaN(value));
            Assert.LessOrEqual(value, _config.WalletSafeCap);
            Assert.IsFalse(double.IsInfinity(_calculator.Compute(int.MaxValue)));
        }

        [Test]
        public void Compute_Range_IsAtLeastOneAndNonDecreasing()
        {
            double previous = 1d;
            for (int n = 0; n <= MonotonicRange; n++)
            {
                double current = _calculator.Compute(n);
                Assert.GreaterOrEqual(current, 1d, "n=" + n);
                Assert.GreaterOrEqual(current, previous, "n=" + n);
                previous = current;
            }
        }

        [Test]
        public void Constructor_NullArguments_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => new BaseCoinValueCalculator(null, new SpinPriceCurve(_config)));
            Assert.Throws<ArgumentNullException>(() => new BaseCoinValueCalculator(_config, null));
        }
    }
}
