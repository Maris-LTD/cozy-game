namespace CozyGachapon.Contracts
{
    public interface IAudioService
    {
        bool IsReady { get; }
        AudioStats Stats { get; }

        bool TryResolveCue(string cueId, out int cueIndex);
        PlayResult Play(string cueId, in AudioPlayParams playParams);
        PlayResult Play(int cueIndex, in AudioPlayParams playParams);
    }
}
