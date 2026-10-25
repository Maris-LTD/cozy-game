using System.Collections.Generic;
using UnityEngine;

namespace CozyGachapon.Audio
{
    [CreateAssetMenu(fileName = "AudioBalanceTargets", menuName = "CozyGachapon/Audio/AudioBalanceTargets")]
    public class AudioBalanceTargets : ScriptableObject
    {
        [SerializeField] private float _stormPegHitRate = 134f;
        [SerializeField] private float _pegClipLengthMaxSeconds = 0.5f;
        [SerializeField] private float _minStealAgeSeconds = 0.2f;
        [SerializeField] private float _manualSpawnClipMaxSeconds = 0.12f;
        [SerializeField] private float _spawnClipMarginSeconds = 0.03f;
        [SerializeField] private float _stormPeakGainMax = 2f;
        [SerializeField] private float _stormTestSeconds = 10f;
        [SerializeField] private int _requestToStartMaxFrames = 0;
        [SerializeField] private float _audioFrameBudgetMs = 1f;
        [SerializeField] private int _sfxDownloadBudgetKb = 600;
        [SerializeField] private int _musicDownloadBudgetKb = 2000;
        [SerializeField] private float _playtestCalmScoreMin = 4f;
        [SerializeField] private float _playtestJackpotRecognitionMin = 0.8f;
        [SerializeField] private int _playtestSampleSize = 5;
        [SerializeField] private float _playtestBackgroundAwarenessMin = 0.6f;
        [SerializeField] private int _playtestBackgroundAnnoyedMax = 0;
        [SerializeField] private float _spawnClipMarginMinSeconds = 0.017f;
        [SerializeField] private float _spawnClipMarginMaxSeconds = 0.05f;
        [SerializeField] private int _cuePriorityMin = 0;
        [SerializeField] private int _cuePriorityMax = 100;
        [SerializeField] private float _boundaryTolerance = 1e-4f;
        [SerializeField] private string[] _spawnCueIds = { "Plinko.CoinSpawn", "Plinko.CoinSpawnAuto" };

        public float StormPegHitRate => _stormPegHitRate;
        public float PegClipLengthMaxSeconds => _pegClipLengthMaxSeconds;
        public float MinStealAgeSeconds => _minStealAgeSeconds;
        public float ManualSpawnClipMaxSeconds => _manualSpawnClipMaxSeconds;
        public float SpawnClipMarginSeconds => _spawnClipMarginSeconds;
        public float StormPeakGainMax => _stormPeakGainMax;
        public float StormTestSeconds => _stormTestSeconds;
        public int RequestToStartMaxFrames => _requestToStartMaxFrames;
        public float AudioFrameBudgetMs => _audioFrameBudgetMs;
        public int SfxDownloadBudgetKb => _sfxDownloadBudgetKb;
        public int MusicDownloadBudgetKb => _musicDownloadBudgetKb;
        public float PlaytestCalmScoreMin => _playtestCalmScoreMin;
        public float PlaytestJackpotRecognitionMin => _playtestJackpotRecognitionMin;
        public int PlaytestSampleSize => _playtestSampleSize;
        public float PlaytestBackgroundAwarenessMin => _playtestBackgroundAwarenessMin;
        public int PlaytestBackgroundAnnoyedMax => _playtestBackgroundAnnoyedMax;
        public float SpawnClipMarginMinSeconds => _spawnClipMarginMinSeconds;
        public float SpawnClipMarginMaxSeconds => _spawnClipMarginMaxSeconds;
        public int CuePriorityMin => _cuePriorityMin;
        public int CuePriorityMax => _cuePriorityMax;
        public float BoundaryTolerance => _boundaryTolerance;
        public IReadOnlyList<string> SpawnCueIds => _spawnCueIds;

        public void SetSpawnClipLimitsForTest(float manualSpawnClipMaxSeconds, float spawnClipMarginSeconds)
        {
            _manualSpawnClipMaxSeconds = manualSpawnClipMaxSeconds;
            _spawnClipMarginSeconds = spawnClipMarginSeconds;
        }
    }
}
