using CozyGachapon.Contracts;

namespace CozyGachapon.Audio
{
    public sealed class AudioUserSettings
    {
        private readonly AudioConfig _config;
        private readonly IAudioSettingsStore _store;

        public AudioUserSettings(AudioConfig config, IAudioSettingsStore store)
        {
            _config = config;
            _store = store;
        }

        public bool ReadUserMuted()
        {
            return _store.TryReadUserMuted(out bool muted) ? muted : _config.DefaultUserMuted;
        }

        public float ReadSlider(AudioBus bus)
        {
            return _store.TryReadSlider(bus, out float slider) ? slider : _config.DefaultUserSlider;
        }

        public void SaveUserMuted(bool muted)
        {
            _store.WriteUserMuted(muted);
        }

        public void SaveSlider(AudioBus bus, float slider)
        {
            _store.WriteSlider(bus, slider);
        }
    }
}
