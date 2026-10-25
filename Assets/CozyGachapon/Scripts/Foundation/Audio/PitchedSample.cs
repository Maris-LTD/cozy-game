using System;
using UnityEngine;

namespace CozyGachapon.Audio
{
    [Serializable]
    public class PitchedSample
    {
        [SerializeField] private AudioClip _clip;
        [SerializeField] private int _nativeSemitone;

        public AudioClip Clip => _clip;
        public int NativeSemitone => _nativeSemitone;

        public PitchedSample(AudioClip clip, int nativeSemitone)
        {
            _clip = clip;
            _nativeSemitone = nativeSemitone;
        }
    }
}
