using System.Collections.Generic;
using UnityEngine;

namespace CozyGachapon.Economy
{
    [CreateAssetMenu(fileName = "EconomyConfig", menuName = "CozyGachapon/Economy/EconomyConfig")]
    public class EconomyConfig : ScriptableObject
    {
        [SerializeField] private CurrencyDefinition[] _currencies = new CurrencyDefinition[0];
        [SerializeField] private double _startingCoins = 0d;
        [SerializeField] private double _walletSafeCap = 1e300d;
        [SerializeField] private double _spinBasePrice = 100d;
        [SerializeField] private double _spinGrowth = 1.05d;
        [SerializeField] private double _coinValueExponent = 0.6d;
        [SerializeField] private int _priceNiceSigDigits = 2;
        [SerializeField] private double _idleCpsUnit = 2d;
        [SerializeField] private double _synergyTierCap = 4d;
        [SerializeField] private double _boostTierCap = 3d;
        [SerializeField] private double _prestigeTierCap = 10d;
        [SerializeField] private double _globalTierCap = 2d;
        [SerializeField] private double _plinkoFlatPerCoinCap = 2d;
        [SerializeField] private TierOwnerPrefixes _tierOwnerPrefixes = new TierOwnerPrefixes();
        [SerializeField] private double _maxFrameDelta = 0.25d;
        [SerializeField] private double _adCompensationMaxSeconds = 90d;
        [SerializeField] private double _plinkoRateWindowSeconds = 10d;
        [SerializeField] private double _ratesPublishInterval = 0.5d;
        [SerializeField] private int _displaySigDigits = 3;
        [SerializeField] private int _rateDecimals = 1;

        public IReadOnlyList<CurrencyDefinition> Currencies => _currencies;
        public double StartingCoins => _startingCoins;
        public double WalletSafeCap => _walletSafeCap;
        public double SpinBasePrice => _spinBasePrice;
        public double SpinGrowth => _spinGrowth;
        public double CoinValueExponent => _coinValueExponent;
        public int PriceNiceSigDigits => _priceNiceSigDigits;
        public double IdleCpsUnit => _idleCpsUnit;
        public double SynergyTierCap => _synergyTierCap;
        public double BoostTierCap => _boostTierCap;
        public double PrestigeTierCap => _prestigeTierCap;
        public double GlobalTierCap => _globalTierCap;
        public double PlinkoFlatPerCoinCap => _plinkoFlatPerCoinCap;
        public TierOwnerPrefixes TierOwnerPrefixes => _tierOwnerPrefixes;
        public double MaxFrameDelta => _maxFrameDelta;
        public double AdCompensationMaxSeconds => _adCompensationMaxSeconds;
        public double PlinkoRateWindowSeconds => _plinkoRateWindowSeconds;
        public double RatesPublishInterval => _ratesPublishInterval;
        public int DisplaySigDigits => _displaySigDigits;
        public int RateDecimals => _rateDecimals;

        public void SetCurrenciesForTest(CurrencyDefinition[] currencies)
        {
            _currencies = currencies;
        }

        public void SetStartingCoinsForTest(double startingCoins)
        {
            _startingCoins = startingCoins;
        }

        public void SetWalletSafeCapForTest(double walletSafeCap)
        {
            _walletSafeCap = walletSafeCap;
        }

        public void SetDisplaySigDigitsForTest(int displaySigDigits)
        {
            _displaySigDigits = displaySigDigits;
        }

        public void SetSpinGrowthForTest(double spinGrowth)
        {
            _spinGrowth = spinGrowth;
        }
    }
}
