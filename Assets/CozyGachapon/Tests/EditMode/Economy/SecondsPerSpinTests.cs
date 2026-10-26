using System.Collections.Generic;
using System.Globalization;
using CozyGachapon.Economy;
using NUnit.Framework;

namespace CozyGachapon.Tests.Economy
{
    [TestFixture]
    public sealed class SecondsPerSpinTests
    {
        [Test]
        public void AC41_SpinCheckpoints_SecondsPerSpinMatchesTableAndUnderMax()
        {
            EconomyConfig config = EconomyTestAssets.LoadConfig();
            EconomyBalanceTargets targets = EconomyTestAssets.LoadTargets();
            EconomyBalanceModel model = new EconomyBalanceModel(config, targets);

            double[] expectedSeconds = { 11.3d, 31.3d, 84.4d };
            IReadOnlyList<int> checkpoints = targets.SpinCheckpoints;
            IReadOnlyList<double> maxSeconds = targets.PlinkoOnlySpinSecondsMax;

            Assert.That(checkpoints.Count, Is.EqualTo(expectedSeconds.Length));
            Assert.That(maxSeconds.Count, Is.EqualTo(expectedSeconds.Length));

            for (int i = 0; i < checkpoints.Count; i++)
            {
                int n = checkpoints[i];
                double actual = model.SecondsPerSpin(n);
                double limit = maxSeconds[i];

                TestContext.WriteLine(
                    "n=" + n.ToString(CultureInfo.InvariantCulture) +
                    " -> seconds_per_spin=" + actual.ToString("F1", CultureInfo.InvariantCulture) +
                    "s (max=" + limit.ToString("F1", CultureInfo.InvariantCulture) + "s)");

                Assert.That(actual, Is.EqualTo(expectedSeconds[i]).Within(0.1d));
                Assert.That(actual, Is.LessThanOrEqualTo(limit));
            }
        }

        [Test]
        public void PlinkoRateModel_AtCheckpoints_MatchesCeilingTimesBaseCoinValue()
        {
            EconomyConfig config = EconomyTestAssets.LoadConfig();
            EconomyBalanceTargets targets = EconomyTestAssets.LoadTargets();
            EconomyBalanceModel model = new EconomyBalanceModel(config, targets);

            Assert.That(model.PlinkoRateModel(PlinkoRateModelPreset.BaseNoAuto, 0), Is.EqualTo(20d).Within(1e-6d));
            Assert.That(model.PlinkoRateModel(PlinkoRateModelPreset.BaseNoAuto, 100), Is.EqualTo(373.6d).Within(0.1d));
            Assert.That(model.PlinkoRateModel(PlinkoRateModelPreset.AutoFloorAndBounceCap, 100), Is.EqualTo(784.5d).Within(0.1d));
        }

        [Test]
        public void BalanceTargetsValidator_DefaultAsset_HasZeroIssues()
        {
            EconomyBalanceTargets targets = EconomyTestAssets.LoadTargets();
            List<EconomyConfigIssue> issues = new List<EconomyConfigIssue>();

            EconomyBalanceTargetsValidator.Validate(targets, issues);

            Assert.That(issues.Count, Is.EqualTo(0));
        }
    }
}
