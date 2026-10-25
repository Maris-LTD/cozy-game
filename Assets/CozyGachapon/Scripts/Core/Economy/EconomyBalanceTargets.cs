using System.Collections.Generic;
using UnityEngine;

namespace CozyGachapon.Economy
{
    [CreateAssetMenu(fileName = "EconomyBalanceTargets", menuName = "CozyGachapon/Economy/EconomyBalanceTargets")]
    public class EconomyBalanceTargets : ScriptableObject
    {
        [SerializeField] private double _realisticTapRate = 3d;
        [SerializeField] private double _plinkoEvEffectiveAssumed = 2.96d;
        [SerializeField] private double _plinkoBounceUpliftAtCapAssumed = 0.4d;
        [SerializeField] private double[] _plinkoRawRateCeilingAssumed = { 20d, 27.1d, 30.7d, 42d };
        [SerializeField] private double _plinkoJackpotProbMeanAssumed = 0.109d;
        [SerializeField] private double _plinkoScoredPegHitsPerCoinAssumed = 14d;
        [SerializeField] private double _firstSpinMaxSeconds = 30d;
        [SerializeField] private int[] _spinCheckpoints = { 0, 50, 100 };
        [SerializeField] private double[] _plinkoOnlySpinSecondsMax = { 30d, 60d, 120d };
        [SerializeField] private double _pipelineFrameBudgetMs = 0.2d;
        [SerializeField] private double _valueRelTolerance = 1e-9d;
        [SerializeField] private double _plinkoSoftCapTestFactor = 1.5d;

        public double RealisticTapRate => _realisticTapRate;
        public double PlinkoEvEffectiveAssumed => _plinkoEvEffectiveAssumed;
        public double PlinkoBounceUpliftAtCapAssumed => _plinkoBounceUpliftAtCapAssumed;
        public IReadOnlyList<double> PlinkoRawRateCeilingAssumed => _plinkoRawRateCeilingAssumed;
        public double PlinkoJackpotProbMeanAssumed => _plinkoJackpotProbMeanAssumed;
        public double PlinkoScoredPegHitsPerCoinAssumed => _plinkoScoredPegHitsPerCoinAssumed;
        public double FirstSpinMaxSeconds => _firstSpinMaxSeconds;
        public IReadOnlyList<int> SpinCheckpoints => _spinCheckpoints;
        public IReadOnlyList<double> PlinkoOnlySpinSecondsMax => _plinkoOnlySpinSecondsMax;
        public double PipelineFrameBudgetMs => _pipelineFrameBudgetMs;
        public double ValueRelTolerance => _valueRelTolerance;
        public double PlinkoSoftCapTestFactor => _plinkoSoftCapTestFactor;
    }
}
