namespace CozyGachapon.Contracts
{
    public interface IAudioService
    {
        bool IsReady { get; }
        AudioStats Stats { get; }

        bool TryResolveCue(string cueId, out int cueIndex);
        PlayResult Play(string cueId, in AudioPlayParams playParams);
        PlayResult Play(int cueIndex, in AudioPlayParams playParams);
        void SetBusBackground(AudioBus bus, bool background);
        void SetUserVolume(AudioBus bus, float slider);
    }
}
