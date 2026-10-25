using System;
using CozyGachapon.Audio;
using UnityEngine;

namespace CozyGachapon.Tests.Audio
{
    public sealed class ThrowingVoiceBackend : IVoiceBackend
    {
        public int VoiceCount => throw new InvalidOperationException();
        public bool SupportsPan => false;

        public void Allocate()
        {
        }

        public void Start(int voice, AudioClip clip, float volume, float pitch, float pan, bool loop)
        {
        }

        public void Stop(int voice)
        {
        }

        public void Pause(int voice)
        {
        }

        public void Resume(int voice)
        {
        }

        public void SetVolume(int voice, float volume)
        {
        }

        public bool IsPlaying(int voice)
        {
            return false;
        }

        public float GetPosition(int voice)
        {
            return 0f;
        }

        public float GetSpatialBlend(int voice)
        {
            return 0f;
        }
    }
}
