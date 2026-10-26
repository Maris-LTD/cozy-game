using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Economy
{
    [TestFixture]
    public sealed class EconomyBootStepTests
    {
        [Test]
        public void AC46_ValidConfig_RegistersStep_TransitionsToReadyOnRun_RegistersService_AndWaitsForInteractiveToAccrueIdle()
        {
            EconomyConfig config = EconomyTestAssets.LoadConfig();
            CountingEconomyDiagnosticsSink sink = new CountingEconomyDiagnosticsSink();
            FakeBootRegistry registry = new FakeBootRegistry();
            FakeSimulationState simState = new FakeSimulationState(false, false);

            EconomyBootModule module = new EconomyBootModule(config, sink, registry, simState);
            Assert.That(module.RegisterSteps(registry.RegisterStep), Is.True);

            Assert.That(registry.CountStepsById(EconomyBootStep.StepId), Is.EqualTo(1));
            IBootStep step = registry.Steps[0];
            Assert.That(step.Id, Is.EqualTo("economy.core"));
            Assert.That(step.Phase, Is.EqualTo(BootPhase.GameplayInit));
            Assert.That(step.RunsAfter.Count, Is.EqualTo(0));
            Assert.That(step.IsCritical, Is.True);

            PlayerActionIssuer issuer = new PlayerActionIssuer();
            PlayerActionContext validContext = issuer.Issue(PlayerActionKind.ShopPurchase);

            Assert.That(module.Core.RunState, Is.EqualTo(EconomyRunState.Uninitialized));
            Assert.That(module.Core.Add(EconomyIds.Coin, 50d, WalletChangeReason.AdReward), Is.False);
            Assert.That(module.Core.TrySpend(EconomyIds.Coin, 10d, SpendReason.ShopPurchase, in validContext), Is.False);
            Assert.That(registry.TryGet<IBaseCoinValueSource>(out _), Is.False);

            step.Run();

            Assert.That(step.IsDone, Is.True);
            Assert.That(step.IsFailed, Is.False);
            Assert.That(module.Core.RunState, Is.EqualTo(EconomyRunState.Ready));

            Assert.That(module.Core.Add(EconomyIds.Coin, 100d, WalletChangeReason.AdReward), Is.True);
            Assert.That(module.Core.TrySpend(EconomyIds.Coin, 25d, SpendReason.ShopPurchase, in validContext), Is.True);
            Assert.That(module.Core.GetBalance(EconomyIds.Coin), Is.EqualTo(75d));

            Assert.That(registry.GetRegistrationCount<IBaseCoinValueSource>(), Is.EqualTo(1));
            Assert.That(registry.TryGet<IBaseCoinValueSource>(out IBaseCoinValueSource registeredSource), Is.True);
            Assert.That(registeredSource, Is.SameAs(module.Core));

            module.Core.IncomePipeline.RegisterIncomeSource("shelf.slot1.idle", IncomeChannel.Idle, 30d);
            Assert.That(module.Core.IncomePipeline.IdleCps, Is.GreaterThan(0d));

            double balanceBeforeTicks = module.Core.GetBalance(EconomyIds.Coin);
            for (int i = 0; i < 60; i++)
            {
                module.Tick(1d / 60d);
            }

            Assert.That(module.Core.GetBalance(EconomyIds.Coin), Is.EqualTo(balanceBeforeTicks));

            simState.IsSimulationRunning = true;
            module.HandleInteractive(default);
            Assert.That(module.Core.RunState, Is.EqualTo(EconomyRunState.Running));

            for (int i = 0; i < 60; i++)
            {
                module.Tick(1d / 60d);
            }

            Assert.That(module.Core.GetBalance(EconomyIds.Coin), Is.EqualTo(balanceBeforeTicks + 30d).Within(1e-6d));
        }

        [Test]
        public void AC46_ConfigMissingCoinCurrency_FailsStepAndLeavesBalanceZero()
        {
            EconomyConfig brokenConfig = ScriptableObject.CreateInstance<EconomyConfig>();
            CurrencyDefinition stampOnly = EconomyTestAssets.LoadShopStamp();
            brokenConfig.SetCurrenciesForTest(new[] { stampOnly });
            brokenConfig.SetStartingCoinsForTest(100d);

            CountingEconomyDiagnosticsSink sink = new CountingEconomyDiagnosticsSink();
            FakeBootRegistry registry = new FakeBootRegistry();
            FakeSimulationState simState = new FakeSimulationState(false, false);

            EconomyBootModule module = new EconomyBootModule(brokenConfig, sink, registry, simState);
            module.RegisterSteps(registry.RegisterStep);

            IBootStep step = registry.Steps[0];
            step.Run();

            UnityEngine.Object.DestroyImmediate(brokenConfig);

            Assert.That(step.IsFailed, Is.True);
            Assert.That(step.IsDone, Is.False);
            Assert.That(module.Core.RunState, Is.EqualTo(EconomyRunState.Uninitialized));
            Assert.That(module.Core.GetBalance(EconomyIds.Coin), Is.EqualTo(0d));
            Assert.That(registry.TryGet<IBaseCoinValueSource>(out _), Is.False);
        }
    }
}
