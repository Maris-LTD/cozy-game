using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Economy
{
    [TestFixture]
    public sealed class WalletEventIntegrityTests
    {
        private EconomyConfig _config;
        private CurrencyDefinition _coinDef;
        private CountingEconomyDiagnosticsSink _sink;
        private FakeSimulationState _simulationState;
        private EconomyCore _core;
        private PlayerActionIssuer _issuer;
        private RecordingWalletEvents _recorder;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<EconomyConfig>();
            _coinDef = ScriptableObject.CreateInstance<CurrencyDefinition>();
            _coinDef.SetIdForTest(EconomyIds.Coin);
            _config.SetCurrenciesForTest(new[] { _coinDef });
            _config.SetStartingCoinsForTest(0d);
            _config.SetWalletSafeCapForTest(1e300d);
            _config.SetSpinBasePriceForTest(100d);
            _config.SetSpinGrowthForTest(1.05d);
            _config.SetPriceNiceSigDigitsForTest(2);
            _config.SetIdleCpsUnitForTest(2d);
            _config.SetMaxFrameDeltaForTest(0.25d);

            _sink = new CountingEconomyDiagnosticsSink();
            _simulationState = new FakeSimulationState(true, false);
            _issuer = new PlayerActionIssuer();
            _core = new EconomyCore(_sink, _simulationState);
            _core.Initialize(_config, ReleaseTier.Mvp, null, _simulationState);
            _core.IncomePipeline.RegisterIncomeSource("shelf.slot1.idle", IncomeChannel.Idle, 10d);

            _recorder = new RecordingWalletEvents();
            _core.WalletChanged += _recorder.OnWalletChanged;
            _core.IncomeCredited += _recorder.OnIncomeCredited;
            _core.SpendRejected += _recorder.OnSpendRejected;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_coinDef);
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void AC39_60SecondSession_AllWalletEventsMaintainIntegrityAndPerFrameConservation()
        {
            const int totalFrames = 3600;
            const double dt = 1d / 60d;
            int plinkoCoinsResolved = 0;
            int spinsPaid = 0;

            for (int frame = 0; frame < totalFrames; frame++)
            {
                _recorder.Clear();
                double frameSpend = 0d;

                if (frame == 0)
                {
                    PlayerActionContext rejectCtx = _issuer.Issue(PlayerActionKind.GachaSpin);
                    bool rejected = _core.TryPaySpin(in rejectCtx);
                    Assert.IsFalse(rejected);
                    Assert.AreEqual(1, _recorder.SpendRejections.Count);
                }

                if (frame % 18 == 0 && plinkoCoinsResolved < 200)
                {
                    CoinResolvedEvent coinEvt = new CoinResolvedEvent(
                        "coin.sim",
                        CoinOrigin.Manual,
                        0,
                        1d,
                        2d,
                        3,
                        CoinResolveReason.Bucket);
                    Assert.IsTrue(_core.HandleCoinResolved(in coinEvt));
                    plinkoCoinsResolved++;
                }

                _core.Tick(dt);

                if ((frame == 1200 || frame == 2400 || frame == 3599) && spinsPaid < 3)
                {
                    double price = _core.SpinPrice;
                    PlayerActionContext spinCtx = _issuer.Issue(PlayerActionKind.GachaSpin);
                    Assert.IsTrue(_core.TryPaySpin(in spinCtx));
                    frameSpend += price;
                    spinsPaid++;
                }

                int idleEventCount = 0;
                double frameWalletDeltaSum = 0d;
                double frameIdlePortion = 0d;

                for (int i = 0; i < _recorder.WalletChanges.Count; i++)
                {
                    WalletChangedEvent evt = _recorder.WalletChanges[i];
                    Assert.IsTrue(WalletChangeReasonValidator.IsValid(evt.Reason));
                    Assert.AreEqual(
                        evt.Current - evt.Previous,
                        evt.Delta,
                        1e-9d);

                    frameWalletDeltaSum += evt.Delta;
                    if (evt.Reason == WalletChangeReason.IdleIncome)
                    {
                        idleEventCount++;
                        frameIdlePortion += evt.Delta;
                    }
                }

                Assert.LessOrEqual(idleEventCount, 1);

                double frameCreditedSum = 0d;
                for (int i = 0; i < _recorder.IncomeCredits.Count; i++)
                {
                    frameCreditedSum += _recorder.IncomeCredits[i].CreditedAmount;
                }

                double expectedFrameDelta = frameCreditedSum + frameIdlePortion - frameSpend;
                Assert.AreEqual(expectedFrameDelta, frameWalletDeltaSum, 1e-8d);
            }

            Assert.AreEqual(200, plinkoCoinsResolved);
            Assert.AreEqual(3, spinsPaid);
        }
    }
}
