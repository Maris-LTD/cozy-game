using System;
using CozyGachapon.Contracts;

namespace CozyGachapon.Audio
{
    public sealed class AudioRuntimeState
    {
        private readonly bool[] _busSilent;
        private readonly bool[] _busBackground;
        private readonly int _maxHops;
        private MuteReasonSet _reasons;

        public bool IsLocked { get; private set; }
        public MuteReason MuteReasons => _reasons.Reasons;
        public MuteReason SuspendReasons => _reasons.SuspendReasons;
        public bool IsUserMuted => _reasons.IsUserMuted;
        public bool IsSuspended => _reasons.IsSuspended;

        public AudioRuntimeState()
        {
            _maxHops = Enum.GetValues(typeof(AudioBus)).Length;
            _busSilent = new bool[_maxHops];
            _busBackground = new bool[_maxHops];
        }

        public void SetLocked(bool locked)
        {
            IsLocked = locked;
        }

        public bool SetSuspendReason(MuteReason reason, bool active)
        {
            return _reasons.Set(reason & MuteReasonSet.SuspendMask, active);
        }

        public bool SetUserMuted(bool muted)
        {
            return _reasons.Set(MuteReason.User, muted);
        }

        public void SetBusSilent(AudioBus bus, bool silent)
        {
            if (IsValidBus(bus))
            {
                _busSilent[(int)bus] = silent;
            }
        }

        public void SetBusBackground(AudioBus bus, bool background)
        {
            if (IsValidBus(bus))
            {
                _busBackground[(int)bus] = background;
            }
        }

        public bool IsBusSilentOnPath(AudioBus bus)
        {
            return IsFlaggedOnPath(_busSilent, bus);
        }

        public bool IsBusBackgroundOnPath(AudioBus bus)
        {
            return IsFlaggedOnPath(_busBackground, bus);
        }

        private bool IsFlaggedOnPath(bool[] flags, AudioBus bus)
        {
            AudioBus current = bus;
            for (int step = 0; step < _maxHops; step++)
            {
                if (!IsValidBus(current))
                {
                    return false;
                }

                if (flags[(int)current])
                {
                    return true;
                }

                if (!AudioBusTable.TryGetParent(current, out AudioBus parent))
                {
                    return false;
                }

                current = parent;
            }

            return false;
        }

        private bool IsValidBus(AudioBus bus)
        {
            int index = (int)bus;
            return index >= 0 && index < _maxHops;
        }
    }
}
