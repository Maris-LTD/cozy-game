using CozyGachapon.Economy;
using CozyGachapon.Tests.Pooling;
using NUnit.Framework;
using Object = UnityEngine.Object;

namespace CozyGachapon.Tests.Economy
{
    public class PriceMathZeroAllocTests
    {
        private const int Iterations = 100;
        private const int LargeN = 20000;

        private EconomyConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = UnityEngine.ScriptableObject.CreateInstance<EconomyConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void PriceMath_AllBranches_DoNotAllocate()
        {
            SpinPriceCurve curve = new SpinPriceCurve(_config);
            DerivedPriceCalculator calculator = new DerivedPriceCalculator(curve);

            int allocations = GcAllocProbe.CountAllocations(() => RunPriceMath(curve, calculator));

            Assert.AreEqual(0, allocations);
        }

        private static void RunPriceMath(SpinPriceCurve curve, DerivedPriceCalculator calculator)
        {
            for (int i = 0; i < Iterations; i++)
            {
                NiceCeiling.Apply(1146.74d, 2);
                NiceCeiling.Apply(double.NaN, 2);
                curve.RawPrice(i);
                curve.Price(i);
                curve.Price(LargeN);
                calculator.TryGet(1.5d, i, out _);
                calculator.TryGet(0d, i, out _);
                calculator.TryGet(2d, LargeN, out _);
            }
        }
    }
}
