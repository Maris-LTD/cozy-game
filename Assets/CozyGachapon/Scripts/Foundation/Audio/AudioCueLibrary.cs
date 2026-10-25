using System;
using System.Collections.Generic;
using UnityEngine;

namespace CozyGachapon.Audio
{
    [CreateAssetMenu(fileName = "AudioCueLibrary", menuName = "CozyGachapon/Audio/AudioCueLibrary")]
    public sealed class AudioCueLibrary : ScriptableObject
    {
        [SerializeField] private SoundCue[] _cues = Array.Empty<SoundCue>();

        public IReadOnlyList<SoundCue> Cues => _cues ?? Array.Empty<SoundCue>();
    }
}
