using CozyGachapon.Audio;
using CozyGachapon.Contracts;

namespace CozyGachapon.Tests.Audio
{
    public sealed class FakeAudioSettingsStore : IAudioSettingsStore
    {
        private bool _hasUserMuted;
        private bool _userMuted;
        private bool _hasMusicSlider;
        private float _musicSlider;
        private bool _hasSfxSlider;
        private float _sfxSlider;

        public int UserMutedWriteCount { get; private set; }
        public int SliderWriteCount { get; private set; }
        public bool HasUserMuted => _hasUserMuted;
        public bool StoredUserMuted => _userMuted;

        public bool TryReadUserMuted(out bool muted)
        {
            muted = _userMuted;
            return _hasUserMuted;
        }

        public void WriteUserMuted(bool muted)
        {
            _hasUserMuted = true;
            _userMuted = muted;
            UserMutedWriteCount++;
        }

        public bool TryReadSlider(AudioBus bus, out float slider)
        {
            switch (bus)
            {
                case AudioBus.Music:
                    slider = _musicSlider;
                    return _hasMusicSlider;
                case AudioBus.Sfx:
                    slider = _sfxSlider;
                    return _hasSfxSlider;
                default:
                    slider = 0f;
                    return false;
            }
        }

        public void WriteSlider(AudioBus bus, float slider)
        {
            SliderWriteCount++;
            if (bus == AudioBus.Music)
            {
                _hasMusicSlider = true;
                _musicSlider = slider;
            }
            else if (bus == AudioBus.Sfx)
            {
                _hasSfxSlider = true;
                _sfxSlider = slider;
            }
        }

        public bool HasSlider(AudioBus bus)
        {
            return TryReadSlider(bus, out _);
        }

        public float GetStoredSlider(AudioBus bus)
        {
            TryReadSlider(bus, out float slider);
            return slider;
        }

        public void Clear()
        {
            _hasUserMuted = false;
            _userMuted = false;
            _hasMusicSlider = false;
            _musicSlider = 0f;
            _hasSfxSlider = false;
            _sfxSlider = 0f;
        }
    }
}
