using CozyGachapon.Contracts;

namespace CozyGachapon.Audio
{
    public interface IAudioSettingsStore
    {
        bool TryReadUserMuted(out bool muted);
        void WriteUserMuted(bool muted);
        bool TryReadSlider(AudioBus bus, out float slider);
        void WriteSlider(AudioBus bus, float slider);
    }
}
