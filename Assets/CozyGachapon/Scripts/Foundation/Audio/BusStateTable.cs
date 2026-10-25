using System;
using CozyGachapon.Contracts;

namespace CozyGachapon.Audio
{
    public sealed class BusStateTable
    {
        private const float FullSlider = 1f;

        private readonly AudioConfig _config;
        private readonly AudioRuntimeState _state;
        private readonly float[] _userDb;
        private readonly float[] _userSlider;

        public BusStateTable(AudioConfig config, AudioRuntimeState state)
        {
            _config = config;
            _state = state;
            int busCount = Enum.GetValues(typeof(AudioBus)).Length;
            _userDb = new float[busCount];
            _userSlider = new float[busCount];
            SetUserVolume(AudioBus.Music, config.DefaultUserSlider);
            SetUserVolume(AudioBus.Sfx, config.DefaultUserSlider);
        }

        public bool SetUserVolume(AudioBus bus, float slider)
        {
            if (!HasUserSlider(bus))
            {
                return false;
            }

            float normalized = SliderMath.Normalize(slider, _config.DefaultUserSlider);
            if (normalized == _userSlider[(int)bus])
            {
                return false;
            }

            bool audible = SliderMath.TryToDb(slider, _config.UserSliderMinDb, _config.DefaultUserSlider, out float userDb);
            _userDb[(int)bus] = audible ? userDb : 0f;
            _userSlider[(int)bus] = normalized;
            _state.SetBusSilent(bus, !audible);
            return true;
        }

        public float GetUserDb(AudioBus bus)
        {
            int index = (int)bus;
            return index >= 0 && index < _userDb.Length ? _userDb[index] : 0f;
        }

        public float GetUserSlider(AudioBus bus)
        {
            return HasUserSlider(bus) ? _userSlider[(int)bus] : FullSlider;
        }

        private static bool HasUserSlider(AudioBus bus)
        {
            return bus == AudioBus.Music || bus == AudioBus.Sfx;
        }
    }
}
