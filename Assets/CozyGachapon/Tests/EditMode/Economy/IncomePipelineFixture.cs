using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using NUnit.Framework;
using Object = UnityEngine.Object;

namespace CozyGachapon.Tests.Economy
{
    public abstract class IncomePipelineFixture
    {
        protected const string SourceA = "shelf.a";
        protected const string SourceB = "shelf.b";
        protected const string PlinkoSource = EconomyIds.PlinkoCoinSource;
        protected const double SynergyCap = 4d;
        protected const double BoostCap = 3d;
        protected const double PrestigeCap = 10d;
        protected const double GlobalCap = 2d;

        protected EconomyConfig Config;
        protected IncomePipeline Pipeline;

        [SetUp]
        public void SetUpPipeline()
        {
            Config = UnityEngine.ScriptableObject.CreateInstance<EconomyConfig>();
            Config.SetSynergyTierCapForTest(SynergyCap);
            Config.SetBoostTierCapForTest(BoostCap);
            Config.SetPrestigeTierCapForTest(PrestigeCap);
            Config.SetGlobalTierCapForTest(GlobalCap);
            Config.TierOwnerPrefixes.SetBaseForTest(new[] { "shelf.", "base.", "flat", "bad." });
            Config.TierOwnerPrefixes.SetSynergyForTest(new[] { "shelf.", "syn.", "syn", "m.", "good", "bad." });
            Config.TierOwnerPrefixes.SetBoostForTest(new[] { "boost.", "ad.", "m.", "many.", "good", "bad." });
            Config.TierOwnerPrefixes.SetPrestigeForTest(new[] { "prestige.", "prestige", "m.", "bad." });
            Config.TierOwnerPrefixes.SetGlobalForTest(new[] { "event.", "global", "m.", "bad." });
            Pipeline = new IncomePipeline(Config);
            Pipeline.RegisterIncomeSource(SourceA, IncomeChannel.Idle, 50d);
            Pipeline.RegisterIncomeSource(SourceB, IncomeChannel.Idle, 20d);
            Pipeline.RegisterIncomeSource(PlinkoSource, IncomeChannel.Plinko, 0d);
        }

        [TearDown]
        public void TearDownPipeline()
        {
            Object.DestroyImmediate(Config);
        }

        protected bool AddChannel(string id, IncomeChannel channel, ModifierTier tier, double value)
        {
            IncomeModifier modifier = IncomeModifier.ForChannel(id, channel, tier, value);
            return Pipeline.RegisterModifier(in modifier);
        }

        protected bool AddSource(string id, ModifierTier tier, double value, string targetKey)
        {
            IncomeModifier modifier = IncomeModifier.ForSource(id, IncomeChannel.Idle, tier, value, targetKey);
            return Pipeline.RegisterModifier(in modifier);
        }
    }
}
