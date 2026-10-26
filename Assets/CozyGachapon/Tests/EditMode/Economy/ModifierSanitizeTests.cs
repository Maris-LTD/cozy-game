using System.Collections;
using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using NUnit.Framework;

namespace CozyGachapon.Tests.Economy
{
    public class ModifierSanitizeTests : IncomePipelineFixture
    {
        private const double FlatUnitValue = 5d;

        private static readonly double[] BadValues =
        {
            -0.5d,
            double.NaN,
            double.PositiveInfinity,
            double.NegativeInfinity
        };

        private static readonly ModifierTier[] Tiers =
        {
            ModifierTier.Base,
            ModifierTier.Synergy,
            ModifierTier.Boost,
            ModifierTier.Prestige,
            ModifierTier.Global
        };

        public static IEnumerable BadModifierCases()
        {
            for (int t = 0; t < Tiers.Length; t++)
            {
                for (int v = 0; v < BadValues.Length; v++)
                {
                    yield return new TestCaseData(Tiers[t], BadValues[v]);
                }
            }
        }

        [TestCaseSource(nameof(BadModifierCases))]
        public void BadModifier_AnyTier_LeavesIncomeUnchanged(ModifierTier tier, double badValue)
        {
            AddValidSetup();
            double expected = Pipeline.GetChannelIncome(SourceA, FlatUnitValue);

            AddBad("bad.one", tier, badValue);

            Assert.AreEqual(expected, Pipeline.GetChannelIncome(SourceA, FlatUnitValue));
        }

        [TestCaseSource(nameof(BadModifierCases))]
        public void BadModifier_AnyTier_IsStillAcceptedAndRemovable(ModifierTier tier, double badValue)
        {
            Assert.IsTrue(AddBad("bad.one", tier, badValue));
            Assert.AreEqual(1, Pipeline.Unregister("bad.one"));
        }

        [Test]
        public void ValidAndBadInSameTier_OnlyValidCounts()
        {
            AddChannel("good", IncomeChannel.Idle, ModifierTier.Boost, 0.5d);
            AddChannel("bad.neg", IncomeChannel.Idle, ModifierTier.Boost, -0.5d);
            AddChannel("bad.nan", IncomeChannel.Idle, ModifierTier.Boost, double.NaN);
            AddChannel("bad.inf", IncomeChannel.Idle, ModifierTier.Boost, double.PositiveInfinity);

            Assert.AreEqual(1.5d, Pipeline.GetTierMultiplier(ModifierTier.Boost, IncomeChannel.Idle));
        }

        [Test]
        public void ValidAndBadSynergy_OnlyValidCounts()
        {
            AddChannel("good", IncomeChannel.Idle, ModifierTier.Synergy, 0.5d);
            AddSource("bad.src", ModifierTier.Synergy, double.NegativeInfinity, SourceA);

            Assert.AreEqual(1.5d, Pipeline.GetSynergyMultiplier(SourceA));
        }

        [TestCase(0d, 0d)]
        [TestCase(1.5d, 1.5d)]
        [TestCase(-0.5d, 0d)]
        [TestCase(double.NaN, 0d)]
        [TestCase(double.PositiveInfinity, 0d)]
        [TestCase(double.NegativeInfinity, 0d)]
        [TestCase(double.MaxValue, double.MaxValue)]
        public void ValueSanitizer_Sanitize_ReturnsNeutralForInvalid(double input, double expected)
        {
            Assert.AreEqual(expected, ValueSanitizer.Sanitize(input));
        }

        private void AddValidSetup()
        {
            AddSource("flat", ModifierTier.Base, 2d, SourceA);
            AddChannel("syn", IncomeChannel.Idle, ModifierTier.Synergy, 1d);
            AddChannel("boost", IncomeChannel.Both, ModifierTier.Boost, 0.5d);
            AddChannel("prestige", IncomeChannel.Both, ModifierTier.Prestige, 1d);
            AddChannel("global", IncomeChannel.Both, ModifierTier.Global, 0.25d);
        }

        private bool AddBad(string id, ModifierTier tier, double value)
        {
            if (tier == ModifierTier.Base)
            {
                return AddSource(id, tier, value, SourceA);
            }

            return AddChannel(id, IncomeChannel.Idle, tier, value);
        }
    }
}
