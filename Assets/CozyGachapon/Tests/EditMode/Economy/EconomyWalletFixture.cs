using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Economy
{
    public abstract class EconomyWalletFixture
    {
        protected const int RecorderCapacity = 1024;

        protected EconomyConfig Config { get; private set; }
        protected CountingEconomyDiagnosticsSink Sink { get; private set; }
        protected EconomyEventRecorder Recorder { get; private set; }
        protected WalletService Wallet { get; private set; }
        protected double RelTolerance { get; private set; }

        [SetUp]
        public void SetUpWallet()
        {
            EconomyBalanceTargets targets = EconomyTestAssets.LoadTargets();
            Assert.IsNotNull(targets);
            RelTolerance = targets.ValueRelTolerance;
            Config = ScriptableObject.CreateInstance<EconomyConfig>();
            Sink = new CountingEconomyDiagnosticsSink();
            Recorder = new EconomyEventRecorder(RecorderCapacity);
            Wallet = new WalletService(Config, Sink);
            Assert.IsTrue(Wallet.RegisterCurrency(EconomyIds.Coin));
            Wallet.WalletChanged += Recorder.OnWalletChanged;
        }

        [TearDown]
        public void TearDownWallet()
        {
            Wallet.WalletChanged -= Recorder.OnWalletChanged;
            Object.DestroyImmediate(Config);
        }

        protected void AssertBalance(double expected, string currencyId)
        {
            double tolerance = EconomyTestAssets.RelativeTolerance(expected, RelTolerance);
            Assert.AreEqual(expected, Wallet.GetBalance(currencyId), tolerance);
        }

        protected void AssertEventIntegrity(WalletChangedEvent change)
        {
            double tolerance = EconomyTestAssets.RelativeTolerance(change.Current, RelTolerance);
            Assert.AreEqual(change.Current, change.Previous + change.Delta, tolerance);
        }
    }
}
