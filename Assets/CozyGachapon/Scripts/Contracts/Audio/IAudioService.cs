using System;

namespace CozyGachapon.Contracts
{
    public interface IAudioService
    {
        bool IsReady { get; }
        AudioStats Stats { get; }
        bool IsUserMuted { get; }

        event Action<bool> MuteChanged;

        bool TryResolveCue(string cueId, out int cueIndex);
        PlayResult Play(string cueId, in AudioPlayParams playParams);
        PlayResult Play(int cueIndex, in AudioPlayParams playParams);
        void SetBusBackground(AudioBus bus, bool background);
        void SetUserVolume(AudioBus bus, float slider);
        float GetUserVolume(AudioBus bus);
        void SetUserMuted(bool muted);
        void SetMuteReason(MuteReason reason, bool active);
        void HandlePaused(in PausedEvent pausedEvent);
        void HandleResumed(in ResumedEvent resumedEvent);
    }
}
