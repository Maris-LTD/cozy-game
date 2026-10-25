using System.Collections.Generic;
using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Economy
{
    public class EconomyConfigValidatorTests
    {
        private readonly List<Object> _created = new List<Object>();
        private readonly List<EconomyConfigIssue> _issues = new List<EconomyConfigIssue>();
        private EconomyConfig _config;
        private EconomySafeRanges _ranges;

        [SetUp]
        public void SetUp()
        {
            _issues.Clear();
            _config = Track(ScriptableObject.CreateInstance<EconomyConfig>());
            _ranges = Track(ScriptableObject.CreateInstance<EconomySafeRanges>());
            _config.SetCurrenciesForTest(new[] { Currency(EconomyIds.Coin), Currency(EconomyIds.ShopStamp) });
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _created.Count; i++)
            {
                Object.DestroyImmediate(_created[i]);
            }

            _created.Clear();
        }

        [Test]
        public void Validate_DefaultAssets_NoIssues()
        {
            EconomyConfig config = EconomyTestAssets.LoadConfig();
            EconomySafeRanges ranges = EconomyTestAssets.LoadSafeRanges();
            Assert.IsNotNull(config);
            Assert.IsNotNull(ranges);

            EconomyConfigValidator.Validate(config, ranges, _issues);

            Assert.AreEqual(0, _issues.Count);
        }

        [Test]
        public void Validate_InstanceDefaults_NoIssues()
        {
            EconomyConfigValidator.Validate(_config, _ranges, _issues);

            Assert.AreEqual(0, _issues.Count);
        }

        [Test]
        public void Validate_NullConfig_ReportsConfigMissing()
        {
            EconomyConfigValidator.Validate(null, _ranges, _issues);

            AssertSingle(EconomyConfigIssueCode.ConfigMissing);
        }

        [Test]
        public void Validate_NullRanges_ReportsSafeRangesMissing()
        {
            EconomyConfigValidator.Validate(_config, null, _issues);

            AssertSingle(EconomyConfigIssueCode.SafeRangesMissing);
        }

        [Test]
        public void Validate_EmptyCurrencies_ReportsCurrenciesEmpty()
        {
            _config.SetCurrenciesForTest(new CurrencyDefinition[0]);

            EconomyConfigValidator.Validate(_config, _ranges, _issues);

            AssertSingle(EconomyConfigIssueCode.CurrenciesEmpty);
        }

        [Test]
        public void Validate_NullCurrencyEntry_ReportsCurrencyMissing()
        {
            _config.SetCurrenciesForTest(new[] { Currency(EconomyIds.Coin), null });

            EconomyConfigValidator.Validate(_config, _ranges, _issues);

            AssertSingle(EconomyConfigIssueCode.CurrencyMissing);
            Assert.AreEqual(1d, _issues[0].Value);
        }

        [Test]
        public void Validate_EmptyCurrencyId_ReportsCurrencyIdMissing()
        {
            _config.SetCurrenciesForTest(new[] { Currency(EconomyIds.Coin), Currency(string.Empty) });

            EconomyConfigValidator.Validate(_config, _ranges, _issues);

            AssertSingle(EconomyConfigIssueCode.CurrencyIdMissing);
        }

        [Test]
        public void Validate_DuplicateCurrencyId_ReportsDuplicate()
        {
            _config.SetCurrenciesForTest(new[] { Currency(EconomyIds.Coin), Currency(EconomyIds.Coin) });

            EconomyConfigValidator.Validate(_config, _ranges, _issues);

            AssertSingle(EconomyConfigIssueCode.DuplicateCurrencyId);
            Assert.AreEqual(EconomyIds.Coin, _issues[0].Subject);
        }

        [Test]
        public void Validate_NoCoinCurrency_ReportsCoinMissing()
        {
            _config.SetCurrenciesForTest(new[] { Currency(EconomyIds.ShopStamp) });

            EconomyConfigValidator.Validate(_config, _ranges, _issues);

            AssertSingle(EconomyConfigIssueCode.CoinCurrencyMissing);
        }

        [TestCase(-1d)]
        [TestCase(101d)]
        [TestCase(double.NaN)]
        public void Validate_StartingCoinsOutOfRange_ReportsKnob(double value)
        {
            _config.SetStartingCoinsForTest(value);

            EconomyConfigValidator.Validate(_config, _ranges, _issues);

            AssertKnob(EconomyKnobRangeValidator.StartingCoins);
        }

        [TestCase(1e99d)]
        [TestCase(double.PositiveInfinity)]
        public void Validate_WalletSafeCapOutOfRange_ReportsKnob(double value)
        {
            _config.SetWalletSafeCapForTest(value);

            EconomyConfigValidator.Validate(_config, _ranges, _issues);

            AssertKnob(EconomyKnobRangeValidator.WalletSafeCap);
            Assert.AreEqual(_ranges.WalletSafeCap.Min, _issues[0].Min);
            Assert.AreEqual(_ranges.WalletSafeCap.Max, _issues[0].Max);
        }

        [Test]
        public void Validate_DisplaySigDigitsOutOfRange_ReportsKnob()
        {
            _config.SetDisplaySigDigitsForTest(5);

            EconomyConfigValidator.Validate(_config, _ranges, _issues);

            AssertKnob(EconomyKnobRangeValidator.DisplaySigDigits);
        }

        [Test]
        public void Validate_SpinGrowthOutOfRange_ReportsKnob()
        {
            _config.SetSpinGrowthForTest(1.2d);

            EconomyConfigValidator.Validate(_config, _ranges, _issues);

            AssertKnob(EconomyKnobRangeValidator.SpinGrowth);
        }

        [Test]
        public void CountErrors_CountsEveryErrorIssue()
        {
            _config.SetSpinGrowthForTest(1.2d);
            _config.SetStartingCoinsForTest(-1d);
            _config.SetCurrenciesForTest(new[] { Currency(EconomyIds.ShopStamp) });

            EconomyConfigValidator.Validate(_config, _ranges, _issues);

            Assert.AreEqual(3, _issues.Count);
            Assert.AreEqual(3, EconomyConfigValidator.CountErrors(_issues));
        }

        private void AssertSingle(EconomyConfigIssueCode code)
        {
            Assert.AreEqual(1, _issues.Count);
            Assert.AreEqual(code, _issues[0].Code);
            Assert.IsTrue(_issues[0].IsError);
        }

        private void AssertKnob(string subject)
        {
            AssertSingle(EconomyConfigIssueCode.KnobOutOfRange);
            Assert.AreEqual(subject, _issues[0].Subject);
        }

        private CurrencyDefinition Currency(string id)
        {
            return Track(EconomyTestAssets.CreateCurrency(id));
        }

        private T Track<T>(T asset) where T : Object
        {
            _created.Add(asset);
            return asset;
        }
    }
}
