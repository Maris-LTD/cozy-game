using System;
using System.Reflection;
using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using NUnit.Framework;
using Object = UnityEngine.Object;

namespace CozyGachapon.Tests.Economy
{
    public class EconomyStateSnapshotTests
    {
        private const int SavedSpins = 37;
        private const double CoinValue = 0.1d;
        private const double StampValue = 7d;
        private const double FutureValue = 3d;
        private const double LargeCoinValue = 1e300d;

        private CountingEconomyDiagnosticsSink _sink;
        private EconomyConfig _config;

        [SetUp]
        public void SetUp()
        {
            _sink = new CountingEconomyDiagnosticsSink();
            _config = EconomyStateTestData.CreateConfigWithCurrencies(0d);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [TestCase(CoinValue)]
        [TestCase(LargeCoinValue)]
        public void RoundTrip_AlphaTier_KeepsEveryValueExactly(double coin)
        {
            EconomyCore first = Init(ReleaseTier.Alpha, BuildSaved(coin));
            EconomyStateData exported = first.ExportState();
            EconomyCore second = Init(ReleaseTier.Alpha, exported);
            EconomyStateData again = second.ExportState();

            AssertState(exported, coin);
            AssertState(again, coin);
            EconomyStateTestData.AssertExactlyEqual(coin, second.GetBalance(EconomyIds.Coin));
            EconomyStateTestData.AssertExactlyEqual(StampValue, second.GetBalance(EconomyIds.ShopStamp));
            Assert.AreEqual(SavedSpins, again.SpinsThisShop);
            Assert.AreEqual(0, _sink.WarningCount);
        }

        [Test]
        public void MvpBuild_UnregisteredCurrencies_StayInExportedState()
        {
            EconomyCore alpha = Init(ReleaseTier.Alpha, BuildSaved(CoinValue));
            EconomyCore mvp = Init(ReleaseTier.Mvp, alpha.ExportState());

            Assert.IsFalse(mvp.IsCurrencyRegistered(EconomyIds.ShopStamp));
            Assert.AreEqual(0d, mvp.GetBalance(EconomyIds.ShopStamp));
            EconomyStateData exported = mvp.ExportState();
            AssertState(exported, CoinValue);

            EconomyCore mvpAgain = Init(ReleaseTier.Mvp, exported);
            AssertState(mvpAgain.ExportState(), CoinValue);
        }

        [Test]
        public void LoadedSpins_DrivePriceAndBaseCoinValue()
        {
            EconomyCore core = Init(ReleaseTier.Alpha, BuildSaved(CoinValue));
            SpinPriceCurve curve = new SpinPriceCurve(_config);
            BaseCoinValueCalculator calculator = new BaseCoinValueCalculator(_config, curve);

            Assert.AreEqual(SavedSpins, core.SpinsThisShop);
            EconomyStateTestData.AssertExactlyEqual(curve.Price(SavedSpins), core.SpinPrice);
            EconomyStateTestData.AssertExactlyEqual(calculator.Compute(SavedSpins), core.BaseCoinValue);
            Assert.Greater(core.BaseCoinValue, 1d);
        }

        [Test]
        public void ExportState_IsIndependentSnapshot()
        {
            EconomyCore core = Init(ReleaseTier.Alpha, BuildSaved(CoinValue));
            EconomyStateData first = core.ExportState();

            Assert.IsTrue(core.Add(EconomyIds.Coin, 5d, WalletChangeReason.AdReward));
            EconomyStateData second = core.ExportState();

            Assert.AreNotSame(first, second);
            EconomyStateTestData.AssertExactlyEqual(CoinValue, EconomyStateTestData.Find(first, EconomyIds.Coin));
            Assert.AreEqual(CoinValue + 5d, EconomyStateTestData.Find(second, EconomyIds.Coin));
        }

        [Test]
        public void StateTypes_HaveOnlyPrivateNonDictionaryFields()
        {
            AssertPrivateFields(typeof(EconomyStateData));
            AssertPrivateFields(typeof(CurrencyBalanceEntry));
        }

        [Test]
        public void ExportState_ListsRegisteredCurrenciesInConfigOrderBeforePreservedOnes()
        {
            EconomyCore core = Init(ReleaseTier.Mvp, BuildSaved(CoinValue));
            EconomyStateData exported = core.ExportState();

            Assert.AreEqual(3, exported.Balances.Count);
            Assert.AreEqual(EconomyIds.Coin, exported.Balances[0].CurrencyId);
        }

        private static EconomyStateData BuildSaved(double coin)
        {
            return EconomyStateTestData.Build(
                SavedSpins,
                new CurrencyBalanceEntry(EconomyIds.Coin, coin),
                new CurrencyBalanceEntry(EconomyIds.ShopStamp, StampValue),
                new CurrencyBalanceEntry(EconomyStateTestData.FutureId, FutureValue));
        }

        private static void AssertState(EconomyStateData state, double coin)
        {
            Assert.AreEqual(3, state.Balances.Count);
            Assert.AreEqual(SavedSpins, state.SpinsThisShop);
            EconomyStateTestData.AssertExactlyEqual(coin, EconomyStateTestData.Find(state, EconomyIds.Coin));
            EconomyStateTestData.AssertExactlyEqual(StampValue, EconomyStateTestData.Find(state, EconomyIds.ShopStamp));
            EconomyStateTestData.AssertExactlyEqual(FutureValue, EconomyStateTestData.Find(state, EconomyStateTestData.FutureId));
        }

        private static void AssertPrivateFields(Type type)
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.Greater(fields.Length, 0);
            for (int i = 0; i < fields.Length; i++)
            {
                Assert.IsTrue(fields[i].IsPrivate, fields[i].Name);
                Type fieldType = fields[i].FieldType;
                Assert.IsFalse(fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(System.Collections.Generic.Dictionary<,>), fields[i].Name);
            }
        }

        private EconomyCore Init(ReleaseTier tier, EconomyStateData loaded)
        {
            EconomyCore core = new EconomyCore(_sink);
            Assert.IsTrue(core.Initialize(_config, tier, loaded));
            return core;
        }
    }
}
