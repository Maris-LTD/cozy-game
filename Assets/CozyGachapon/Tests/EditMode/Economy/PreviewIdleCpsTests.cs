using System.Collections.Generic;
using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Economy
{
    [TestFixture]
    public sealed class PreviewIdleCpsTests
    {
        private const string SourceA = "shelf.slot1.idle";
        private const string SourceB = "shelf.slot2.idle";
        private const string SourceC = "shelf.slot3.idle";
        private EconomyConfig _config;
        private CurrencyDefinition _coinDef;
        private CountingEconomyDiagnosticsSink _sink;
        private FakeSimulationState _simulationState;
        private EconomyCore _core;
        private List<WalletChangedEvent> _walletEvents;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<EconomyConfig>();
            _coinDef = ScriptableObject.CreateInstance<CurrencyDefinition>();
            _coinDef.SetIdForTest(EconomyIds.Coin);
            _config.SetCurrenciesForTest(new[] { _coinDef });
            _config.SetStartingCoinsForTest(100d);
            _config.SetWalletSafeCapForTest(1e300d);
            _config.SetIdleCpsUnitForTest(2d);
            _config.SetSynergyTierCapForTest(4d);
            _config.SetBoostTierCapForTest(3d);

            _sink = new CountingEconomyDiagnosticsSink();
            _simulationState = new FakeSimulationState(false);
            _core = new EconomyCore(_sink, _simulationState);
            _core.Initialize(_config, ReleaseTier.Mvp, null, _simulationState);

            _core.IncomePipeline.RegisterIncomeSource(SourceA, 10d);
            _core.IncomePipeline.RegisterIncomeSource(SourceB, 4d);

            IncomeModifier synA = IncomeModifier.ForSource("shelf.synA", IncomeChannel.Idle, ModifierTier.Synergy, 0.5d, SourceA);
            IncomeModifier comboA = IncomeModifier.ForSource("shelf.comboA", IncomeChannel.Idle, ModifierTier.Synergy, 0.5d, SourceA);
            IncomeModifier flatB = IncomeModifier.ForSource("shelf.flatB", IncomeChannel.Idle, ModifierTier.Base, 1.0d, SourceB);
            IncomeModifier comboB = IncomeModifier.ForSource("shelf.comboB", IncomeChannel.Idle, ModifierTier.Synergy, 0.5d, SourceB);
            IncomeModifier tea = IncomeModifier.ForChannel("boost.tea", IncomeChannel.Both, ModifierTier.Boost, 1.0d, true);

            _core.IncomePipeline.RegisterModifier(in synA);
            _core.IncomePipeline.RegisterModifier(in comboA);
            _core.IncomePipeline.RegisterModifier(in flatB);
            _core.IncomePipeline.RegisterModifier(in comboB);
            _core.IncomePipeline.RegisterModifier(in tea);

            _walletEvents = new List<WalletChangedEvent>();
            _core.WalletChanged += _walletEvents.Add;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_coinDef);
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void AC51_PreviewIdleCps_AllCasesInReadyRunningAndPaused_PureWithoutMutatingState()
        {
            Assert.AreEqual(58d, _core.IncomePipeline.IdleCps, 1e-4d);

            Assert.AreEqual(EconomyRunState.Ready, _core.RunState);
            VerifyAllPlanCases();

            _simulationState.IsSimulationRunning = true;
            _core.HandleInteractive(new InteractiveEvent(0.5f));
            Assert.AreEqual(EconomyRunState.Running, _core.RunState);
            VerifyAllPlanCases();

            _simulationState.IsSimulationRunning = false;
            _core.HandlePaused(new PausedEvent(PauseReason.Hidden));
            Assert.AreEqual(EconomyRunState.Paused, _core.RunState);
            VerifyAllPlanCases();
        }

        private void VerifyAllPlanCases()
        {
            int recomputeBefore = _core.IncomePipeline.IdleRecomputeCount;
            double balanceBefore = _core.GetBalance(EconomyIds.Coin);

            FakeIdlePlan planCurrent = CreateCurrentPlan();
            Assert.AreEqual(58d, _core.PreviewIdleCps(planCurrent), 1e-4d);

            FakeIdlePlan planAddC = CreateCurrentPlan();
            planAddC.AddSource(SourceC, 4d, 0d);
            Assert.AreEqual(66d, _core.PreviewIdleCps(planAddC), 1e-4d);

            FakeIdlePlan planRemoveB = new FakeIdlePlan();
            planRemoveB.AddSource(SourceA, 10d, 0d);
            planRemoveB.AddModifier(IncomeModifier.ForSource("shelf.synA", IncomeChannel.Idle, ModifierTier.Synergy, 0.5d, SourceA));
            planRemoveB.AddModifier(IncomeModifier.ForSource("shelf.comboA", IncomeChannel.Idle, ModifierTier.Synergy, 0.5d, SourceA));
            Assert.AreEqual(40d, _core.PreviewIdleCps(planRemoveB), 1e-4d);

            FakeIdlePlan planBadInputs = new FakeIdlePlan();
            planBadInputs.AddSource(SourceA, 10d, 0d);
            planBadInputs.AddSource(SourceC, double.NaN, -3d);
            planBadInputs.AddModifier(IncomeModifier.ForSource("shelf.synA", IncomeChannel.Idle, ModifierTier.Synergy, 0.5d, SourceA));
            planBadInputs.AddModifier(IncomeModifier.ForSource("shelf.comboA", IncomeChannel.Idle, ModifierTier.Synergy, 0.5d, SourceA));
            planBadInputs.AddModifier(IncomeModifier.ForSource("shelf.badMod", IncomeChannel.Idle, ModifierTier.Synergy, -0.5d, SourceA));

            FakeIdlePlan planZeroEquiv = new FakeIdlePlan();
            planZeroEquiv.AddSource(SourceA, 10d, 0d);
            planZeroEquiv.AddSource(SourceC, 0d, 0d);
            planZeroEquiv.AddModifier(IncomeModifier.ForSource("shelf.synA", IncomeChannel.Idle, ModifierTier.Synergy, 0.5d, SourceA));
            planZeroEquiv.AddModifier(IncomeModifier.ForSource("shelf.comboA", IncomeChannel.Idle, ModifierTier.Synergy, 0.5d, SourceA));
            planZeroEquiv.AddModifier(IncomeModifier.ForSource("shelf.badMod", IncomeChannel.Idle, ModifierTier.Synergy, 0d, SourceA));

            Assert.AreEqual(_core.PreviewIdleCps(planZeroEquiv), _core.PreviewIdleCps(planBadInputs), 1e-6d);
            Assert.AreEqual(40d, _core.PreviewIdleCps(planBadInputs), 1e-4d);

            Assert.AreEqual(0d, _core.PreviewIdleCps(null));
            Assert.AreEqual(0d, _core.PreviewIdleCps(new FakeIdlePlan()));

            Assert.AreEqual(recomputeBefore, _core.IncomePipeline.IdleRecomputeCount);
            Assert.AreEqual(58d, _core.IncomePipeline.IdleCps, 1e-4d);
            Assert.AreEqual(recomputeBefore, _core.IncomePipeline.IdleRecomputeCount);
            Assert.AreEqual(balanceBefore, _core.GetBalance(EconomyIds.Coin), 1e-6d);
            Assert.AreEqual(0, _walletEvents.Count);
        }

        private static FakeIdlePlan CreateCurrentPlan()
        {
            FakeIdlePlan plan = new FakeIdlePlan();
            plan.AddSource(SourceA, 10d, 0d);
            plan.AddSource(SourceB, 4d, 1d);
            plan.AddModifier(IncomeModifier.ForSource("shelf.synA", IncomeChannel.Idle, ModifierTier.Synergy, 0.5d, SourceA));
            plan.AddModifier(IncomeModifier.ForSource("shelf.comboA", IncomeChannel.Idle, ModifierTier.Synergy, 0.5d, SourceA));
            plan.AddModifier(IncomeModifier.ForSource("shelf.comboB", IncomeChannel.Idle, ModifierTier.Synergy, 0.5d, SourceB));
            return plan;
        }
    }
}
