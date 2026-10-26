using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using NUnit.Framework;

namespace CozyGachapon.Tests.Economy
{
    public class ModifierTierTests : IncomePipelineFixture
    {
        [Test]
        public void Synergy_NoModifiers_IsOne()
        {
            Assert.AreEqual(1d, Pipeline.GetSynergyMultiplier(SourceA));
        }

        [Test]
        public void Synergy_TwoIdleChannelModifiers_Adds()
        {
            AddChannel("m.one", IncomeChannel.Idle, ModifierTier.Synergy, 1d);
            AddChannel("m.two", IncomeChannel.Idle, ModifierTier.Synergy, 1d);

            Assert.AreEqual(3d, Pipeline.GetSynergyMultiplier(SourceA));
        }

        [Test]
        public void Synergy_SumAboveCap_ClampsToConfigCap()
        {
            AddChannel("m.one", IncomeChannel.Idle, ModifierTier.Synergy, 2.1d);
            AddChannel("m.two", IncomeChannel.Idle, ModifierTier.Synergy, 2.1d);

            Assert.AreEqual(Config.SynergyTierCap, Pipeline.GetSynergyMultiplier(SourceA));
            Assert.AreEqual(SynergyCap, Pipeline.GetSynergyMultiplier(SourceA));
        }

        [Test]
        public void Synergy_SumExactlyAtCap_EqualsCap()
        {
            AddChannel("m.one", IncomeChannel.Idle, ModifierTier.Synergy, 3d);

            Assert.AreEqual(Config.SynergyTierCap, Pipeline.GetSynergyMultiplier(SourceA));
        }

        [Test]
        public void Synergy_SourceTargeted_OnlyAffectsThatSource()
        {
            AddSource("m.src", ModifierTier.Synergy, 0.5d, SourceA);

            Assert.AreEqual(1.5d, Pipeline.GetSynergyMultiplier(SourceA));
            Assert.AreEqual(1d, Pipeline.GetSynergyMultiplier(SourceB));
        }

        [Test]
        public void Synergy_IdleChannelModifier_DoesNotAffectPlinkoSource()
        {
            AddChannel("m.idle", IncomeChannel.Idle, ModifierTier.Synergy, 0.5d);

            Assert.AreEqual(1d, Pipeline.GetSynergyMultiplier(PlinkoSource));
        }

        [Test]
        public void Synergy_BothChannelModifier_AffectsIdleAndPlinkoSources()
        {
            AddChannel("m.both", IncomeChannel.Both, ModifierTier.Synergy, 0.5d);

            Assert.AreEqual(1.5d, Pipeline.GetSynergyMultiplier(SourceA));
            Assert.AreEqual(1.5d, Pipeline.GetSynergyMultiplier(PlinkoSource));
        }

        [Test]
        public void Synergy_PlinkoChannelModifier_DoesNotAffectIdleSource()
        {
            AddChannel("m.plinko", IncomeChannel.Plinko, ModifierTier.Synergy, 1d);

            Assert.AreEqual(1d, Pipeline.GetSynergyMultiplier(SourceA));
            Assert.AreEqual(2d, Pipeline.GetSynergyMultiplier(PlinkoSource));
        }

        [Test]
        public void Synergy_UnknownSource_IsOne()
        {
            AddChannel("m.both", IncomeChannel.Both, ModifierTier.Synergy, 1d);

            Assert.AreEqual(1d, Pipeline.GetSynergyMultiplier("missing"));
        }

        [Test]
        public void Boost_BothAndPlinkoModifiers_ClampPlinkoAndLeaveIdleAtTwo()
        {
            AddChannel("m.tea", IncomeChannel.Both, ModifierTier.Boost, 1d);
            AddChannel("m.ad", IncomeChannel.Plinko, ModifierTier.Boost, 1d);

            Assert.AreEqual(System.Math.Min(3d, Config.BoostTierCap), Pipeline.GetTierMultiplier(ModifierTier.Boost, IncomeChannel.Plinko));
            Assert.AreEqual(3d, Pipeline.GetTierMultiplier(ModifierTier.Boost, IncomeChannel.Plinko));
            Assert.AreEqual(2d, Pipeline.GetTierMultiplier(ModifierTier.Boost, IncomeChannel.Idle));
        }

        [Test]
        public void Boost_ThreeBothModifiers_ClampToBoostCap()
        {
            AddChannel("m.one", IncomeChannel.Both, ModifierTier.Boost, 1d);
            AddChannel("m.two", IncomeChannel.Both, ModifierTier.Boost, 1d);
            AddChannel("m.three", IncomeChannel.Both, ModifierTier.Boost, 1d);

            Assert.AreEqual(Config.BoostTierCap, Pipeline.GetTierMultiplier(ModifierTier.Boost, IncomeChannel.Idle));
        }

        [Test]
        public void Prestige_SumAboveCap_ClampsToPrestigeCap()
        {
            AddChannel("m.one", IncomeChannel.Both, ModifierTier.Prestige, 20d);

            Assert.AreEqual(Config.PrestigeTierCap, Pipeline.GetTierMultiplier(ModifierTier.Prestige, IncomeChannel.Idle));
            Assert.AreEqual(Config.PrestigeTierCap, Pipeline.GetTierMultiplier(ModifierTier.Prestige, IncomeChannel.Plinko));
        }

        [Test]
        public void Global_SumAboveCap_ClampsToGlobalCap()
        {
            AddChannel("event.test", IncomeChannel.Both, ModifierTier.Global, 5d);

            Assert.AreEqual(Config.GlobalTierCap, Pipeline.GetTierMultiplier(ModifierTier.Global, IncomeChannel.Idle));
            Assert.AreEqual(Config.GlobalTierCap, Pipeline.GetTierMultiplier(ModifierTier.Global, IncomeChannel.Plinko));
        }

        [Test]
        public void Caps_AreReadFromConfig()
        {
            Config.SetBoostTierCapForTest(1.5d);
            Pipeline = new IncomePipeline(Config);
            AddChannel("m.one", IncomeChannel.Both, ModifierTier.Boost, 1d);

            Assert.AreEqual(1.5d, Pipeline.GetTierMultiplier(ModifierTier.Boost, IncomeChannel.Idle));
        }

        [Test]
        public void Cache_RegisterAfterRead_UpdatesMultiplier()
        {
            Assert.AreEqual(1d, Pipeline.GetTierMultiplier(ModifierTier.Boost, IncomeChannel.Idle));

            AddChannel("m.one", IncomeChannel.Idle, ModifierTier.Boost, 1d);

            Assert.AreEqual(2d, Pipeline.GetTierMultiplier(ModifierTier.Boost, IncomeChannel.Idle));
        }

        [Test]
        public void Unregister_RemovesAllModifiersOfSource_AndRestoresOne()
        {
            AddChannel("m.one", IncomeChannel.Idle, ModifierTier.Synergy, 1d);
            AddChannel("m.one", IncomeChannel.Idle, ModifierTier.Boost, 1d);
            AddChannel("m.two", IncomeChannel.Idle, ModifierTier.Synergy, 0.5d);
            Pipeline.GetSynergyMultiplier(SourceA);

            int removed = Pipeline.Unregister("m.one");

            Assert.AreEqual(2, removed);
            Assert.AreEqual(1.5d, Pipeline.GetSynergyMultiplier(SourceA));
            Assert.AreEqual(1d, Pipeline.GetTierMultiplier(ModifierTier.Boost, IncomeChannel.Idle));
        }

        [Test]
        public void Unregister_UnknownId_RemovesNothingAndStaysClean()
        {
            Pipeline.GetSynergyMultiplier(SourceA);

            Assert.AreEqual(0, Pipeline.Unregister("missing"));
            Assert.IsFalse(Pipeline.IsDirty);
        }

        [Test]
        public void DirtyFlag_SetByRegister_ClearedByRead()
        {
            Pipeline.GetSynergyMultiplier(SourceA);
            Assert.IsFalse(Pipeline.IsDirty);

            AddChannel("m.one", IncomeChannel.Idle, ModifierTier.Synergy, 1d);
            Assert.IsTrue(Pipeline.IsDirty);

            Pipeline.GetSynergyMultiplier(SourceA);
            Assert.IsFalse(Pipeline.IsDirty);
        }

        [Test]
        public void GetTierMultiplier_BaseSynergyOrBothChannel_IsOne()
        {
            AddChannel("m.one", IncomeChannel.Both, ModifierTier.Synergy, 1d);
            AddChannel("m.two", IncomeChannel.Both, ModifierTier.Boost, 1d);

            Assert.AreEqual(1d, Pipeline.GetTierMultiplier(ModifierTier.Synergy, IncomeChannel.Idle));
            Assert.AreEqual(1d, Pipeline.GetTierMultiplier(ModifierTier.Base, IncomeChannel.Idle));
            Assert.AreEqual(1d, Pipeline.GetTierMultiplier(ModifierTier.Boost, IncomeChannel.Both));
        }

        [Test]
        public void RegisterModifier_MalformedInput_IsRejected()
        {
            Assert.IsFalse(AddChannel(null, IncomeChannel.Idle, ModifierTier.Boost, 1d));
            Assert.IsFalse(AddChannel(string.Empty, IncomeChannel.Idle, ModifierTier.Boost, 1d));
            Assert.IsFalse(AddChannel("m.bad", (IncomeChannel)99, ModifierTier.Boost, 1d));
            Assert.IsFalse(AddChannel("m.bad", IncomeChannel.Idle, (ModifierTier)99, 1d));
            Assert.IsFalse(AddSource("m.bad", ModifierTier.Synergy, 1d, null));
            Assert.IsFalse(AddSource("m.bad", ModifierTier.Synergy, 1d, string.Empty));
        }
    }
}
