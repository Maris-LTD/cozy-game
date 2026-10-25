using UnityEngine;

namespace CozyGachapon.Audio
{
    public interface IVoiceBackend
    {
        int VoiceCount { get; }
        bool SupportsPan { get; }

        void Start(int voice, AudioClip clip, float volume, float pitch, float pan, bool loop);
        void Stop(int voice);
        void Pause(int voice);
        void Resume(int voice);
        void SetVolume(int voice, float volume);
        bool IsPlaying(int voice);
        float GetPosition(int voice);
        float GetSpatialBlend(int voice);
    }
}
