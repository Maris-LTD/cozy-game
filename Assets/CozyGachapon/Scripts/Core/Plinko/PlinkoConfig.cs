using System;
using UnityEngine;
using CozyGachapon.Contracts;

namespace CozyGachapon.Plinko
{
    [CreateAssetMenu(fileName = "PlinkoConfig", menuName = "CozyGachapon/Plinko/PlinkoConfig")]
    public class PlinkoConfig : ScriptableObject, IValidatableAsset
    {
        [SerializeField] private int _pegRows = 12;
        [SerializeField] private int _pegsPerRow = 10;
        [SerializeField] private float _pegSpacing = 0.55f;
        [SerializeField] private float _rowSpacingRatio = 0.866f;
        [SerializeField] private float _pegRadius = 0.06f;
        [SerializeField] private float _coinRadius = 0.16f;
        [SerializeField] private float _wallBumpRadius = 0.06f;
        [SerializeField] private float _minGapMargin = 0.06f;
        [SerializeField] private bool _useWallBumps = true;
        [SerializeField] private float[] _bucketMultipliers = new float[] { 5f, 2f, 1f, 2f, 5f };
        [SerializeField] private float[] _bucketWidthWeights = new float[] { 1f, 1f, 1f, 1f, 1f };
        [SerializeField] private float _dropZoneWidthRatio = 0.14f;
        [SerializeField] private float _dropJitter = 1.0f;
        [SerializeField] private float _basePegBounciness = 0.5f;
        [SerializeField] private float _bouncinessCap = 0.85f;
        [SerializeField] private float _coinGravityScale = 0.6f;
        [SerializeField] private float _maxCoinSpeed = 6f;
        [SerializeField] private float _maxSimStep = 0.25f;
        [SerializeField] private float _tapCooldown = 0.15f;
        [SerializeField] private int _maxActiveCoins = 24;
        [SerializeField] private float _burstSpawnInterval = 0.08f;
        [SerializeField] private int _burstQueueMax = 60;
        [SerializeField] private int _manualReservedSlots = 2;
        [SerializeField] private int _jackpotBurstCount = 5;
        [SerializeField] private float _autoDropBaseInterval = 2.0f;
        [SerializeField] private float _autoDropMinInterval = 0.4f;
        [SerializeField] private int _maxScoredPegHits = 20;
        [SerializeField] private float _pegHitDebounce = 0.10f;
        [SerializeField] private float _stuckSpeedThreshold = 0.15f;
        [SerializeField] private float _stuckTime = 1.0f;
        [SerializeField] private float _coinMaxLifetime = 8f;
        [SerializeField] private float _stuckPayoutMultiplier = 1.0f;
        [SerializeField] private float _instantPayoutMultiplier = 1.9f;
        [SerializeField] private double _baseCoinValue = 1d;

        public int PegRows => _pegRows;
        public int PegsPerRow => _pegsPerRow;
        public float PegSpacing => _pegSpacing;
        public float RowSpacingRatio => _rowSpacingRatio;
        public float PegRadius => _pegRadius;
        public float CoinRadius => _coinRadius;
        public float WallBumpRadius => _wallBumpRadius;
        public float MinGapMargin => _minGapMargin;
        public bool UseWallBumps => _useWallBumps;
        public float[] BucketMultipliers => _bucketMultipliers;
        public float[] BucketWidthWeights => _bucketWidthWeights;
        public float DropZoneWidthRatio => _dropZoneWidthRatio;
        public float DropJitter => _dropJitter;
        public float BasePegBounciness => _basePegBounciness;
        public float BouncinessCap => _bouncinessCap;
        public float CoinGravityScale => _coinGravityScale;
        public float MaxCoinSpeed => _maxCoinSpeed;
        public float MaxSimStep => _maxSimStep;
        public float TapCooldown => _tapCooldown;
        public int MaxActiveCoins => _maxActiveCoins;
        public float BurstSpawnInterval => _burstSpawnInterval;
        public int BurstQueueMax => _burstQueueMax;
        public int ManualReservedSlots => _manualReservedSlots;
        public int JackpotBurstCount => _jackpotBurstCount;
        public float AutoDropBaseInterval => _autoDropBaseInterval;
        public float AutoDropMinInterval => _autoDropMinInterval;
        public int MaxScoredPegHits => _maxScoredPegHits;
        public float PegHitDebounce => _pegHitDebounce;
        public float StuckSpeedThreshold => _stuckSpeedThreshold;
        public float StuckTime => _stuckTime;
        public float CoinMaxLifetime => _coinMaxLifetime;
        public float StuckPayoutMultiplier => _stuckPayoutMultiplier;
        public float InstantPayoutMultiplier => _instantPayoutMultiplier;
        public double BaseCoinValue => _baseCoinValue;

        public void Validate(IValidationContext context)
        {
            PlinkoConfigValidator.Validate(this, context);
        }

        public void SetPegRowsForTest(int pegRows) => _pegRows = pegRows;
        public void SetPegsPerRowForTest(int pegsPerRow) => _pegsPerRow = pegsPerRow;
        public void SetPegSpacingForTest(float pegSpacing) => _pegSpacing = pegSpacing;
        public void SetRowSpacingRatioForTest(float ratio) => _rowSpacingRatio = ratio;
        public void SetPegRadiusForTest(float radius) => _pegRadius = radius;
        public void SetCoinRadiusForTest(float radius) => _coinRadius = radius;
        public void SetWallBumpRadiusForTest(float radius) => _wallBumpRadius = radius;
        public void SetMinGapMarginForTest(float margin) => _minGapMargin = margin;
        public void SetUseWallBumpsForTest(bool useBumps) => _useWallBumps = useBumps;
        public void SetBucketMultipliersForTest(float[] multipliers) => _bucketMultipliers = multipliers;
        public void SetBucketWidthWeightsForTest(float[] weights) => _bucketWidthWeights = weights;
        public void SetBouncinessCapForTest(float cap) => _bouncinessCap = cap;
        public void SetBasePegBouncinessForTest(float bounciness) => _basePegBounciness = bounciness;
        public void SetMaxCoinSpeedForTest(float speed) => _maxCoinSpeed = speed;
        public void SetBaseCoinValueForTest(double value) => _baseCoinValue = value;
    }
}

