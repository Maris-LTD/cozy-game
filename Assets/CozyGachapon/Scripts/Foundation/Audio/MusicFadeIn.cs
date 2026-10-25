using CozyGachapon.Contracts;

namespace CozyGachapon.Audio
{
    public sealed class MusicFadeIn
    {
        private const float FullGain = 1f;
        private const float SilentGain = 0f;

        private readonly AudioConfig _config;
        private AudioFade _fade;
        private double _fadeEnd;
        private bool _refreshPending;

        public MusicFadeIn(AudioConfig config)
        {
            _config = config;
            _fade.Snap(FullGain);
        }

        public void Begin(double now)
        {
            _fade.StartFade(SilentGain, FullGain, _config.MusicFadeInSeconds, now);
            _fadeEnd = now + _config.MusicFadeInSeconds;
            _refreshPending = true;
        }

        public void Cancel()
        {
            _fade.Snap(FullGain);
            _refreshPending = false;
        }

        public float Factor(AudioBus bus, double now)
        {
            return bus == AudioBus.Music ? _fade.Evaluate(now) : FullGain;
        }

        public bool ConsumeRefresh(double now)
        {
            if (!_refreshPending)
            {
                return false;
            }

            if (now >= _fadeEnd)
            {
                _refreshPending = false;
            }

            return true;
        }
    }
}
