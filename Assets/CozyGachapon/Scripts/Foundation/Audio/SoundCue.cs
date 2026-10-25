using System.Collections.Generic;
using CozyGachapon.Contracts;
using UnityEngine;

namespace CozyGachapon.Audio
{
    [CreateAssetMenu(fileName = "SoundCue", menuName = "CozyGachapon/Audio/SoundCue")]
    public class SoundCue : ScriptableObject
    {
        [SerializeField] private string _cueId;
        [SerializeField] private AudioBus _bus;
        [SerializeField] private AudioClip[] _clips = new AudioClip[0];
        [SerializeField] private float _volumeDb;
        [SerializeField] private int _priority;
        [SerializeField] private int _maxInstances = 1;
        [SerializeField] private float _minInterval;
        [SerializeField] private InstanceCapPolicy _instanceCapPolicy;
        [SerializeField] private PitchMode _pitchMode;
        [SerializeField] private float _pitchJitterRatio;
        [SerializeField] private bool _keepWhenBusBackground;
        [SerializeField] private bool _avoidRepeat;
        [SerializeField] private bool _panFromPosition;
        [SerializeField] private bool _loop;
        [SerializeField] private PitchedSample[] _pitchedSamples = new PitchedSample[0];

        public string CueId => _cueId;
        public AudioBus Bus => _bus;
        public IReadOnlyList<AudioClip> Clips => _clips;
        public float VolumeDb => _volumeDb;
        public int Priority => _priority;
        public int MaxInstances => _maxInstances;
        public float MinInterval => _minInterval;
        public InstanceCapPolicy InstanceCapPolicy => _instanceCapPolicy;
        public PitchMode PitchMode => _pitchMode;
        public float PitchJitterRatio => _pitchJitterRatio;
        public bool KeepWhenBusBackground => _keepWhenBusBackground;
        public bool AvoidRepeat => _avoidRepeat;
        public bool PanFromPosition => _panFromPosition;
        public bool Loop => _loop;
        public IReadOnlyList<PitchedSample> PitchedSamples => _pitchedSamples;

        public void SetIdentityForTest(string cueId, AudioBus bus)
        {
            _cueId = cueId;
            _bus = bus;
        }

        public void SetClipsForTest(AudioClip[] clips)
        {
            _clips = clips;
        }

        public void SetPitchForTest(PitchMode pitchMode, float pitchJitterRatio, PitchedSample[] pitchedSamples)
        {
            _pitchMode = pitchMode;
            _pitchJitterRatio = pitchJitterRatio;
            _pitchedSamples = pitchedSamples;
        }
    }
}
