using UnityEngine;

namespace CozyGachapon.Plinko
{
    [CreateAssetMenu(fileName = "PlinkoBalanceTargets", menuName = "CozyGachapon/Plinko/PlinkoBalanceTargets")]
    public class PlinkoBalanceTargets : ScriptableObject
    {
        [SerializeField] private float _targetMedianFallTime = 2.5f;
        [SerializeField] private float _fallTimeTolerance = 0.4f;
        [SerializeField] private float _fallTimeP95Max = 5.0f;
        [SerializeField] private float _jackpotProbMeanMin = 0.10f;
        [SerializeField] private float _jackpotProbMeanMax = 0.13f;
        [SerializeField] private float _jackpotProbMaxAtAnyX = 0.18f;
        [SerializeField] private float _jackpotProbMinAtCenter = 0.06f;
        [SerializeField] private float _evMeanMin = 1.85f;
        [SerializeField] private float _evMeanMax = 2.05f;
        [SerializeField] private float _evRatioMax = 1.20f;
        [SerializeField] private float _capJackpotProbMeanMax = 0.20f;
        [SerializeField] private float _capEvMeanMax = 2.3f;
        [SerializeField] private float _stuckRateMax = 0.01f;
        [SerializeField] private int _simCoinsPerDropX = 2000;
        [SerializeField] private int _simDropXSamples = 7;
        [SerializeField] private float _lifetimeMarginSeconds = 1.0f;
        [SerializeField] private float _energyGainTolerance = 0.02f;
        [SerializeField] private float _maxRawPayoutPerSecondPerBaseCoin = 24f;
        [SerializeField] private float _maxRawPayoutPerSecondPerBaseCoinWithAuto = 32f;
        [SerializeField] private float _maxRawPayoutPerSecondPerBaseCoinAtCap = 36f;
        [SerializeField] private float _maxRawPayoutPerSecondPerBaseCoinAutoAndCap = 50f;
        [SerializeField] private float _dropUniformityTolerance = 0.015f;
        [SerializeField] private int _dropUniformityBins = 10;
        [SerializeField] private float _geometryTolerance = 1e-4f;
        [SerializeField] private float _speedTolerance = 1e-3f;
        [SerializeField] private float _contactTolerance = 1e-3f;
        [SerializeField] private int _simCoinCountLarge = 10000;
        [SerializeField] private int _simCoinCountSmall = 1000;
        [SerializeField] private int[] _autoclickerRatesPerSecond = new int[] { 30, 60, 240 };
        [SerializeField] private float _autoclickerSimSeconds = 10f;
        [SerializeField] private float _throughputSimSeconds = 60f;
        [SerializeField] private float _pauseTestSeconds = 60f;
        [SerializeField] private float _tabSwitchTestSeconds = 5f;
        [SerializeField] private int _lifecycleTestCoins = 10;
        [SerializeField] private float _hitchFrameSeconds = 1.0f;
        [SerializeField] private int _multiTouchTestFingers = 3;
        [SerializeField] private Vector2Int _screenshotReferenceResolution = new Vector2Int(1080, 1920);
        [SerializeField] private int _screenshotPositionTolerancePx = 4;
        [SerializeField] private float _labelContrastMin = 4.5f;
        [SerializeField] private float _physicsFrameBudgetMs = 4.0f;
        [SerializeField] private int _tapToSpawnMaxMs = 50;
        [SerializeField] private int _resolveToFeedbackMaxMs = 34;
        [SerializeField] private int _boardDrawCallBudget = 15;

        public float TargetMedianFallTime => _targetMedianFallTime;
        public float FallTimeTolerance => _fallTimeTolerance;
        public float FallTimeP95Max => _fallTimeP95Max;
        public float JackpotProbMeanMin => _jackpotProbMeanMin;
        public float JackpotProbMeanMax => _jackpotProbMeanMax;
        public float JackpotProbMaxAtAnyX => _jackpotProbMaxAtAnyX;
        public float JackpotProbMinAtCenter => _jackpotProbMinAtCenter;
        public float EvMeanMin => _evMeanMin;
        public float EvMeanMax => _evMeanMax;
        public float EvRatioMax => _evRatioMax;
        public float CapJackpotProbMeanMax => _capJackpotProbMeanMax;
        public float CapEvMeanMax => _capEvMeanMax;
        public float StuckRateMax => _stuckRateMax;
        public int SimCoinsPerDropX => _simCoinsPerDropX;
        public int SimDropXSamples => _simDropXSamples;
        public float LifetimeMarginSeconds => _lifetimeMarginSeconds;
        public float EnergyGainTolerance => _energyGainTolerance;
        public float MaxRawPayoutPerSecondPerBaseCoin => _maxRawPayoutPerSecondPerBaseCoin;
        public float MaxRawPayoutPerSecondPerBaseCoinWithAuto => _maxRawPayoutPerSecondPerBaseCoinWithAuto;
        public float MaxRawPayoutPerSecondPerBaseCoinAtCap => _maxRawPayoutPerSecondPerBaseCoinAtCap;
        public float MaxRawPayoutPerSecondPerBaseCoinAutoAndCap => _maxRawPayoutPerSecondPerBaseCoinAutoAndCap;
        public float DropUniformityTolerance => _dropUniformityTolerance;
        public int DropUniformityBins => _dropUniformityBins;
        public float GeometryTolerance => _geometryTolerance;
        public float SpeedTolerance => _speedTolerance;
        public float ContactTolerance => _contactTolerance;
        public int SimCoinCountLarge => _simCoinCountLarge;
        public int SimCoinCountSmall => _simCoinCountSmall;
        public int[] AutoclickerRatesPerSecond => _autoclickerRatesPerSecond;
        public float AutoclickerSimSeconds => _autoclickerSimSeconds;
        public float ThroughputSimSeconds => _throughputSimSeconds;
        public float PauseTestSeconds => _pauseTestSeconds;
        public float TabSwitchTestSeconds => _tabSwitchTestSeconds;
        public int LifecycleTestCoins => _lifecycleTestCoins;
        public float HitchFrameSeconds => _hitchFrameSeconds;
        public int MultiTouchTestFingers => _multiTouchTestFingers;
        public Vector2Int ScreenshotReferenceResolution => _screenshotReferenceResolution;
        public int ScreenshotPositionTolerancePx => _screenshotPositionTolerancePx;
        public float LabelContrastMin => _labelContrastMin;
        public float PhysicsFrameBudgetMs => _physicsFrameBudgetMs;
        public int TapToSpawnMaxMs => _tapToSpawnMaxMs;
        public int ResolveToFeedbackMaxMs => _resolveToFeedbackMaxMs;
        public int BoardDrawCallBudget => _boardDrawCallBudget;
    }
}

