using System;
using CozyGachapon.Economy;
using NUnit.Framework;
using Object = UnityEngine.Object;

namespace CozyGachapon.Tests.Economy
{
    public class SpinPriceCurveTests
    {
        private const int MonotonicRange = 2000;
        private const double SmallCap = 1e6d;
        private const double Tolerance = 1e-9d;

        private EconomyConfig _config;
        private SpinPriceCurve _curve;

        [SetUp]
        public void SetUp()
        {
            _config = UnityEngine.ScriptableObject.CreateInstance<EconomyConfig>();
            _curve = new SpinPriceCurve(_config);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [TestCase(0, 100d)]
        [TestCase(1, 110d)]
        [TestCase(2, 120d)]
        [TestCase(3, 120d)]
        [TestCase(10, 170d)]
        [TestCase(20, 270d)]
        [TestCase(50, 1200d)]
        [TestCase(100, 14000d)]
        [TestCase(150, 160000d)]
        public void Price_DefaultConfig_MatchesGddTable(int n, double expected)
        {
            Assert.AreEqual(expected, _curve.Price(n));
        }

        [Test]
        public void Price_DefaultConfig_IsNonDecreasing()
        {
            double previous = _curve.Price(0);
            for (int n = 1; n <= MonotonicRange; n++)
            {
                double current = _curve.Price(n);
                Assert.GreaterOrEqual(current, previous, "n=" + n);
                previous = current;
            }
        }

        [Test]
        public void NegativeN_IsTreatedAsZero()
        {
            Assert.AreEqual(_curve.Price(0), _curve.Price(-1));
            Assert.AreEqual(100d, _curve.RawPrice(-5));
        }

        [Test]
        public void RawPrice_IsUnroundedExponentialCurve()
        {
            Assert.AreEqual(105d, _curve.RawPrice(1), 105d * Tolerance);
            Assert.AreEqual(1146.74d, _curve.RawPrice(50), 0.01d);
        }

        [Test]
        public void Price_LargeN_ClampsToWalletSafeCap()
        {
            double cap = _config.WalletSafeCap;
            Assert.AreEqual(cap, _curve.Price(20000));
            Assert.AreEqual(cap, _curve.Price(int.MaxValue));
            Assert.IsFalse(double.IsInfinity(_curve.Price(int.MaxValue)));
            Assert.LessOrEqual(_curve.Price(14100), cap);
        }

        [Test]
        public void Price_SmallCap_ClampsAndStaysNonDecreasing()
        {
            _config.SetWalletSafeCapForTest(SmallCap);
            double previous = 0d;
            for (int n = 0; n <= 400; n++)
            {
                double current = _curve.Price(n);
                Assert.LessOrEqual(current, SmallCap);
                Assert.GreaterOrEqual(current, previous);
                previous = current;
            }

            Assert.AreEqual(SmallCap, _curve.Price(200));
            Assert.AreEqual(SmallCap, _curve.Price(int.MaxValue));
        }

        [Test]
        public void Price_ReadsKnobsFromConfig()
        {
            _config.SetSpinBasePriceForTest(200d);
            Assert.AreEqual(200d, _curve.Price(0));

            _config.SetSpinBasePriceForTest(100d);
            _config.SetPriceNiceSigDigitsForTest(3);
            Assert.AreEqual(105d, _curve.Price(1));

            _config.SetPriceNiceSigDigitsForTest(2);
            _config.SetSpinGrowthForTest(1.08d);
            Assert.AreEqual(110d, _curve.Price(1));
            Assert.AreEqual(220d, _curve.Price(10));
        }

        [Test]
        public void Constructor_NullConfig_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new SpinPriceCurve(null));
        }
    }
}
