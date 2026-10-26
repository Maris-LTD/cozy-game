using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using NUnit.Framework;

namespace CozyGachapon.Tests.Economy
{
    public class ChannelIncomeTests : IncomePipelineFixture
    {
        private const double FlatUnitValue = 5d;
        private const double FlatUnits = 2d;
        private const double BaseRate = 50d;

        [Test]
        public void ChannelIncome_SynergyBoostGlobal_AddsWithinTierAndMultipliesAcross()
        {
            AddChannel("syn.one", IncomeChannel.Idle, ModifierTier.Synergy, 1d);
            AddChannel("syn.two", IncomeChannel.Idle, ModifierTier.Synergy, 1d);
            AddChannel("boost.one", IncomeChannel.Both, ModifierTier.Boost, 1d);
            AddChannel("event.test", IncomeChannel.Both, ModifierTier.Global, 1d);

            double income = Pipeline.GetChannelIncome(SourceA, 0d);

            Assert.AreEqual(600d, income);
            Assert.AreNotEqual(800d, income);
            Assert.AreNotEqual(250d, income);
        }

        [Test]
        public void ChannelIncome_NoModifiers_EqualsBaseRate()
        {
            Assert.AreEqual(BaseRate, Pipeline.GetChannelIncome(SourceA, 0d));
        }

        [Test]
        public void ChannelIncome_ZeroBase_IsZero()
        {
            AddChannel("boost.one", IncomeChannel.Both, ModifierTier.Boost, 1d);
            AddChannel("syn.one", IncomeChannel.Both, ModifierTier.Synergy, 1d);

            Assert.AreEqual(0d, Pipeline.GetChannelIncome(PlinkoSource, 0d));
        }

        [Test]
        public void ChannelIncome_FlatUnits_AddBeforeMultipliers()
        {
            AddSource("base.flat", ModifierTier.Base, FlatUnits, SourceA);
            AddChannel("boost.one", IncomeChannel.Idle, ModifierTier.Boost, 1d);

            double income = Pipeline.GetChannelIncome(SourceA, FlatUnitValue);

            Assert.AreEqual((BaseRate + FlatUnits * FlatUnitValue) * 2d, income);
            Assert.AreEqual(FlatUnits, Pipeline.GetFlatUnits(SourceA));
            Assert.AreEqual(0d, Pipeline.GetFlatUnits(SourceB));
        }

        [Test]
        public void ChannelIncome_FlatOnZeroBasePlinkoSource_UsesFlatUnitValue()
        {
            AddSource("base.flat", ModifierTier.Base, 1.5d, PlinkoSource);

            Assert.AreEqual(1.5d * FlatUnitValue, Pipeline.GetChannelIncome(PlinkoSource, FlatUnitValue));
        }

        [Test]
        public void ChannelIncome_PlinkoBoost_DoesNotAffectIdleSource()
        {
            AddChannel("boost.plinko", IncomeChannel.Plinko, ModifierTier.Boost, 1d);

            Assert.AreEqual(BaseRate, Pipeline.GetChannelIncome(SourceA, 0d));
        }

        [Test]
        public void ChannelIncome_UnknownSource_IsZero()
        {
            Assert.AreEqual(0d, Pipeline.GetChannelIncome("missing", FlatUnitValue));
        }

        [Test]
        public void ChannelIncome_BadFlatUnitValue_IsTreatedAsZero()
        {
            AddSource("base.flat", ModifierTier.Base, FlatUnits, SourceA);

            Assert.AreEqual(BaseRate, Pipeline.GetChannelIncome(SourceA, double.NaN));
            Assert.AreEqual(BaseRate, Pipeline.GetChannelIncome(SourceA, -3d));
        }

        [Test]
        public void RegisterIncomeSource_InvalidInput_IsRejected()
        {
            Assert.IsFalse(Pipeline.RegisterIncomeSource("shelf.c", IncomeChannel.Both, 1d));
            Assert.IsFalse(Pipeline.RegisterIncomeSource("shelf.c", IncomeChannel.Idle, double.NaN));
            Assert.IsFalse(Pipeline.RegisterIncomeSource("shelf.c", IncomeChannel.Idle, -1d));
            Assert.IsFalse(Pipeline.RegisterIncomeSource("shelf.c", IncomeChannel.Idle, double.PositiveInfinity));
            Assert.IsFalse(Pipeline.RegisterIncomeSource(null, IncomeChannel.Idle, 1d));
            Assert.IsFalse(Pipeline.RegisterIncomeSource(string.Empty, IncomeChannel.Idle, 1d));
            Assert.AreEqual(0d, Pipeline.GetChannelIncome("shelf.c", 0d));
        }

        [Test]
        public void RegisterIncomeSource_SameKey_UpdatesBaseRate()
        {
            Assert.IsTrue(Pipeline.RegisterIncomeSource(SourceA, IncomeChannel.Idle, 70d));

            Assert.AreEqual(70d, Pipeline.GetChannelIncome(SourceA, 0d));
        }

        [Test]
        public void RegisterIncomeSource_BeyondDefaultCapacity_KeepsAllSources()
        {
            for (int i = 0; i < 40; i++)
            {
                Assert.IsTrue(Pipeline.RegisterIncomeSource("grow." + i, IncomeChannel.Idle, i + 1));
            }

            Assert.AreEqual(40d, Pipeline.GetChannelIncome("grow.39", 0d));
            Assert.AreEqual(BaseRate, Pipeline.GetChannelIncome(SourceA, 0d));
        }

        [Test]
        public void RegisterModifier_BeyondDefaultCapacity_KeepsAllModifiers()
        {
            for (int i = 0; i < 40; i++)
            {
                AddChannel("many." + i, IncomeChannel.Idle, ModifierTier.Boost, 0.01d);
            }

            Assert.AreEqual(1.4d, Pipeline.GetTierMultiplier(ModifierTier.Boost, IncomeChannel.Idle), 1e-9d);
        }

        [Test]
        public void EconomyCore_Initialize_PreRegistersPlinkoCoinSource()
        {
            EconomyCore core = new EconomyCore(new CountingEconomyDiagnosticsSink());

            Assert.IsTrue(core.Initialize(EconomyTestAssets.LoadConfig(), ReleaseTier.Mvp, null));

            Assert.IsNotNull(core.IncomePipeline);
            Assert.AreEqual(1d, core.IncomePipeline.GetSynergyMultiplier(EconomyIds.PlinkoCoinSource));
            Assert.AreEqual(0d, core.IncomePipeline.GetChannelIncome(EconomyIds.PlinkoCoinSource, 1d));
        }
    }
}
