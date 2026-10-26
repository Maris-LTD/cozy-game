using CozyGachapon.Economy;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Economy
{
    [TestFixture]
    public sealed class FirstSpinTimeTests
    {
        private const double DefaultMedianFallTime = 2.5d;

        [Test]
        public void AC40_DefaultConfigAndTargets_FirstSpinTimeMatchesExpectedAndUnderMax()
        {
            EconomyConfig config = EconomyTestAssets.LoadConfig();
            EconomyBalanceTargets targets = EconomyTestAssets.LoadTargets();
            EconomyBalanceModel model = new EconomyBalanceModel(config, targets);

            double firstSpinTime = model.FirstSpinTime(DefaultMedianFallTime);

            Assert.That(firstSpinTime, Is.EqualTo(13.8d).Within(0.1d));
            Assert.That(firstSpinTime, Is.LessThanOrEqualTo(targets.FirstSpinMaxSeconds));
        }

        [Test]
        public void FirstSpinTime_WhenStartingCoinsExceedSpinBasePrice_ReturnsMedianFallTimeOnly()
        {
            EconomyConfig config = ScriptableObject.CreateInstance<EconomyConfig>();
            config.SetStartingCoinsForTest(150d);
            config.SetSpinBasePriceForTest(100d);
            config.SetSpinGrowthForTest(1.05d);
            config.SetCoinValueExponentForTest(0.6d);

            EconomyBalanceTargets targets = EconomyTestAssets.LoadTargets();
            EconomyBalanceModel model = new EconomyBalanceModel(config, targets);

            double firstSpinTime = model.FirstSpinTime(DefaultMedianFallTime);
            UnityEngine.Object.DestroyImmediate(config);

            Assert.That(firstSpinTime, Is.EqualTo(DefaultMedianFallTime).Within(1e-9d));
        }
    }
}
