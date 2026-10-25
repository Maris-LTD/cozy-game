using CozyGachapon.Economy;
using CozyGachapon.Tests.Pooling;
using NUnit.Framework;
using Object = UnityEngine.Object;

namespace CozyGachapon.Tests.Economy
{
    public class NumberFormatterZeroAllocTests
    {
        private const int Iterations = 100;
        private const int BufferLength = 32;

        private static readonly double[] Values =
        {
            0d, 999.9d, 1000d, 1234.5d, 12345d, 999999.6d, 1e6d, 1e9d, 1e12d, 1e15d, 1e18d, 1e300d, 1e305d,
            6.58d, double.NaN, -5d, double.PositiveInfinity, double.NegativeInfinity, double.MaxValue
        };

        private EconomyConfig _config;
        private char[] _destination;

        [SetUp]
        public void SetUp()
        {
            _config = UnityEngine.ScriptableObject.CreateInstance<EconomyConfig>();
            _destination = new char[BufferLength];
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void TryFormat_AmountAndRate_IntoSpan_DoNotAllocate()
        {
            NumberFormatter formatter = new NumberFormatter(_config);

            int allocations = GcAllocProbe.CountAllocations(() => RunTryFormat(formatter));

            Assert.AreEqual(0, allocations);
        }

        [Test]
        public void FormatAmount_CachedPath_DoesNotAllocate()
        {
            NumberFormatter formatter = new NumberFormatter(_config);

            int allocations = GcAllocProbe.CountAllocations(() => RunCachedAmount(formatter));

            Assert.AreEqual(0, allocations);
        }

        [Test]
        public void FormatRate_CachedPath_DoesNotAllocate()
        {
            NumberFormatter formatter = new NumberFormatter(_config);

            int allocations = GcAllocProbe.CountAllocations(() => RunCachedRate(formatter));

            Assert.AreEqual(0, allocations);
        }

        private void RunTryFormat(NumberFormatter formatter)
        {
            System.Span<char> destination = _destination;
            for (int i = 0; i < Iterations; i++)
            {
                for (int v = 0; v < Values.Length; v++)
                {
                    formatter.TryFormatAmount(Values[v], destination);
                    formatter.TryFormatRate(Values[v], destination);
                }
            }
        }

        private static void RunCachedAmount(NumberFormatter formatter)
        {
            for (int i = 0; i < Iterations; i++)
            {
                formatter.FormatAmount(1234.5d);
                formatter.FormatAmount(1239d);
                formatter.FormatAmount(1234.9d);
            }
        }

        private static void RunCachedRate(NumberFormatter formatter)
        {
            for (int i = 0; i < Iterations; i++)
            {
                formatter.FormatRate(6.58d);
                formatter.FormatRate(6.51d);
                formatter.FormatRate(6.59d);
            }
        }
    }
}
