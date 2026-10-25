using System;
using UnityEngine;

namespace CozyGachapon.Audio
{
    public sealed class UnityVoiceBackend : IVoiceBackend
    {
        private const float NeutralPitch = 1f;

        private readonly Transform _container;
        private readonly int _requestedVoices;
        private AudioSource[] _sources = Array.Empty<AudioSource>();

        public bool IsAllocated { get; private set; }
        public int LastStartedVoice { get; private set; } = -1;
        public bool SupportsPan => true;

        public int VoiceCount => _sources.Length;

        public UnityVoiceBackend(Transform container, int voiceCount)
        {
            _container = container;
            _requestedVoices = voiceCount;
        }

        public void Allocate()
        {
            if (IsAllocated)
            {
                return;
            }

            IsAllocated = true;
            if (_container == null || _requestedVoices <= 0)
            {
                return;
            }

            AudioSource[] sources = new AudioSource[_requestedVoices];
            GameObject host = _container.gameObject;
            for (int i = 0; i < sources.Length; i++)
            {
                AudioSource source = host.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.loop = false;
                sources[i] = source;
            }

            _sources = sources;
        }

        public AudioSource GetSource(int voice)
        {
            return IsValid(voice) ? _sources[voice] : null;
        }

        public void Start(int voice, AudioClip clip, float volume, float pitch, float pan, bool loop)
        {
            if (!IsValid(voice))
            {
                return;
            }

            AudioSource source = _sources[voice];
            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch > 0f ? pitch : NeutralPitch;

            if (SupportsPan)
            {
                source.panStereo = pan;
            }

            source.loop = loop;
            source.Play();
            LastStartedVoice = voice;
        }

        public void Stop(int voice)
        {
            if (IsValid(voice))
            {
                _sources[voice].Stop();
            }
        }

        public void Pause(int voice)
        {
            if (IsValid(voice))
            {
                _sources[voice].Pause();
            }
        }

        public void Resume(int voice)
        {
            if (IsValid(voice))
            {
                _sources[voice].UnPause();
            }
        }

        public void SetVolume(int voice, float volume)
        {
            if (IsValid(voice))
            {
                _sources[voice].volume = volume;
            }
        }

        public bool IsPlaying(int voice)
        {
            return IsValid(voice) && _sources[voice].isPlaying;
        }

        public float GetPosition(int voice)
        {
            return IsValid(voice) ? _sources[voice].time : 0f;
        }

        public float GetSpatialBlend(int voice)
        {
            return IsValid(voice) ? _sources[voice].spatialBlend : 0f;
        }

        private bool IsValid(int voice)
        {
            return voice >= 0 && voice < _sources.Length && _sources[voice] != null;
        }
    }
}
