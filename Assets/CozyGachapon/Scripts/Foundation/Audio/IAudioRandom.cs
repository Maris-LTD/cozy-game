namespace CozyGachapon.Audio
{
    public interface IAudioRandom
    {
        float NextSigned();
        int NextIndex(int count);
    }
}
