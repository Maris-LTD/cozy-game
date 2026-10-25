using CozyGachapon.Contracts;

namespace CozyGachapon.Audio
{
    public sealed class BusBackgroundController
    {
        private readonly AudioConfig _config;
        private readonly AudioRuntimeState _state;
        private AudioFade _fade;
        private bool _isBackground;
        private double _transitionEnd;
        private bool _refreshPending;

        public BusBackgroundController(AudioConfig config, AudioRuntimeState state)
        {
            _config = config;
            _state = state;
            _fade.Snap(0f);
        }

        public bool Set(AudioBus bus, bool on, double now)
        {
            if (bus != AudioBus.Plinko || on == _isBackground)
            {
                return false;
            }

            _isBackground = on;
            _state.SetBusBackground(AudioBus.Plinko, on);
            _fade.Retarget(on ? 1f : 0f, _config.BusTransitionSeconds, now);
            _transitionEnd = now + _config.BusTransitionSeconds;
            _refreshPending = true;
            return true;
        }

        public float GetBackgroundDb(AudioBus bus, double now)
        {
            if (bus != AudioBus.Plinko)
            {
                return 0f;
            }

            return _config.PlinkoBackgroundOffsetDb * _fade.Evaluate(now);
        }

        public bool ConsumeRefresh(double now)
        {
            if (!_refreshPending)
            {
                return false;
            }

            if (now >= _transitionEnd)
            {
                _refreshPending = false;
            }

            return true;
        }
    }
}
