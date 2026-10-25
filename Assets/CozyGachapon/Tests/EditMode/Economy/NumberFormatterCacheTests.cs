using System;
using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using NUnit.Framework;
using Object = UnityEngine.Object;

namespace CozyGachapon.Tests.Economy
{
    public class NumberFormatterCacheTests
    {
        private EconomyConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = UnityEngine.ScriptableObject.CreateInstance<EconomyConfig>();
            _config.SetDisplaySigDigitsForTest(3);
            _config.SetRateDecimalsForTest(1);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void Cache_SameDisplayString_ReturnsSameReferenceUntilItChanges()
        {
            NumberFormatter formatter = Create();

            string first = formatter.FormatAmount(1234.5d);
            string second = formatter.FormatAmount(1239d);
            string third = formatter.FormatAmount(1240d);

            Assert.AreSame(first, second);
            Assert.AreNotSame(first, third);
            Assert.AreEqual("1.24K", third);
        }

        [Test]
        public void Cache_AmountAndRate_AreIndependent()
        {
            NumberFormatter formatter = Create();

            string amount = formatter.FormatAmount(1234.5d);
            string rate = formatter.FormatRate(1234.5d);

            Assert.AreEqual("1.23K", amount);
            Assert.AreEqual("1.23K/s", rate);
            Assert.AreSame(amount, formatter.FormatAmount(1234.9d));
            Assert.AreSame(rate, formatter.FormatRate(1234.9d));
        }

        [Test]
        public void Constructor_NullConfig_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new NumberFormatter(null));
        }

        [Test]
        public void Formatter_ImplementsContract()
        {
            INumberFormatter formatter = Create();

            Assert.AreEqual("1.00K", formatter.FormatAmount(1000d));
        }

        private NumberFormatter Create()
        {
            return new NumberFormatter(_config);
        }
    }
}
