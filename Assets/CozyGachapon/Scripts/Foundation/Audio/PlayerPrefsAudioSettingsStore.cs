using CozyGachapon.Contracts;
using UnityEngine;

namespace CozyGachapon.Audio
{
    public sealed class PlayerPrefsAudioSettingsStore : IAudioSettingsStore
    {
        private const string UserMutedKey = "CozyGachapon.Audio.UserMuted";
        private const string MusicSliderKey = "CozyGachapon.Audio.MusicSlider";
        private const string SfxSliderKey = "CozyGachapon.Audio.SfxSlider";
        private const int MutedValue = 1;
        private const int UnmutedValue = 0;

        public bool TryReadUserMuted(out bool muted)
        {
            if (!PlayerPrefs.HasKey(UserMutedKey))
            {
                muted = false;
                return false;
            }

            muted = PlayerPrefs.GetInt(UserMutedKey, UnmutedValue) == MutedValue;
            return true;
        }

        public void WriteUserMuted(bool muted)
        {
            PlayerPrefs.SetInt(UserMutedKey, muted ? MutedValue : UnmutedValue);
            PlayerPrefs.Save();
        }

        public bool TryReadSlider(AudioBus bus, out float slider)
        {
            if (!TryGetSliderKey(bus, out string key) || !PlayerPrefs.HasKey(key))
            {
                slider = 0f;
                return false;
            }

            slider = PlayerPrefs.GetFloat(key);
            return true;
        }

        public void WriteSlider(AudioBus bus, float slider)
        {
            if (!TryGetSliderKey(bus, out string key))
            {
                return;
            }

            PlayerPrefs.SetFloat(key, slider);
            PlayerPrefs.Save();
        }

        private static bool TryGetSliderKey(AudioBus bus, out string key)
        {
            switch (bus)
            {
                case AudioBus.Music:
                    key = MusicSliderKey;
                    return true;
                case AudioBus.Sfx:
                    key = SfxSliderKey;
                    return true;
                default:
                    key = null;
                    return false;
            }
        }
    }
}
