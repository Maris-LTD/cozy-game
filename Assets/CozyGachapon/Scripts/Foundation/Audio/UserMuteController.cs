namespace CozyGachapon.Audio
{
    public sealed class UserMuteController
    {
        private const float FullGain = 1f;
        private const float SilentGain = 0f;

        private readonly AudioConfig _config;
        private readonly AudioRuntimeState _state;
        private AudioFade _fade;
        private double _transitionEnd;
        private bool _refreshPending;
        private bool _soundOnPending;

        public UserMuteController(AudioConfig config, AudioRuntimeState state)
        {
            _config = config;
            _state = state;
            _fade.Snap(state.IsUserMuted ? SilentGain : FullGain);
        }

        public void Restore(bool muted)
        {
            _state.SetUserMuted(muted);
            _fade.Snap(muted ? SilentGain : FullGain);
            _refreshPending = false;
            _soundOnPending = false;
        }

        public bool Set(bool muted, double now)
        {
            if (!_state.SetUserMuted(muted))
            {
                return false;
            }

            _fade.Retarget(muted ? SilentGain : FullGain, _config.UserMuteFadeSeconds, now);
            _transitionEnd = now + _config.UserMuteFadeSeconds;
            _refreshPending = true;
            _soundOnPending = !muted;
            return true;
        }

        public float Gain(double now)
        {
            return _fade.Evaluate(now);
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

        public bool ConsumeSoundOnDue(double now)
        {
            if (!_soundOnPending || now < _transitionEnd)
            {
                return false;
            }

            _soundOnPending = false;
            return true;
        }
    }
}
