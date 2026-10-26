using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using NUnit.Framework;

namespace CozyGachapon.Tests.Economy
{
    [TestFixture]
    public sealed class SpinCountIntegrityTests
    {
        private const int InitialSpins = 5;

        [Test]
        public void AC35_AllPublicApisExceptSuccessfulTryPaySpinAndReset_KeepSpinsThisShopAndPriceUnchanged()
        {
            EconomyConfig config = EconomyTestAssets.LoadConfig();
            CountingEconomyDiagnosticsSink sink = new CountingEconomyDiagnosticsSink();
            FakeSimulationState simState = new FakeSimulationState(true, false);

            EconomyStateData loaded = new EconomyStateData(
                new[] { new CurrencyBalanceEntry(EconomyIds.Coin, 50d) },
                InitialSpins);

            EconomyCore core = new EconomyCore(sink, simState);
            Assert.That(core.Initialize(config, ReleaseTier.Alpha, loaded, simState), Is.True);

            double initialPrice = core.SpinPrice;
            double initialBaseCoinValue = core.BaseCoinValue;
            Assert.That(core.SpinsThisShop, Is.EqualTo(InitialSpins));

            PlayerActionIssuer issuer = new PlayerActionIssuer();
            PlayerActionContext validContext = issuer.Issue(PlayerActionKind.GachaSpin);
            PlayerActionContext invalidContext = default;

            Assert.That(core.TryPaySpin(in invalidContext), Is.False);
            AssertUnchanged(core, initialPrice, initialBaseCoinValue);

            Assert.That(core.TryPaySpin(in validContext), Is.False);
            AssertUnchanged(core, initialPrice, initialBaseCoinValue);

            Assert.That(core.TrySpend(EconomyIds.Coin, 10d, SpendReason.ShopPurchase, in validContext), Is.True);
            AssertUnchanged(core, initialPrice, initialBaseCoinValue);

            Assert.That(core.Add(EconomyIds.Coin, 20d, WalletChangeReason.AdReward), Is.True);
            AssertUnchanged(core, initialPrice, initialBaseCoinValue);

            _ = core.CanAfford(EconomyIds.Coin, 10d);
            _ = core.CanAffordSpin();
            _ = core.GetSpinPrice();
            _ = core.GetDerivedPrice(1.5d);
            _ = core.TryGetDerivedPrice(1.5d, out _);
            AssertUnchanged(core, initialPrice, initialBaseCoinValue);

            core.IncomePipeline.RegisterIncomeSource("shelf.slot1.idle", IncomeChannel.Idle, 10d);
            core.IncomePipeline.RegisterIncomeSource("shelf.slot1.idle", 12d);
            IncomeModifier mod = IncomeModifier.ForSource("shelf.syn1", IncomeChannel.Idle, ModifierTier.Synergy, 0.5d, "shelf.slot1.idle");
            core.IncomePipeline.RegisterModifier(in mod);
            _ = core.GetSourceIdleCps("shelf.slot1.idle");
            _ = core.PreviewIdleCps(new FakeIdlePlan());
            AssertUnchanged(core, initialPrice, initialBaseCoinValue);

            CoinResolvedEvent coinEvt = new CoinResolvedEvent("coin.1", CoinOrigin.Manual, 0, 1d, 5d, 2, CoinResolveReason.Bucket);
            Assert.That(core.HandleCoinResolved(in coinEvt), Is.True);
            Assert.That(core.SubmitIncome(IncomeChannel.Plinko, "shelf.slot1", 5d), Is.True);
            AssertUnchanged(core, initialPrice, initialBaseCoinValue);

            _ = core.Tick(1d / 60d);
            core.HandlePaused(new PausedEvent(default));
            core.HandleResumed(new ResumedEvent(default, 5d));
            core.HandleInteractive(default);
            _ = core.HandleAdClosed(new AdClosedEvent(10d));
            _ = core.ExportState();
            AssertUnchanged(core, initialPrice, initialBaseCoinValue);

            core.IncomePipeline.Unregister("shelf.syn1");
            core.IncomePipeline.UnregisterIncomeSource("shelf.slot1.idle");
            AssertUnchanged(core, initialPrice, initialBaseCoinValue);
        }

        private static void AssertUnchanged(EconomyCore core, double expectedPrice, double expectedBaseCoinValue)
        {
            Assert.That(core.SpinsThisShop, Is.EqualTo(InitialSpins));
            Assert.That(core.SpinPrice, Is.EqualTo(expectedPrice));
            Assert.That(core.BaseCoinValue, Is.EqualTo(expectedBaseCoinValue));
        }
    }
}
