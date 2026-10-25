using System;
using CozyGachapon.Contracts;
using UnityEngine;

namespace CozyGachapon.Audio
{
    public sealed class VoiceGainCalculator
    {
        private const float MaxLinearVolume = 1f;

        private readonly AudioConfig _config;
        private readonly AudioRuntimeState _state;
        private readonly BusStateTable _busTable;
        private readonly BusBackgroundController _background;
        private readonly int _maxHops;

        public VoiceGainCalculator(
            AudioConfig config,
            AudioRuntimeState state,
            BusStateTable busTable,
            BusBackgroundController background)
        {
            _config = config;
            _state = state;
            _busTable = busTable;
            _background = background;
            _maxHops = Enum.GetValues(typeof(AudioBus)).Length;
        }

        public float Compute(SoundCue cue, double now)
        {
            if (_state.IsBusSilentOnPath(cue.Bus))
            {
                return 0f;
            }

            float totalDb = cue.VolumeDb;
            AudioBus current = cue.Bus;
            for (int step = 0; step < _maxHops; step++)
            {
                totalDb += _config.GetBusDefaultDb(current)
                    + _busTable.GetUserDb(current)
                    + _background.GetBackgroundDb(current, now);
                if (!AudioBusTable.TryGetParent(current, out AudioBus parent))
                {
                    break;
                }

                current = parent;
            }

            return Mathf.Min(MaxLinearVolume, AudioMath.DbToLinear(totalDb, _config.SilenceFloorDb));
        }
    }
}
