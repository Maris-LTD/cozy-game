using System;
using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using CozyGachapon.Tests.Pooling;
using NUnit.Framework;

namespace CozyGachapon.Tests.Economy
{
    [TestFixture]
    public sealed class EconomyFrameAllocationTests
    {
        [Test]
        public void AC43_600Frames_IdleAccrual_24CoinsPerSec_AndFormatting_AllocatesZeroBytes()
        {
            EconomyConfig config = EconomyTestAssets.LoadConfig();
            CountingEconomyDiagnosticsSink sink = new CountingEconomyDiagnosticsSink();
            FakeBootRegistry registry = new FakeBootRegistry();
            FakeSimulationState simState = new FakeSimulationState(true, false);

            EconomyBootModule module = new EconomyBootModule(config, sink, registry, simState);
            module.BootStep.Run();
            module.HandleInteractive(default);

            EconomyCore core = module.Core;
            NumberFormatter formatter = module.Formatter;

            core.IncomePipeline.RegisterIncomeSource("shelf.slot1.idle", IncomeChannel.Idle, 25d);
            IncomeModifier synergyMod = IncomeModifier.ForSource("shelf.syn1", IncomeChannel.Both, ModifierTier.Synergy, 0.5d, "shelf.slot1.idle");
            core.IncomePipeline.RegisterModifier(in synergyMod);

            registry.TryGet<IBaseCoinValueSource>(out IBaseCoinValueSource baseValueSource);

            char[] amountBuffer = new char[formatter.MaxLength];
            char[] idleRateBuffer = new char[formatter.MaxLength];
            char[] plinkoRateBuffer = new char[formatter.MaxLength];

            CoinResolvedEvent resolvedCoin = new CoinResolvedEvent(
                "coin.steady",
                CoinOrigin.Auto,
                1,
                2d,
                2d,
                4,
                CoinResolveReason.Bucket);

            const double dt = 1d / 60d;
            StepFrame(core, formatter, baseValueSource, in resolvedCoin, dt, true, amountBuffer, idleRateBuffer, plinkoRateBuffer);

            int allocations = GcAllocProbe.CountAllocations(() =>
            {
                for (int frame = 0; frame < 600; frame++)
                {
                    bool resolveCoinThisFrame = (frame % 5 == 0) || (frame % 5 == 2);
                    StepFrame(core, formatter, baseValueSource, in resolvedCoin, dt, resolveCoinThisFrame, amountBuffer, idleRateBuffer, plinkoRateBuffer);
                }
            });

            Assert.That(allocations, Is.EqualTo(0));
        }

        private static void StepFrame(
            EconomyCore core,
            NumberFormatter formatter,
            IBaseCoinValueSource baseValueSource,
            in CoinResolvedEvent resolvedCoin,
            double dt,
            bool resolveCoin,
            char[] amountBuffer,
            char[] idleRateBuffer,
            char[] plinkoRateBuffer)
        {
            _ = baseValueSource.BaseCoinValue;
            if (resolveCoin)
            {
                core.HandleCoinResolved(in resolvedCoin);
            }

            core.Tick(dt);

            Span<char> amountSpan = new Span<char>(amountBuffer);
            Span<char> idleSpan = new Span<char>(idleRateBuffer);
            Span<char> plinkoSpan = new Span<char>(plinkoRateBuffer);

            _ = formatter.TryFormatAmount(core.GetBalance(EconomyIds.Coin), amountSpan);
            _ = formatter.TryFormatRate(core.IncomePipeline.IdleCps, idleSpan);
            _ = formatter.TryFormatRate(core.PlinkoRateAvg, plinkoSpan);
        }
    }
}
