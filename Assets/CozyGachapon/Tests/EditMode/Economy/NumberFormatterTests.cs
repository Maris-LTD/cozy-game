using System;
using CozyGachapon.Economy;
using NUnit.Framework;
using Object = UnityEngine.Object;

namespace CozyGachapon.Tests.Economy
{
    public class NumberFormatterTests
    {
        private const int MaxGroup = 101;

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

        [TestCase(0d, "0")]
        [TestCase(999.9d, "999")]
        [TestCase(1000d, "1.00K")]
        [TestCase(1234.5d, "1.23K")]
        [TestCase(12345d, "12.3K")]
        [TestCase(999999.6d, "999K")]
        [TestCase(1e6d, "1.00M")]
        [TestCase(1e9d, "1.00B")]
        [TestCase(1e12d, "1.00T")]
        [TestCase(1e15d, "1.00aa")]
        [TestCase(1e18d, "1.00ab")]
        [TestCase(1e300d, "1.00dr")]
        [TestCase(double.NaN, "0")]
        public void AC37_Amount_SigDigits3(double value, string expected)
        {
            Assert.AreEqual(expected, Create().FormatAmount(value));
        }

        [TestCase(1999.99d, "1.99K")]
        [TestCase(1999.999999d, "1.99K")]
        [TestCase(4.56e6d, "4.56M")]
        [TestCase(9.99e8d, "999M")]
        [TestCase(12999.9d, "12.9K")]
        public void Amount_JustBelowDigitBoundary_TruncatesAndExactValuesKeepTheirDigits(double value, string expected)
        {
            Assert.AreEqual(expected, Create().FormatAmount(value));
        }

        [TestCase(6.58d, "6.5/s")]
        [TestCase(0d, "0.0/s")]
        [TestCase(1234.5d, "1.23K/s")]
        public void AC38_Rate_RateDecimals1(double value, string expected)
        {
            Assert.AreEqual(expected, Create().FormatRate(value));
        }

        [TestCase(-1d)]
        [TestCase(-1e300d)]
        [TestCase(double.NegativeInfinity)]
        [TestCase(double.NaN)]
        public void Amount_NegativeOrNaN_FormatsZero(double value)
        {
            Assert.AreEqual("0", Create().FormatAmount(value));
        }

        [TestCase(-1d)]
        [TestCase(-1e300d)]
        [TestCase(double.NegativeInfinity)]
        [TestCase(double.NaN)]
        public void Rate_NegativeOrNaN_FormatsZeroRate(double value)
        {
            Assert.AreEqual("0.0/s", Create().FormatRate(value));
        }

        [Test]
        public void Amount_PositiveInfinity_DoesNotThrowAndEqualsMaxValue()
        {
            NumberFormatter formatter = Create();

            string infinity = null;
            Assert.DoesNotThrow(() => infinity = formatter.FormatAmount(double.PositiveInfinity));

            Assert.AreEqual("179dt", infinity);
            Assert.AreEqual(formatter.FormatAmount(double.MaxValue), infinity);
            Assert.AreEqual("179dt/s", formatter.FormatRate(double.PositiveInfinity));
        }

        [Test]
        public void Amount_SigDigits4_KeepsFourSignificantDigits()
        {
            _config.SetDisplaySigDigitsForTest(4);
            NumberFormatter formatter = Create();

            Assert.AreEqual("1.234K", formatter.FormatAmount(1234.5d));
            Assert.AreEqual("12.34K", formatter.FormatAmount(12345d));
            Assert.AreEqual("123.4K", formatter.FormatAmount(123456d));
            Assert.AreEqual("1.000M", formatter.FormatAmount(1e6d));
        }

        [Test]
        public void Rate_Decimals0_HasNoSeparator()
        {
            _config.SetRateDecimalsForTest(0);
            NumberFormatter formatter = Create();

            Assert.AreEqual("2/s", formatter.FormatRate(2.5d));
            Assert.AreEqual("0/s", formatter.FormatRate(double.NaN));
            Assert.AreEqual("999/s", formatter.FormatRate(999.9d));
        }

        [Test]
        public void Rate_Decimals2_KeepsTwoDecimals()
        {
            _config.SetRateDecimalsForTest(2);
            NumberFormatter formatter = Create();

            Assert.AreEqual("6.58/s", formatter.FormatRate(6.58d));
            Assert.AreEqual("0.00/s", formatter.FormatRate(0d));
            Assert.AreEqual("999.99/s", formatter.FormatRate(999.999d));
        }

        [Test]
        public void Rate_JustBelowThousand_TruncatesAndNeverRollsOver()
        {
            NumberFormatter formatter = Create();

            Assert.AreEqual("999.9/s", formatter.FormatRate(999.99d));
            Assert.AreEqual("999.9/s", formatter.FormatRate(999.9999999999999d));
            Assert.AreEqual("1.00K/s", formatter.FormatRate(1000d));
        }

        [Test]
        public void Amount_TruncatesInsteadOfRounding()
        {
            NumberFormatter formatter = Create();

            Assert.AreEqual("999", formatter.FormatAmount(999.99999d));
            Assert.AreEqual("999K", formatter.FormatAmount(999999.999d));
            Assert.AreEqual("999M", formatter.FormatAmount(999999999.9d));
            Assert.AreEqual("999M", formatter.FormatAmount(999999999.9999999d));
            Assert.AreEqual("9.99K", formatter.FormatAmount(9999.9d));
            Assert.AreEqual("99.9K", formatter.FormatAmount(99999.9d));
        }

        [Test]
        public void Amount_DivisionErrorDoesNotLoseADigit()
        {
            NumberFormatter formatter = Create();

            Assert.AreEqual("1.23K", formatter.FormatAmount(1230d));
            Assert.AreEqual("1.10K", formatter.FormatAmount(1100d));
            Assert.AreEqual("4.56M", formatter.FormatAmount(4560000d));
            Assert.AreEqual("7.89B", formatter.FormatAmount(7.89e9d));
        }

        [Test]
        public void Amount_UpToSafeRangeCeiling_Formats()
        {
            NumberFormatter formatter = Create();

            Assert.AreEqual("100ds", formatter.FormatAmount(1e305d));
            Assert.AreEqual("150ds", formatter.FormatAmount(1.5e305d));
            Assert.AreEqual("1.00dr", formatter.FormatAmount(_config.WalletSafeCap));
        }

        [Test]
        public void Amount_EveryPowerOfThousand_FormatsOneWithRightSuffix()
        {
            NumberFormatter formatter = Create();
            string[] expectedSuffixes = { "K", "M", "B", "T" };

            for (int k = 1; k <= MaxGroup; k++)
            {
                double value = double.Parse("1e" + (3 * k), System.Globalization.CultureInfo.InvariantCulture);
                string expectedSuffix = k <= expectedSuffixes.Length
                    ? expectedSuffixes[k - 1]
                    : new string(new[] { (char)('a' + (k - 5) / 26), (char)('a' + (k - 5) % 26) });

                Assert.AreEqual("1.00" + expectedSuffix, formatter.FormatAmount(value), "k=" + k);
            }
        }

        [Test]
        public void Amount_GroupBoundariesBetweenTwoLetterSuffixes()
        {
            NumberFormatter formatter = Create();

            Assert.AreEqual("1.00az", formatter.FormatAmount(1e90d));
            Assert.AreEqual("1.00ba", formatter.FormatAmount(1e93d));
            Assert.AreEqual("999az", formatter.FormatAmount(9.999999e92d));
        }

        [Test]
        public void TryFormat_DestinationTooShort_ReturnsZeroAndMatchesWhenLongEnough()
        {
            NumberFormatter formatter = Create();
            Span<char> tooShort = stackalloc char[3];
            Span<char> fits = stackalloc char[16];

            Assert.AreEqual(0, formatter.TryFormatAmount(1234.5d, tooShort));
            Assert.AreEqual(0, formatter.TryFormatRate(1234.5d, tooShort));
            int amountLength = formatter.TryFormatAmount(1234.5d, fits);
            Assert.AreEqual("1.23K", new string(fits.Slice(0, amountLength)));
            int rateLength = formatter.TryFormatRate(1234.5d, fits);
            Assert.AreEqual("1.23K/s", new string(fits.Slice(0, rateLength)));
        }

        [Test]
        public void MaxLength_CoversEveryFormattedString()
        {
            NumberFormatter formatter = Create();
            double[] values = { 0d, 6.58d, 999.9d, 1234.5d, 123456d, 1e300d, 1e305d, double.MaxValue, double.PositiveInfinity };

            for (int i = 0; i < values.Length; i++)
            {
                Assert.LessOrEqual(formatter.FormatAmount(values[i]).Length, formatter.MaxLength);
                Assert.LessOrEqual(formatter.FormatRate(values[i]).Length, formatter.MaxLength);
            }
        }

        private NumberFormatter Create()
        {
            return new NumberFormatter(_config);
        }
    }
}
