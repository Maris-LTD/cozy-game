using System;
using CozyGachapon.Economy;
using NUnit.Framework;
using Object = UnityEngine.Object;

namespace CozyGachapon.Tests.Economy
{
    public class DerivedPriceTests
    {
        private EconomyConfig _config;
        private SpinPriceCurve _curve;
        private DerivedPriceCalculator _calculator;

        [SetUp]
        public void SetUp()
        {
            _config = UnityEngine.ScriptableObject.CreateInstance<EconomyConfig>();
            _curve = new SpinPriceCurve(_config);
            _calculator = new DerivedPriceCalculator(_curve);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [TestCase(1.5d, 0, 150d)]
        [TestCase(1.5d, 50, 1800d)]
        [TestCase(2d, 100, 27000d)]
        public void TryGet_ValidCoef_ReturnsNicePrice(double coef, int n, double expected)
        {
            Assert.IsTrue(_calculator.TryGet(coef, n, out double price));
            Assert.AreEqual(expected, price);
            Assert.Greater(price, 0d);
        }

        [TestCase(0d)]
        [TestCase(-1d)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void TryGet_InvalidCoef_ReturnsFalseWithZeroPrice(double coef)
        {
            Assert.IsFalse(_calculator.TryGet(coef, 10, out double price));
            Assert.AreEqual(0d, price);
        }

        [Test]
        public void TryGet_HugeN_ClampsToWalletSafeCap()
        {
            Assert.IsTrue(_calculator.TryGet(1.5d, 20000, out double price));
            Assert.AreEqual(_config.WalletSafeCap, price);
        }

        [Test]
        public void TryGet_TinyCoef_NeverReturnsTrueWithZero()
        {
            bool found = _calculator.TryGet(double.Epsilon, 0, out double price);
            if (found)
            {
                Assert.Greater(price, 0d);
            }
            else
            {
                Assert.AreEqual(0d, price);
            }
        }

        [Test]
        public void TryGet_TwoCalculators_AgreeOnSameInputs()
        {
            DerivedPriceCalculator other = new DerivedPriceCalculator(new SpinPriceCurve(_config));
            Assert.IsTrue(_calculator.TryGet(1.5d, 73, out double first));
            Assert.IsTrue(other.TryGet(1.5d, 73, out double second));
            Assert.AreEqual(first, second);
        }

        [Test]
        public void Constructor_NullCurve_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new DerivedPriceCalculator(null));
        }
    }
}
