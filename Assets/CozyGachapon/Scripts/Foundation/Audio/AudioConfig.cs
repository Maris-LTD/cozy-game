using System;
using System.Collections.Generic;
using CozyGachapon.Contracts;
using UnityEngine;

namespace CozyGachapon.Audio
{
    [CreateAssetMenu(fileName = "AudioConfig", menuName = "CozyGachapon/Audio/AudioConfig")]
    public class AudioConfig : ScriptableObject
    {
        [SerializeField] private int _totalVoices = 16;
        [SerializeField] private int _musicVoices = 1;
        [SerializeField] private int _ambienceVoices = 0;
        [SerializeField] private int _minFreeVoicesForRewards = 3;
        [SerializeField] private int _stormPriorityCeiling = 60;
        [SerializeField] private float _silenceFloorDb = -80f;
        [SerializeField] private float _masterDefaultDb = 0f;
        [SerializeField] private float _sfxDefaultDb = 0f;
        [SerializeField] private float _gameplayDefaultDb = 0f;
        [SerializeField] private float _plinkoDefaultDb = 0f;
        [SerializeField] private float _gachaDefaultDb = 0f;
        [SerializeField] private float _uiDefaultDb = 0f;
        [SerializeField] private float _ambienceDefaultDb = -18f;
        [SerializeField] private float _musicOffsetFromSfxDb = -12f;
        [SerializeField] private float _plinkoBackgroundOffsetDb = -12f;
        [SerializeField] private float _busTransitionSeconds = 0.2f;
        [SerializeField] private float _userMuteFadeSeconds = 0.15f;
        [SerializeField] private float _resumeFadeInSeconds = 0.3f;
        [SerializeField] private float _musicFadeInSeconds = 2f;
        [SerializeField] private float _maxPan = 0.3f;
        [SerializeField] private float _fallbackPitchPosition = 0.5f;
        [SerializeField] private int _scaleRootMidi = 60;
        [SerializeField] private int[] _scaleIntervals = { 0, 2, 4, 7, 9 };
        [SerializeField] private int _scaleNoteCount = 10;
        [SerializeField] private int _maxSampleShiftSemitones = 4;
        [SerializeField] private float _userSliderMinDb = -40f;
        [SerializeField] private float _defaultUserSlider = 1f;
        [SerializeField] private bool _defaultUserMuted = false;

        public int TotalVoices => _totalVoices;
        public int MusicVoices => _musicVoices;
        public int AmbienceVoices => _ambienceVoices;
        public int MinFreeVoicesForRewards => _minFreeVoicesForRewards;
        public int StormPriorityCeiling => _stormPriorityCeiling;
        public float SilenceFloorDb => _silenceFloorDb;
        public float MusicOffsetFromSfxDb => _musicOffsetFromSfxDb;
        public float PlinkoBackgroundOffsetDb => _plinkoBackgroundOffsetDb;
        public float BusTransitionSeconds => _busTransitionSeconds;
        public float UserMuteFadeSeconds => _userMuteFadeSeconds;
        public float ResumeFadeInSeconds => _resumeFadeInSeconds;
        public float MusicFadeInSeconds => _musicFadeInSeconds;
        public float MaxPan => _maxPan;
        public float FallbackPitchPosition => _fallbackPitchPosition;
        public int ScaleRootMidi => _scaleRootMidi;
        public IReadOnlyList<int> ScaleIntervals => _scaleIntervals;
        public int ScaleNoteCount => _scaleNoteCount;
        public int MaxSampleShiftSemitones => _maxSampleShiftSemitones;
        public float UserSliderMinDb => _userSliderMinDb;
        public float DefaultUserSlider => _defaultUserSlider;
        public bool DefaultUserMuted => _defaultUserMuted;

        public float GetBusDefaultDb(AudioBus bus)
        {
            switch (bus)
            {
                case AudioBus.Master:
                    return _masterDefaultDb;
                case AudioBus.Music:
                    return _sfxDefaultDb + _musicOffsetFromSfxDb;
                case AudioBus.Ambience:
                    return _ambienceDefaultDb;
                case AudioBus.Sfx:
                    return _sfxDefaultDb;
                case AudioBus.Gameplay:
                    return _gameplayDefaultDb;
                case AudioBus.Plinko:
                    return _plinkoDefaultDb;
                case AudioBus.Gacha:
                    return _gachaDefaultDb;
                case AudioBus.Ui:
                    return _uiDefaultDb;
                default:
                    throw new ArgumentOutOfRangeException(nameof(bus), bus, null);
            }
        }

        public void SetSilenceFloorDbForTest(float silenceFloorDb)
        {
            _silenceFloorDb = silenceFloorDb;
        }

        public void SetBusTransitionSecondsForTest(float seconds)
        {
            _busTransitionSeconds = seconds;
        }
    }
}
