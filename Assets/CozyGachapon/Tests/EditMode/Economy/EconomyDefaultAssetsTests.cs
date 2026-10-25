using System;
using System.Reflection;
using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using NUnit.Framework;

namespace CozyGachapon.Tests.Economy
{
    public class EconomyDefaultAssetsTests
    {
        private const string CoreAssemblyName = "CozyGachapon.Core";
        private const string ProjectAssemblyPrefix = "CozyGachapon.";
        private const string ContractsAssemblyName = "CozyGachapon.Contracts";
        private const string FoundationAssemblyName = "CozyGachapon.Foundation";

        [Test]
        public void DefaultConfig_MatchesGddCurrentValues()
        {
            EconomyConfig config = EconomyTestAssets.LoadConfig();
            Assert.IsNotNull(config);

            Assert.AreEqual(0d, config.StartingCoins);
            Assert.AreEqual(1e300d, config.WalletSafeCap);
            Assert.AreEqual(100d, config.SpinBasePrice);
            Assert.AreEqual(1.05d, config.SpinGrowth);
            Assert.AreEqual(0.6d, config.CoinValueExponent);
            Assert.AreEqual(2, config.PriceNiceSigDigits);
            Assert.AreEqual(2d, config.IdleCpsUnit);
            Assert.AreEqual(4d, config.SynergyTierCap);
            Assert.AreEqual(3d, config.BoostTierCap);
            Assert.AreEqual(10d, config.PrestigeTierCap);
            Assert.AreEqual(2d, config.GlobalTierCap);
            Assert.AreEqual(2d, config.PlinkoFlatPerCoinCap);
            Assert.AreEqual(0.25d, config.MaxFrameDelta);
            Assert.AreEqual(90d, config.AdCompensationMaxSeconds);
            Assert.AreEqual(10d, config.PlinkoRateWindowSeconds);
            Assert.AreEqual(0.5d, config.RatesPublishInterval);
            Assert.AreEqual(3, config.DisplaySigDigits);
            Assert.AreEqual(1, config.RateDecimals);
            Assert.IsNotNull(config.TierOwnerPrefixes);
            Assert.AreEqual(2, config.TierOwnerPrefixes.Boost.Count);
        }

        [Test]
        public void DefaultConfig_ListsCoinThenShopStamp()
        {
            EconomyConfig config = EconomyTestAssets.LoadConfig();
            Assert.IsNotNull(config);

            Assert.AreEqual(2, config.Currencies.Count);
            Assert.AreSame(EconomyTestAssets.LoadCoin(), config.Currencies[0]);
            Assert.AreSame(EconomyTestAssets.LoadShopStamp(), config.Currencies[1]);
        }

        [Test]
        public void DefaultCurrencies_MatchGddDefinitions()
        {
            CurrencyDefinition coin = EconomyTestAssets.LoadCoin();
            CurrencyDefinition stamp = EconomyTestAssets.LoadShopStamp();
            Assert.IsNotNull(coin);
            Assert.IsNotNull(stamp);

            Assert.AreEqual(EconomyIds.Coin, coin.Id);
            Assert.IsTrue(coin.ResetsOnNewShop);
            Assert.AreEqual(ReleaseTier.Mvp, coin.ReleaseTier);
            Assert.IsFalse(string.IsNullOrEmpty(coin.DisplayName));
            Assert.AreEqual(EconomyIds.ShopStamp, stamp.Id);
            Assert.IsFalse(stamp.ResetsOnNewShop);
            Assert.AreEqual(ReleaseTier.Alpha, stamp.ReleaseTier);
            Assert.IsFalse(string.IsNullOrEmpty(stamp.DisplayName));
        }

        [Test]
        public void DefaultBalanceTargets_MatchGddCurrentValues()
        {
            EconomyBalanceTargets targets = EconomyTestAssets.LoadTargets();
            Assert.IsNotNull(targets);

            Assert.AreEqual(3d, targets.RealisticTapRate);
            Assert.AreEqual(2.96d, targets.PlinkoEvEffectiveAssumed);
            Assert.AreEqual(0.4d, targets.PlinkoBounceUpliftAtCapAssumed);
            CollectionAssert.AreEqual(new[] { 20d, 27.1d, 30.7d, 42d }, targets.PlinkoRawRateCeilingAssumed);
            Assert.AreEqual(0.109d, targets.PlinkoJackpotProbMeanAssumed);
            Assert.AreEqual(14d, targets.PlinkoScoredPegHitsPerCoinAssumed);
            Assert.AreEqual(30d, targets.FirstSpinMaxSeconds);
            CollectionAssert.AreEqual(new[] { 0, 50, 100 }, targets.SpinCheckpoints);
            CollectionAssert.AreEqual(new[] { 30d, 60d, 120d }, targets.PlinkoOnlySpinSecondsMax);
            Assert.AreEqual(0.2d, targets.PipelineFrameBudgetMs);
            Assert.AreEqual(1e-9d, targets.ValueRelTolerance);
            Assert.AreEqual(1.5d, targets.PlinkoSoftCapTestFactor);
        }

        [Test]
        public void DefaultSafeRanges_LoadAndBoundWalletCap()
        {
            EconomySafeRanges ranges = EconomyTestAssets.LoadSafeRanges();
            Assert.IsNotNull(ranges);

            Assert.AreEqual(1e100d, ranges.WalletSafeCap.Min);
            Assert.AreEqual(1e305d, ranges.WalletSafeCap.Max);
            Assert.AreEqual(0d, ranges.StartingCoins.Min);
            Assert.AreEqual(100d, ranges.StartingCoins.Max);
        }

        [Test]
        public void CoreAssembly_ReferencesOnlyContractsAndFoundation()
        {
            Assembly core = typeof(WalletService).Assembly;
            Assert.AreEqual(CoreAssemblyName, core.GetName().Name);

            AssemblyName[] references = core.GetReferencedAssemblies();
            for (int i = 0; i < references.Length; i++)
            {
                string name = references[i].Name;
                if (!name.StartsWith(ProjectAssemblyPrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                Assert.IsTrue(
                    name == ContractsAssemblyName || name == FoundationAssemblyName,
                    name);
            }
        }

        [Test]
        public void CurrencyLedger_IsNotPublic()
        {
            Assert.IsFalse(typeof(CurrencyLedger).IsPublic);
            Assert.AreEqual(CoreAssemblyName, typeof(CurrencyLedger).Assembly.GetName().Name);
        }

        [Test]
        public void WalletService_HasNoStaticFieldsOrPublicSetters()
        {
            BindingFlags staticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            Assert.AreEqual(0, typeof(WalletService).GetFields(staticFlags).Length);
            Assert.AreEqual(0, typeof(CurrencyLedger).GetFields(staticFlags).Length);

            PropertyInfo[] properties = typeof(WalletService).GetProperties(BindingFlags.Instance | BindingFlags.Public);
            for (int i = 0; i < properties.Length; i++)
            {
                Assert.IsNull(properties[i].GetSetMethod(), properties[i].Name);
            }
        }
    }
}
