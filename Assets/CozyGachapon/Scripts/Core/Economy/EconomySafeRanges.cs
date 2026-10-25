using UnityEngine;

namespace CozyGachapon.Economy
{
    [CreateAssetMenu(fileName = "EconomySafeRanges", menuName = "CozyGachapon/Economy/EconomySafeRanges")]
    public class EconomySafeRanges : ScriptableObject
    {
        [SerializeField] private DoubleRange _startingCoins = new DoubleRange(0d, 100d);
        [SerializeField] private DoubleRange _walletSafeCap = new DoubleRange(1e100d, 1e305d);
        [SerializeField] private DoubleRange _spinBasePrice = new DoubleRange(50d, 200d);
        [SerializeField] private DoubleRange _spinGrowth = new DoubleRange(1.03d, 1.08d);
        [SerializeField] private DoubleRange _coinValueExponent = new DoubleRange(0.5d, 0.7d);
        [SerializeField] private DoubleRange _priceNiceSigDigits = new DoubleRange(2d, 3d);
        [SerializeField] private DoubleRange _idleCpsUnit = new DoubleRange(1d, 5d);
        [SerializeField] private DoubleRange _synergyTierCap = new DoubleRange(2d, 8d);
        [SerializeField] private DoubleRange _boostTierCap = new DoubleRange(2d, 5d);
        [SerializeField] private DoubleRange _prestigeTierCap = new DoubleRange(3d, 50d);
        [SerializeField] private DoubleRange _globalTierCap = new DoubleRange(1d, 3d);
        [SerializeField] private DoubleRange _plinkoFlatPerCoinCap = new DoubleRange(0d, 5d);
        [SerializeField] private DoubleRange _maxFrameDelta = new DoubleRange(0.1d, 0.5d);
        [SerializeField] private DoubleRange _adCompensationMaxSeconds = new DoubleRange(30d, 300d);
        [SerializeField] private DoubleRange _plinkoRateWindowSeconds = new DoubleRange(5d, 30d);
        [SerializeField] private DoubleRange _ratesPublishInterval = new DoubleRange(0.25d, 1d);
        [SerializeField] private DoubleRange _displaySigDigits = new DoubleRange(3d, 4d);
        [SerializeField] private DoubleRange _rateDecimals = new DoubleRange(0d, 2d);

        public DoubleRange StartingCoins => _startingCoins;
        public DoubleRange WalletSafeCap => _walletSafeCap;
        public DoubleRange SpinBasePrice => _spinBasePrice;
        public DoubleRange SpinGrowth => _spinGrowth;
        public DoubleRange CoinValueExponent => _coinValueExponent;
        public DoubleRange PriceNiceSigDigits => _priceNiceSigDigits;
        public DoubleRange IdleCpsUnit => _idleCpsUnit;
        public DoubleRange SynergyTierCap => _synergyTierCap;
        public DoubleRange BoostTierCap => _boostTierCap;
        public DoubleRange PrestigeTierCap => _prestigeTierCap;
        public DoubleRange GlobalTierCap => _globalTierCap;
        public DoubleRange PlinkoFlatPerCoinCap => _plinkoFlatPerCoinCap;
        public DoubleRange MaxFrameDelta => _maxFrameDelta;
        public DoubleRange AdCompensationMaxSeconds => _adCompensationMaxSeconds;
        public DoubleRange PlinkoRateWindowSeconds => _plinkoRateWindowSeconds;
        public DoubleRange RatesPublishInterval => _ratesPublishInterval;
        public DoubleRange DisplaySigDigits => _displaySigDigits;
        public DoubleRange RateDecimals => _rateDecimals;
    }
}
