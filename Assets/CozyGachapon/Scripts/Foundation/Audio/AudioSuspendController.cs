using CozyGachapon.Contracts;

namespace CozyGachapon.Audio
{
    public sealed class AudioSuspendController
    {
        private const float FullGain = 1f;
        private const float SilentGain = 0f;

        private readonly AudioConfig _config;
        private readonly AudioRuntimeState _state;
        private AudioFade _fade;
        private double _transitionEnd;
        private bool _refreshPending;

        public bool IsSuspended => _state.IsSuspended;

        public AudioSuspendController(AudioConfig config, AudioRuntimeState state)
        {
            _config = config;
            _state = state;
            _fade.Snap(state.IsSuspended ? SilentGain : FullGain);
        }

        public SuspendTransition Apply(MuteReason reason, bool active, double now)
        {
            bool wasSuspended = _state.IsSuspended;
            if (!_state.SetSuspendReason(reason, active) || wasSuspended == _state.IsSuspended)
            {
                return SuspendTransition.None;
            }

            if (_state.IsSuspended)
            {
                _fade.Snap(SilentGain);
                _refreshPending = false;
                return SuspendTransition.Suspended;
            }

            _fade.StartFade(SilentGain, FullGain, _config.ResumeFadeInSeconds, now);
            _transitionEnd = now + _config.ResumeFadeInSeconds;
            _refreshPending = true;
            return SuspendTransition.Resumed;
        }

        public float FadeFactor(double now)
        {
            return _state.IsSuspended ? SilentGain : _fade.Evaluate(now);
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
