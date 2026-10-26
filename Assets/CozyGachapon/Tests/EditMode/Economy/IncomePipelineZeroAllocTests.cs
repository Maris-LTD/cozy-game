using CozyGachapon.Contracts;
using CozyGachapon.Tests.Pooling;
using NUnit.Framework;

namespace CozyGachapon.Tests.Economy
{
    public class IncomePipelineZeroAllocTests : IncomePipelineFixture
    {
        private const int Iterations = 100;
        private const int PreloadedModifiers = 10;
        private const string TransientId = "transient.boost";

        [Test]
        public void ComputePath_IncludingRecomputeAfterDirty_DoesNotAllocate()
        {
            for (int i = 0; i < PreloadedModifiers; i++)
            {
                AddChannel("preload." + i, IncomeChannel.Both, ModifierTier.Synergy, 0.01d);
            }

            AddSource("flat", ModifierTier.Base, 2d, SourceA);
            AddChannel("boost", IncomeChannel.Both, ModifierTier.Boost, 0.5d);
            IncomeModifier transient = IncomeModifier.ForChannel(TransientId, IncomeChannel.Idle, ModifierTier.Boost, 0.5d);

            int allocations = GcAllocProbe.CountAllocations(() => RunCompute(transient));

            Assert.AreEqual(0, allocations);
        }

        private void RunCompute(IncomeModifier transient)
        {
            for (int i = 0; i < Iterations; i++)
            {
                Pipeline.RegisterModifier(in transient);
                Pipeline.GetSynergyMultiplier(SourceA);
                Pipeline.Unregister(TransientId);
                Pipeline.GetTierMultiplier(ModifierTier.Boost, IncomeChannel.Idle);
                Pipeline.GetTierMultiplier(ModifierTier.Prestige, IncomeChannel.Plinko);
                Pipeline.GetTierMultiplier(ModifierTier.Global, IncomeChannel.Idle);
                Pipeline.GetFlatUnits(SourceA);
                Pipeline.GetChannelIncome(SourceA, 5d);
                Pipeline.GetChannelIncome(PlinkoSource, 1d);
            }
        }
    }
}
