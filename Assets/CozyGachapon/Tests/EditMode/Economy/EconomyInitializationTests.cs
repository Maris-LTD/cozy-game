using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using CozyGachapon.Tests.Pooling;
using NUnit.Framework;
using Object = UnityEngine.Object;

namespace CozyGachapon.Tests.Economy
{
    public class EconomyInitializationTests
    {
        private const int Iterations = 200;
        private const double StartingCoins = 50d;

        private CountingEconomyDiagnosticsSink _sink;
        private EconomyCore _core;
        private EconomyConfig _custom;

        [SetUp]
        public void SetUp()
        {
            _sink = new CountingEconomyDiagnosticsSink();
            _core = new EconomyCore(_sink);
        }

        [TearDown]
        public void TearDown()
        {
            if (_custom != null)
            {
                Object.DestroyImmediate(_custom);
            }
        }

        [Test]
        public void Initialize_DefaultConfigNewGameMvp_RegistersOnlyCoinWithStartingState()
        {
            EconomyConfig config = EconomyTestAssets.LoadConfig();

            Assert.IsTrue(_core.Initialize(config, ReleaseTier.Mvp, null));

            Assert.IsTrue(_core.IsInitialized);
            Assert.IsTrue(_core.IsCurrencyRegistered(EconomyIds.Coin));
            Assert.IsFalse(_core.IsCurrencyRegistered(EconomyIds.ShopStamp));
            EconomyStateTestData.AssertExactlyEqual(config.StartingCoins, _core.GetBalance(EconomyIds.Coin));
            Assert.AreEqual(0, _core.SpinsThisShop);
            EconomyStateTestData.AssertExactlyEqual(1d, _core.BaseCoinValue);
            EconomyStateTestData.AssertExactlyEqual(config.SpinBasePrice, _core.SpinPrice);
        }

        [Test]
        public void Initialize_Mvp_ShopStampIsNotQueryableOrAddable()
        {
            _core.Initialize(EconomyTestAssets.LoadConfig(), ReleaseTier.Mvp, null);

            Assert.IsFalse(_core.CanAfford(EconomyIds.ShopStamp, 1d));
            Assert.IsFalse(_core.Add(EconomyIds.ShopStamp, 1d, WalletChangeReason.AdReward));
            Assert.AreEqual(0d, _core.GetBalance(EconomyIds.ShopStamp));
        }

        [Test]
        public void Initialize_StartingCoinsFifty_CoinEqualsFifty()
        {
            _custom = EconomyStateTestData.CreateConfigWithCurrencies(StartingCoins);

            Assert.IsTrue(_core.Initialize(_custom, ReleaseTier.Mvp, null));

            EconomyStateTestData.AssertExactlyEqual(StartingCoins, _core.GetBalance(EconomyIds.Coin));
        }

        [Test]
        public void Initialize_AlphaTier_RegistersCoinAndShopStamp()
        {
            Assert.IsTrue(_core.Initialize(EconomyTestAssets.LoadConfig(), ReleaseTier.Alpha, null));

            Assert.IsTrue(_core.IsCurrencyRegistered(EconomyIds.Coin));
            Assert.IsTrue(_core.IsCurrencyRegistered(EconomyIds.ShopStamp));
            Assert.AreEqual(0d, _core.GetBalance(EconomyIds.ShopStamp));
        }

        [Test]
        public void Initialize_LoadedWithEmptyBalances_IsTreatedAsNewGame()
        {
            _custom = EconomyStateTestData.CreateConfigWithCurrencies(StartingCoins);
            EconomyStateData empty = EconomyStateTestData.Build(12);

            Assert.IsTrue(_core.Initialize(_custom, ReleaseTier.Mvp, empty));

            EconomyStateTestData.AssertExactlyEqual(StartingCoins, _core.GetBalance(EconomyIds.Coin));
            Assert.AreEqual(0, _core.SpinsThisShop);
            EconomyStateTestData.AssertExactlyEqual(1d, _core.BaseCoinValue);
        }

        [Test]
        public void BeforeInitialize_ReturnsSafeValues()
        {
            PlayerActionContext context = new PlayerActionIssuer().Issue(PlayerActionKind.GachaSpin);

            Assert.IsFalse(_core.IsInitialized);
            Assert.IsFalse(_core.Add(EconomyIds.Coin, 1d, WalletChangeReason.AdReward));
            Assert.IsFalse(_core.CanAfford(EconomyIds.Coin, 1d));
            Assert.IsFalse(_core.TrySpend(EconomyIds.Coin, 1d, SpendReason.GachaSpin, in context));
            Assert.AreEqual(0d, _core.GetBalance(EconomyIds.Coin));
            Assert.AreEqual(1d, _core.BaseCoinValue);
            Assert.AreEqual(0d, _core.SpinPrice);
            Assert.IsFalse(_core.IsCurrencyRegistered(EconomyIds.Coin));
            Assert.IsNull(_core.ExportState());
        }

        [Test]
        public void Initialize_NullConfig_ReturnsFalse()
        {
            Assert.IsFalse(_core.Initialize(null, ReleaseTier.Mvp, null));
            Assert.IsFalse(_core.IsInitialized);
        }

        [Test]
        public void Initialize_Twice_SecondCallFailsAndStateIsUnchanged()
        {
            _custom = EconomyStateTestData.CreateConfigWithCurrencies(StartingCoins);
            Assert.IsTrue(_core.Initialize(_custom, ReleaseTier.Mvp, null));

            Assert.IsFalse(_core.Initialize(EconomyTestAssets.LoadConfig(), ReleaseTier.Alpha, null));

            Assert.IsFalse(_core.IsCurrencyRegistered(EconomyIds.ShopStamp));
            EconomyStateTestData.AssertExactlyEqual(StartingCoins, _core.GetBalance(EconomyIds.Coin));
        }

        [Test]
        public void Initialize_NewGame_EmitsNoEventsAndNoWarnings()
        {
            EconomyEventRecorder recorder = new EconomyEventRecorder(8);
            _core.WalletChanged += recorder.OnWalletChanged;
            _custom = EconomyStateTestData.CreateConfigWithCurrencies(StartingCoins);

            _core.Initialize(_custom, ReleaseTier.Mvp, null);
            _core.WalletChanged -= recorder.OnWalletChanged;

            Assert.AreEqual(0, recorder.Count);
            Assert.AreEqual(0, _sink.WarningCount);
            Assert.AreEqual(0, _sink.ErrorCount);
        }

        [Test]
        public void WalletEvents_AfterInitialize_AreForwardedOnce()
        {
            EconomyEventRecorder recorder = new EconomyEventRecorder(8);
            _core.Initialize(EconomyTestAssets.LoadConfig(), ReleaseTier.Mvp, null);
            _core.WalletChanged += recorder.OnWalletChanged;

            Assert.IsTrue(_core.Add(EconomyIds.Coin, 5d, WalletChangeReason.AdReward));
            _core.WalletChanged -= recorder.OnWalletChanged;

            Assert.AreEqual(1, recorder.Count);
            Assert.AreEqual(5d, _core.GetBalance(EconomyIds.Coin));
        }

        [Test]
        public void Queries_AfterInitialize_DoNotAllocate()
        {
            _core.Initialize(EconomyTestAssets.LoadConfig(), ReleaseTier.Mvp, null);
            EconomyCore core = _core;

            int allocations = GcAllocProbe.CountAllocations(() => RunQueries(core));

            Assert.AreEqual(0, allocations);
        }

        private static void RunQueries(EconomyCore core)
        {
            for (int i = 0; i < Iterations; i++)
            {
                core.GetBalance(EconomyIds.Coin);
                core.GetBalance(EconomyIds.ShopStamp);
                core.IsCurrencyRegistered(EconomyIds.Coin);
                core.CanAfford(EconomyIds.Coin, 1d);
                double unused = core.BaseCoinValue + core.SpinPrice + core.SpinsThisShop;
                if (unused < 0d)
                {
                    core.GetBalance(string.Empty);
                }
            }
        }
    }
}
