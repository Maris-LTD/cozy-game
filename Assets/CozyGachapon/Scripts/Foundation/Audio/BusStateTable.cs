using System;
using CozyGachapon.Contracts;

namespace CozyGachapon.Audio
{
    public sealed class BusStateTable
    {
        private readonly AudioConfig _config;
        private readonly AudioRuntimeState _state;
        private readonly float[] _userDb;

        public BusStateTable(AudioConfig config, AudioRuntimeState state)
        {
            _config = config;
            _state = state;
            _userDb = new float[Enum.GetValues(typeof(AudioBus)).Length];
            SetUserVolume(AudioBus.Music, config.DefaultUserSlider);
            SetUserVolume(AudioBus.Sfx, config.DefaultUserSlider);
        }

        public bool SetUserVolume(AudioBus bus, float slider)
        {
            if (bus != AudioBus.Music && bus != AudioBus.Sfx)
            {
                return false;
            }

            bool audible = SliderMath.TryToDb(slider, _config.UserSliderMinDb, _config.DefaultUserSlider, out float userDb);
            _userDb[(int)bus] = audible ? userDb : 0f;
            _state.SetBusSilent(bus, !audible);
            return true;
        }

        public float GetUserDb(AudioBus bus)
        {
            int index = (int)bus;
            return index >= 0 && index < _userDb.Length ? _userDb[index] : 0f;
        }
    }
}
